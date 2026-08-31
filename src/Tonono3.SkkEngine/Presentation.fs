namespace Tonono3.SKKEngine

open System
open System.Text

module internal Presentation =
    let create (state: EngineState) (config: AppConfig) (version: int64 ) =
        let core = state.Core
        let selectionKeys = config.CandidateSelectionKeys
        let pageSize = selectionKeys.Length
        let depth = core.Registrations.Length
        let inputMode = StateModel.inputMode core.Input
        let statusMode = match inputMode with InputMode.Hiragana -> "あ" | InputMode.Katakana -> "ア" | InputMode.Zenkaku -> "全" | _ -> "？"
        let status = String('[', depth + 1) + statusMode + String(']', depth + 1)
        let value = StateModel.composition core.Input
        let visible = not core.Registrations.IsEmpty || value |> Option.exists (fun item -> item.Text <> "" || item.Romaji <> "")
        let compositionText =
            match value with
            | Some item ->
                match item.Candidates, item.Completion with
                | Some selection, _ ->
                    let selected = if selection.Index < pageSize then List.item selection.Index selection.Items else ""
                    let suffix =
                        match item.Mode with
                        | Conversion(Some okuri) ->
                            let start = min okuri.Reading.Length item.Text.Length
                            "[" + item.Text.Substring(start) + item.Romaji + "]"
                        | _ -> ""
                    DisplayPrefix.Conversion + selected + suffix
                | None, Some completion -> DisplayPrefix.Composition + List.item completion.Index completion.Items + item.Romaji
                | None, None -> DisplayPrefix.Composition + item.Text + item.Romaji
            | None -> DisplayPrefix.Composition
        let candidateList =
            match value |> Option.bind _.Candidates with
            | Some selection when selection.Index >= pageSize ->
                let start = selection.Index / pageSize * pageSize
                selection.Items
                |> List.skip start
                |> List.truncate pageSize
                |> List.mapi (fun index candidate -> (if start + index = selection.Index then $"[{selectionKeys[index]}] : " else $" {selectionKeys[index]}  : ") + candidate + " ")
                |> String.concat ""
            | _ -> ""
        let reading, word = match core.Registrations with current :: _ -> current.Reading, current.Word | [] -> "", ""
        UiSnapshot(version, visible, status, not core.Registrations.IsEmpty, reading, word, compositionText, candidateList)
