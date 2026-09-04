namespace Tonono3.SkkEngine

open System

module internal CommandContext =
    type Runtime =
        { State: CoreState
          Dictionary: DictionarySnapshot
          Effects: EngineEffect list }

    let mode (runtime: Runtime) = StateModel.inputMode runtime.State.Input
    let composition (runtime: Runtime) = StateModel.composition runtime.State.Input
    let isBufferActive (runtime: Runtime) = composition runtime |> Option.exists (fun value -> value.Text <> "" || value.Romaji <> "")
    let canStartConversion (runtime: Runtime) = composition runtime |> Option.exists (fun value -> value.Text <> "")
    let withInput (input: InputState) (runtime: Runtime) = { runtime with State = { runtime.State with Input = input } }
    let withComposition (value: Composition) (runtime: Runtime) =
        let currentMode = mode runtime
        let input =
            if value.Mode = Direct && value.Romaji = "" && value.Text = "" && value.Candidates.IsNone && value.Completion.IsNone then
                if currentMode = InputMode.Disabled then Disabled else Idle currentMode
            else Composing(currentMode, value)
        withInput input runtime
    let addEffect (effect: EngineEffect) (runtime: Runtime) = { runtime with Effects = effect :: runtime.Effects }
    let turnOffIme (runtime: Runtime) = addEffect (TurnOffImeEffect()) runtime
    let addLog (message: string) (runtime: Runtime) = addEffect (WriteLogEffect(message)) runtime
    let reset (runtime: Runtime) = withInput (if mode runtime = InputMode.Disabled then Disabled else Idle(mode runtime)) runtime
    let changeMode (next: InputMode) (runtime: Runtime) =
        match runtime.State.Input with
        | Composing(_, value) -> withInput (Composing(next, value)) runtime
        | _ -> withInput (if next = InputMode.Disabled then Disabled else Idle next) runtime

    let commitProduced (text: string) (runtime: Runtime) =
        if String.IsNullOrEmpty(text) then runtime
        else
            match runtime.State.Registrations with
            | current :: rest ->
                let updated = RegistrationFrame(current.Reading, current.PreviousMode, current.Word + text)
                { runtime with State = { runtime.State with Registrations = updated :: rest } }
            | [] -> addEffect (CommitTextEffect(text)) runtime

    let updateDictionary operation (reading: string) (word: string) (runtime: Runtime) =
        match operation reading word runtime.Dictionary with
        | Some dictionary -> { runtime with Dictionary = dictionary } |> addEffect (PersistUserDictionaryEffect())
        | None -> runtime

    let addDictionaryWord = updateDictionary Dictionary.addWord
    let removeDictionaryWord = updateDictionary Dictionary.removeWord

    let dictionaryKey (value: Composition) =
        match value.Mode with
        | Conversion(Some okuri) -> okuri.Reading + okuri.Prefix
        | _ -> value.Text
        |> KanaConverter.kataToHiragana

    let startRegistration (reading: string) (runtime: Runtime) =
        let frame = RegistrationFrame(reading, mode runtime, "")
        { runtime with State = { Input = Idle InputMode.Hiragana; Registrations = frame :: runtime.State.Registrations } }

    let cancelRegistration (runtime: Runtime) =
        match runtime.State.Registrations with
        | current :: rest ->
            let value = { StateModel.emptyComposition (Conversion None) with Text = current.Reading }
            { runtime with State = { Input = Composing(current.PreviousMode, value); Registrations = rest } }
        | [] -> runtime

    let finishRegistration (runtime: Runtime) =
        match runtime.State.Registrations with
        | current :: rest ->
            let cleared = { runtime with State = { Input = Idle current.PreviousMode; Registrations = rest } }
            if String.IsNullOrWhiteSpace(current.Word) then cleared
            else cleared |> addDictionaryWord current.Reading current.Word |> commitProduced current.Word
        | [] -> runtime

    let registrationBackspace (runtime: Runtime) =
        match runtime.State.Registrations with
        | current :: rest when current.Word.Length > 0 ->
            let word = current.Word.Substring(0, current.Word.Length - 1)
            { runtime with State = { runtime.State with Registrations = RegistrationFrame(current.Reading, current.PreviousMode, word) :: rest } }
        | _ -> runtime
