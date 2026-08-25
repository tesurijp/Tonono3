namespace Tonono3.SKKEngine

open System.Collections.Generic
open tsr_di

[<AbstractClass; Sealed>]
type SkkEngineFacade private () =
    [<ServiceFunction>]
    static member CreateInitialState() =
        EngineState({ Input = Disabled; Registrations = [] })

    [<ServiceFunction>]
    static member CreateKeyCommand(vkCode: int, shift: bool, control: bool, ch: char) =
        KeyCommand(vkCode, shift, control, ch)

    [<ServiceFunction>]
    static member CompileConfig(
        configFolder: string, dictionaryPaths: string array, userDictionaryPath: string,
        vowels: string, rows: Dictionary<string, string array>, irregularRomaji: Dictionary<string, string>,
        moraModifiers: Dictionary<string, List<string>>, moraComplete: Dictionary<string, string>,
        zenkakuStart: char, zenkakuEnd: char, zenkakuOffset: int,
        irregularZenkaku: Dictionary<string, string>, viApps: string array) =
        Config.compile configFolder dictionaryPaths userDictionaryPath vowels rows irregularRomaji
            moraModifiers moraComplete zenkakuStart zenkakuEnd zenkakuOffset irregularZenkaku viApps

    [<ServiceFunction>]
    static member LoadDictionary(
        mainSources: IEnumerable<string>,
        userSource: IEnumerable<string>) =
        Dictionary.loadAll mainSources userSource

    [<ServiceFunction>]
    static member GetCandidates(dictionary: DictionarySnapshot, reading: string) =
        Dictionary.candidates reading dictionary |> List.toArray

    [<ServiceFunction>]
    static member GetCompletions(dictionary: DictionarySnapshot, prefix: string) =
        Dictionary.completions prefix dictionary |> List.toArray

    [<ServiceFunction>]
    static member ProcessKey(state, config, dictionary, command, activeProcessPath: string) =
        Engine.run state config dictionary command activeProcessPath

    [<ServiceFunction>]
    static member CreateUiSnapshot(state, version) = Presentation.create state version
