namespace Tonono3.SKKEngine

open System.Collections.Generic
open System.Collections.Immutable

[<Sealed>]
type EngineState internal (core: CoreState) =
    new(
        romajiBuffer: string, compositionBuffer: string, mode: InputMode,
        isConversionMode: bool, isAbbreviationMode: bool, okuriPrefix: string,
        readingBeforeOkuri: string, candidates: string array, candidateIndex: int,
        completions: string array, completionIndex: int, registrationStack: RegistrationFrame array) =
        EngineState(StateModel.ofCompatibility romajiBuffer compositionBuffer mode isConversionMode isAbbreviationMode okuriPrefix readingBeforeOkuri candidates candidateIndex completions completionIndex registrationStack)
    member internal _.Core = core
    member _.RomajiBuffer = StateModel.composition core.Input |> Option.map _.Romaji |> Option.defaultValue ""
    member _.CompositionBuffer = StateModel.composition core.Input |> Option.map _.Text |> Option.defaultValue ""
    member _.Mode = StateModel.inputMode core.Input
    member _.Completions =
        StateModel.composition core.Input |> Option.bind _.Completion
        |> Option.map (fun value -> List.toArray value.Items) |> Option.defaultValue [||]
    member _.CompletionIndex = StateModel.composition core.Input |> Option.bind _.Completion |> Option.map _.Index |> Option.defaultValue -1
    member _.RegistrationStack = core.Registrations |> List.rev |> List.toArray

[<Sealed>]
type EngineConfig internal (
    romaji: Map<string,string>, mora: Map<string,string>, moraComplete: Map<string,string>,
    zenkaku: Map<char,string>, viApps: Set<string>) =
    member internal _.Romaji = romaji
    member internal _.Mora = mora
    member internal _.MoraComplete = moraComplete
    member internal _.Zenkaku = zenkaku
    member internal _.ViApps = viApps

[<Sealed>]
type DictionarySnapshot internal (main: Map<string,string list>, user: Map<string,string list>) =
    let toImmutable (source: Map<string,string list>) =
        source
        |> Seq.map (fun (KeyValue(k, values)) -> KeyValuePair(k, ImmutableArray.CreateRange(values)))
        |> ImmutableDictionary.CreateRange
    member internal _.MainMap = main
    member internal _.UserMap = user
    member _.User = toImmutable user

[<Sealed>]
type ParsedDictionaryEntry(isValid: bool, reading: string, candidates: string array) =
    member _.IsValid = isValid
    member _.Reading = reading
    member _.Candidates = Array.copy candidates

[<AbstractClass>]
type EngineEffect() = class end

[<Sealed>]
type CommitTextEffect(text: string) =
    inherit EngineEffect()
    member _.Text = text

[<Sealed>]
type PersistUserDictionaryEffect() = inherit EngineEffect()

[<Sealed>]
type TurnOffImeEffect() = inherit EngineEffect()

[<Sealed>]
type WriteLogEffect(message: string) =
    inherit EngineEffect()
    member _.Message = message

[<Sealed>]
type TransitionResult(state: EngineState, dictionary: DictionarySnapshot, handled: bool, effects: EngineEffect array) =
    member _.State = state
    member _.Dictionary = dictionary
    member _.IsHandled = handled
    member _.Effects = Array.copy effects

[<Sealed>]
type UiSnapshot(
    isVisible: bool, statusText: string, isRegistration: bool, registrationReading: string,
    registrationWord: string, composition: string, candidateList: string) =
    member _.IsVisible = isVisible
    member _.StatusText = statusText
    member _.IsInRegistrationMode = isRegistration
    member _.RegistrationReading = registrationReading
    member _.RegistrationWord = registrationWord
    member _.Composition = composition
    member _.CandidateList = candidateList
