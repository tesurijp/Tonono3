namespace Tonono3.SKKEngine

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
