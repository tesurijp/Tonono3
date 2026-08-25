namespace Tonono3.SKKEngine

open System
open System.Collections.Generic
open System.Collections.Immutable

type InputMode = Disabled = 0 | Hiragana = 1 | Katakana = 2 | Zenkaku = 3

[<Sealed>]
type KeyCommand(vkCode: int, shift: bool, control: bool, ch: char) =
    member _.VkCode = vkCode
    member _.Shift = shift
    member _.Control = control
    member _.Character = ch

[<Sealed>]
type RegistrationFrame(reading: string, previousMode: InputMode, word: string) =
    member _.Reading = reading
    member _.PreviousMode = previousMode
    member _.Word = word

type internal Okuri = { Prefix: string; Reading: string }
type internal CompositionMode = Direct | Conversion of Okuri option | Abbreviation
type internal Selection = { Items: string list; Index: int }
type internal Completion = { Items: string list; Index: int }
type internal Composition =
    { Romaji: string
      Text: string
      Mode: CompositionMode
      Candidates: Selection option
      Completion: Completion option }
type internal InputState = Disabled | Idle of InputMode | Composing of InputMode * Composition
type internal CoreState = { Input: InputState; Registrations: RegistrationFrame list }
type internal KanaConversion =
    { ShouldStartConversion: bool
      Output: string
      Remaining: string
      Diagnostic: string option }

module internal StateModel =
    let emptyComposition mode =
        { Romaji = ""; Text = ""; Mode = mode; Candidates = None; Completion = None }

    let inputMode = function Disabled -> InputMode.Disabled | Idle mode | Composing(mode, _) -> mode

    let composition = function Composing(_, value) -> Some value | _ -> None

    let ofCompatibility romaji text mode conversion abbreviation okuri reading candidates candidateIndex completions completionIndex registrations =
        let compositionMode =
            if abbreviation then Abbreviation
            elif conversion then
                Conversion(if isNull okuri then None else Some { Prefix = okuri; Reading = reading })
            else Direct
        let selection: Selection option =
            if candidateIndex >= 0 && candidateIndex < Array.length candidates then
                Some { Items = List.ofArray candidates; Index = candidateIndex }
            else None
        let completion: Completion option =
            if completionIndex >= 0 && completionIndex < Array.length completions then
                Some { Items = List.ofArray completions; Index = completionIndex }
            else None
        let active = romaji <> "" || text <> "" || compositionMode <> Direct || selection.IsSome || completion.IsSome
        let input =
            if mode = InputMode.Disabled then Disabled
            elif active then Composing(mode, { Romaji = romaji; Text = text; Mode = compositionMode; Candidates = selection; Completion = completion })
            else Idle mode
        { Input = input; Registrations = registrations |> Array.rev |> List.ofArray }

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
type AppConfig internal (
    romaji: Map<string,string>, mora: Map<string,string>, moraComplete: Map<string,string>,
    zenkaku: Map<char,string>, dictionaryPaths: string array, userDictionaryPath: string,
    viAppEntries: string array) =
    let stringEntries source =
        source
        |> Map.toArray
        |> Array.map (fun (key, value) -> KeyValuePair(key, value))
    let charEntries source =
        source
        |> Map.toArray
        |> Array.map (fun (key, value) -> KeyValuePair(key, value))
    member internal _.Romaji = romaji
    member internal _.Mora = mora
    member internal _.MoraComplete = moraComplete
    member internal _.Zenkaku = zenkaku
    member internal _.DictionaryPathEntries = dictionaryPaths
    member internal _.ViAppEntries = viAppEntries
    member _.RomajiEntries = stringEntries romaji
    member _.MoraEntries = stringEntries mora
    member _.MoraCompleteEntries = stringEntries moraComplete
    member _.ZenkakuEntries = charEntries zenkaku
    member _.DictionaryPaths = Array.copy dictionaryPaths
    member _.UserDictionaryPath = userDictionaryPath
    member _.ViCompatibleApps = Array.copy viAppEntries
    member _.HasError =
        Map.isEmpty romaji || Map.isEmpty mora || Map.isEmpty zenkaku || Array.isEmpty dictionaryPaths
    member this.HasChange(other: AppConfig) =
        this.UserDictionaryPath <> other.UserDictionaryPath ||
        this.Romaji <> other.Romaji ||
        this.Mora <> other.Mora ||
        this.MoraComplete <> other.MoraComplete ||
        this.Zenkaku <> other.Zenkaku ||
        this.DictionaryPathEntries <> other.DictionaryPathEntries ||
        this.ViAppEntries <> other.ViAppEntries

[<Sealed>]
type DictionarySnapshot internal (main: Map<string,string list>, user: Map<string,string list>) =
    let toImmutable (source: Map<string,string list>) =
        source
        |> Seq.map (fun (KeyValue(k, values)) -> KeyValuePair(k, ImmutableArray.CreateRange(values)))
        |> ImmutableDictionary.CreateRange
    member internal _.MainMap = main
    member internal _.UserMap = user
    member _.User = toImmutable user

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
