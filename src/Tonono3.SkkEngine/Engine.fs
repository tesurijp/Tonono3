namespace Tonono3.SKKEngine

open System

module internal Engine =
    open KeyCode
    open CommandContext
    open EngineComposition

    let rec private processKey (config: AppConfig) (activePath: string) (command: KeyCommand) (runtime: Runtime) =
        match preCheck activePath config command runtime with
        | Some result -> result
        | None ->
            match composition runtime, runtime.State.Registrations, runtime.State.Input with
            | Some { Candidates = Some _ }, _, _ -> processConversion config activePath command runtime
            | Some _, _, _ -> processComposition config command runtime
            | None, _ :: _, _ -> processRegistration config command runtime
            | None, _, Disabled ->
                if command.Control && command.VkCode = J then true, runtime |> changeMode InputMode.Hiragana |> turnOffIme else false, runtime
            | None, _, Idle InputMode.Zenkaku ->
                if command.Control && command.VkCode = J then true, runtime |> changeMode InputMode.Hiragana |> turnOffIme
                else Map.tryFind command.Character config.Zenkaku |> Option.map (fun text -> true, commitProduced text runtime) |> Option.defaultValue (false, runtime)
            | None, _, Idle _ -> processIdle config command runtime
            | None, _, Composing _ -> false, runtime

    and private processIdle (config: AppConfig) (command: KeyCommand) (runtime: Runtime) =
        match command.Control, command.VkCode, command.Shift with
        | true, _, _ -> commonControl command runtime
        | false, L, false -> true, runtime |> turnOffIme |> commitAll |> changeMode InputMode.Disabled
        | false, L, true -> true, runtime |> commitAll |> changeMode InputMode.Zenkaku
        | false, Q, _ -> handleQ config runtime
        | false, Slash, false -> true, withComposition (StateModel.emptyComposition Abbreviation) runtime
        | false, Back, _ -> handleBackspace runtime
        | false, Return, _ when isBufferActive runtime -> true, commitAll runtime
        | false, Space, _ when canStartConversion runtime -> true, startConversion config runtime
        | _ -> handleChar config command.Character runtime

    and private processComposition (config: AppConfig) (command: KeyCommand) (runtime: Runtime) =
        let runtime =
            match composition runtime with
            | Some value when value.Completion.IsSome && command.VkCode <> Tab && command.VkCode <> Space -> withComposition { value with Completion = None } runtime
            | _ -> runtime
        match command.Control, command.VkCode, command.Shift with
        | _, Escape, _ -> true, reset runtime
        | true, _, _ -> commonControl command runtime
        | false, L, false -> true, runtime |> turnOffIme |> commitAll |> changeMode InputMode.Disabled
        | false, L, true -> true, runtime |> commitAll |> changeMode InputMode.Zenkaku
        | false, Q, _ -> handleQ config runtime
        | false, Return, _ -> true, commitAll runtime
        | false, Tab, false ->
            let value = composition runtime |> Option.get
            let completion =
                match value.Completion with
                | None ->
                    match Dictionary.completions value.Text runtime.Dictionary with
                    | [] -> None
                    | items -> Some ({ Items = items; Index = 0 }: Completion)
                | Some current -> Some { current with Index = (current.Index + 1) % current.Items.Length }
            true, withComposition { value with Completion = completion } runtime
        | false, Space, _ ->
            let value = composition runtime |> Option.get
            match value.Completion with
            | Some current ->
                let accepted = { value with Text = List.item current.Index current.Items; Completion = None }
                true, withComposition accepted runtime |> startConversion config
            | None when canStartConversion runtime -> true, startConversion config runtime
            | None -> false, runtime
        | false, Back, _ -> handleBackspace runtime
        | _ -> handleChar config command.Character runtime

    and private processRegistration (config: AppConfig) (command: KeyCommand) (runtime: Runtime) =
        match command.Control, command.VkCode with
        | _, Escape -> true, cancelRegistration runtime
        | true, J -> true, commitAll runtime
        | true, G -> true, cancelRegistration runtime
        | true, _ -> false, runtime
        | false, Return -> true, finishRegistration runtime
        | false, Back -> true, registrationBackspace runtime
        | _ -> processIdle config command runtime

    and private processConversion (config: AppConfig) (activePath: string) (command: KeyCommand) (runtime: Runtime) =
        let value = composition runtime |> Option.get
        let selection = value.Candidates |> Option.get
        let pageStart = selection.Index / 7 * 7
        let direct = match command.VkCode with A -> Some 0 | S -> Some 1 | D -> Some 2 | F -> Some 3 | J -> Some 4 | K -> Some 5 | L -> Some 6 | _ -> None
        match direct with
        | Some offset when selection.Index >= 4 && pageStart + offset < selection.Items.Length ->
            true, withComposition { value with Candidates = Some { selection with Index = pageStart + offset } } runtime |> commitAll
        | _ ->
            match command.Control, command.VkCode with
            | _, Escape | true, G | _, Back -> true, withComposition { value with Mode = Conversion None; Candidates = None } runtime
            | true, J -> true, commitAll runtime
            | true, N ->
                let index = selection.Index + 1
                if index < selection.Items.Length then true, withComposition { value with Candidates = Some { selection with Index = index } } runtime
                else true, startRegistration (dictionaryKey value) runtime
            | true, P ->
                let candidates = if selection.Index = 0 then None else Some { selection with Index = selection.Index - 1 }
                true, withComposition { value with Candidates = candidates } runtime
            | true, X ->
                let word = List.item selection.Index selection.Items
                let items = selection.Items |> List.mapi (fun index item -> index, item) |> List.choose (fun (index, item) -> if index = selection.Index then None else Some item)
                let candidates: Selection option = if items.IsEmpty then None else Some { Items = items; Index = selection.Index % items.Length }
                true, runtime |> removeDictionaryWord (dictionaryKey value) word |> withComposition { value with Candidates = candidates }
            | true, _ -> false, runtime
            | false, Space ->
                let index = if selection.Index >= 4 then pageStart + 7 else selection.Index + 1
                if index < selection.Items.Length then true, withComposition { value with Candidates = Some { selection with Index = index } } runtime
                else true, startRegistration (dictionaryKey value) runtime
            | false, Return | false, Q -> true, commitAll runtime
            | false, _ when command.Character = char 0 -> false, runtime
            | false, _ when Char.IsControl(command.Character) -> false, commitAll runtime
            | _ ->
                let committed = commitAll runtime
                let _, next = processKey config activePath command committed
                true, next

    let run (state: EngineState) (config: AppConfig) (dictionary: DictionarySnapshot) (command: KeyCommand) (activePath: string) =
        let initial = { State = state.Core; Dictionary = dictionary; Effects = [] }
        let handled, result = processKey config (if isNull activePath then "" else activePath) command initial
        TransitionResult(EngineState(result.State), result.Dictionary, handled, result.Effects |> List.rev |> List.toArray)
