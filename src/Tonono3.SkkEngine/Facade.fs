namespace Tonono3.SKKEngine

open System.Collections.Generic
open System.Collections.Immutable
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
    static member CreateConfig(
        romaji: ImmutableArray<KeyValuePair<string,string>>, mora: ImmutableArray<KeyValuePair<string,string>>,
        moraComplete: ImmutableArray<KeyValuePair<string,string>>, zenkaku: ImmutableArray<KeyValuePair<char,string>>,
        viApps: ImmutableArray<string>) =
        let toMap (source: ImmutableArray<KeyValuePair<'k,'v>>) = source |> Seq.map (fun pair -> pair.Key, pair.Value) |> Map.ofSeq
        EngineConfig(toMap romaji, toMap mora, toMap moraComplete, toMap zenkaku, viApps |> Seq.map (fun value -> value.ToLowerInvariant()) |> Set.ofSeq)

    [<ServiceFunction>]
    static member CreateDictionary(
        main: ImmutableDictionary<string, ImmutableArray<string>>,
        user: ImmutableDictionary<string, ImmutableArray<string>>) =
        let toMap (source: ImmutableDictionary<string, ImmutableArray<string>>) =
            source |> Seq.map (fun pair -> pair.Key, pair.Value |> Seq.toList) |> Map.ofSeq
        DictionarySnapshot(toMap main, toMap user)

    [<ServiceFunction>]
    static member ParseDictionaryLine(line: string) =
        match Dictionary.parseLine line with
        | Some(reading, candidates) -> ParsedDictionaryEntry(true, reading, List.toArray candidates)
        | None -> ParsedDictionaryEntry(false, "", [||])

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
    static member CreateUiSnapshot(state) = Presentation.create state
