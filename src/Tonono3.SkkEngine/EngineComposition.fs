namespace Tonono3.SkkEngine

open System

module internal EngineComposition =
    open KeyCode
    open CommandContext

    let handleKana (kana: string) (runtime: Runtime) =
        let output = if mode runtime = InputMode.Katakana then KanaConverter.hiraToKatakana kana else kana
        match composition runtime with
        | Some value when (match value.Mode with Conversion _ -> true | _ -> false) ->
            withComposition { value with Text = value.Text + output } runtime
        | _ -> commitProduced output runtime

    let rec startConversion (config: AppConfig) (runtime: Runtime) =
        let value = composition runtime |> Option.defaultValue (StateModel.emptyComposition (Conversion None))
        let runtime, value =
            match KanaConverter.tryFinish config value.Romaji with
            | Some finished -> handleKana finished runtime, { value with Romaji = "" }
            | None -> runtime, value
        let value = composition runtime |> Option.defaultValue value |> fun current -> { current with Romaji = value.Romaji; Completion = None }
        let key = dictionaryKey value
        match Dictionary.candidates key runtime.Dictionary with
        | current :: remaining -> withComposition { value with Candidates = Some { Items = current :: remaining; Index = 0 } } runtime
        | [] -> runtime |> addLog $"No candidates found for: {key}" |> startRegistration key

    and tryConvertRomaji (config: AppConfig) (runtime: Runtime) =
        match composition runtime with
        | None -> runtime
        | Some value ->
            let canStart = match value.Mode with Conversion(Some _) when value.Candidates.IsNone -> true | _ -> false
            let conversion = KanaConverter.convert config value.Romaji canStart
            let runtime = conversion.Diagnostic |> Option.map (fun message -> addLog message runtime) |> Option.defaultValue runtime
            let runtime = if conversion.Output = "" then runtime else handleKana conversion.Output runtime
            let current = composition runtime |> Option.defaultValue value
            let runtime = withComposition { current with Romaji = conversion.Remaining } runtime
            if conversion.ShouldStartConversion then startConversion config runtime else runtime

    let commitAll (runtime: Runtime) =
        match composition runtime with
        | None -> runtime
        | Some value ->
            let text, runtime =
                match value.Candidates with
                | Some selection ->
                    let word = List.item selection.Index selection.Items
                    let learned = addDictionaryWord (dictionaryKey value) word runtime
                    let suffix =
                        match value.Mode with
                        | Conversion(Some okuri) ->
                            let start = min okuri.Reading.Length value.Text.Length
                            value.Text.Substring(start) + value.Romaji
                        | _ -> ""
                    word + suffix, learned
                | None -> value.Text + value.Romaji, runtime
            runtime |> commitProduced text |> reset

    let flipAndCommit (config: AppConfig) (runtime: Runtime) =
        match composition runtime with
        | None -> runtime
        | Some value ->
            let text = KanaConverter.tryFinish config value.Romaji |> Option.map ((+) value.Text) |> Option.defaultValue value.Text
            let flipped = if mode runtime = InputMode.Hiragana then KanaConverter.hiraToKatakana text else KanaConverter.kataToHiragana text
            runtime |> commitProduced flipped |> reset

    let viCompatible (activePath: string) (config: AppConfig) =
        if String.IsNullOrEmpty(activePath) then false
        else
            let normalized = activePath.Replace('/', '\\')
            config.ViAppEntries |> Array.exists (fun app -> normalized.EndsWith(app, StringComparison.OrdinalIgnoreCase))

    let preCheck (activePath: string) (config: AppConfig) (command: KeyCommand) (runtime: Runtime) =
        if command.VkCode = Escape && mode runtime <> InputMode.Direct && viCompatible activePath config then
            Some(false, runtime |> reset |> changeMode InputMode.Direct)
        elif command.VkCode >= NavigationFirst && command.VkCode <= NavigationLast then Some(false, runtime)
        else None

    let handleBackspace (runtime: Runtime) =
        match composition runtime with
        | Some value when value.Romaji <> "" ->
            true, withComposition { value with Romaji = value.Romaji.Substring(0, value.Romaji.Length - 1) } runtime
        | Some value when value.Text <> "" ->
            let text = value.Text.Substring(0, value.Text.Length - 1)
            let nextMode =
                match value.Mode with
                | Conversion(Some okuri) when text.Length < okuri.Reading.Length -> Conversion None
                | current when text = "" -> Direct
                | current -> current
            true, withComposition { value with Text = text; Mode = nextMode } runtime
        | _ -> false, runtime

    let handleRomaji (config: AppConfig) (character: char) (runtime: Runtime) =
        let initial = composition runtime |> Option.defaultValue (StateModel.emptyComposition Direct)
        let value =
            if Char.IsUpper(character) && Char.IsLetter(character) then
                match initial.Mode with
                | Direct -> { initial with Mode = Conversion None }
                | Conversion None when initial.Text <> "" ->
                    let text, romaji =
                        match KanaConverter.tryFinish config initial.Romaji with
                        | Some mora -> initial.Text + mora, (if initial.Romaji.Length > 0 then initial.Romaji.Substring(1) else "")
                        | None -> initial.Text, initial.Romaji
                    let prefix = Char.ToLower(if romaji.Length > 0 then romaji[0] else character).ToString()
                    { initial with Text = text; Romaji = romaji; Mode = Conversion(Some { Prefix = prefix; Reading = text }) }
                | _ -> initial
            else initial
        let value = { value with Romaji = value.Romaji + Char.ToLower(character).ToString() }
        true, withComposition value runtime |> tryConvertRomaji config

    let handleChar (config: AppConfig) (character: char) (runtime: Runtime) =
        if character = char 0 || Char.IsControl(character) then false, runtime
        else
            match composition runtime with
            | Some value when value.Mode = Abbreviation -> true, withComposition { value with Text = value.Text + string character } runtime
            | _ ->
                let symbol = not(Char.IsLetter(character)) && not(Char.IsDigit(character))
                if (symbol && not(KanaConverter.canMatch config (string character))) || character = ' ' then
                    false, if isBufferActive runtime then commitAll runtime else runtime
                else handleRomaji config character runtime

    let handleQ (config: AppConfig) (runtime: Runtime) =
        if isBufferActive runtime then true, flipAndCommit config runtime
        else
            let next = match mode runtime with InputMode.Hiragana -> InputMode.Katakana | InputMode.Katakana -> InputMode.Hiragana | value -> value
            true, changeMode next runtime

    let commonControl (command: KeyCommand) (runtime: Runtime) =
        match command.VkCode with
        | J -> true, runtime |> turnOffIme |> commitAll |> changeMode InputMode.Hiragana
        | G when isBufferActive runtime -> true, reset runtime
        | G -> false, runtime
        | _ -> false, runtime

