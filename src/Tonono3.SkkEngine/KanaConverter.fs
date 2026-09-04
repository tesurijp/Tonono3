namespace Tonono3.SkkEngine

open System
open System.Text

module internal KanaConverter =
    let private potentialPrefix (table: Map<string,string>) romaji =
        table |> Map.exists (fun key _ -> key.StartsWith(romaji, StringComparison.Ordinal))

    let canMatch (config: AppConfig) (value: string) = Map.containsKey value config.Romaji || potentialPrefix config.Romaji value

    let tryFinish (config: AppConfig) (romaji: string) = Map.tryFind romaji config.MoraComplete

    let rec convert (config: AppConfig) (romaji: string) (start: bool) =
        match Map.tryFind romaji config.Romaji with
        | Some kana -> { ShouldStartConversion = start; Output = kana; Remaining = ""; Diagnostic = None }
        | None ->
            match Map.tryFind romaji config.Mora with
            | Some mora -> { ShouldStartConversion = false; Output = mora; Remaining = romaji.Substring(1); Diagnostic = None }
            | None when potentialPrefix config.Romaji romaji -> { ShouldStartConversion = false; Output = ""; Remaining = romaji; Diagnostic = None }
            | None when romaji.Length > 1 ->
                match tryFinish config (romaji.Substring(0, romaji.Length - 1)) with
                | Some finished ->
                    let next = convert config (romaji.Substring(romaji.Length - 1)) false
                    { next with Output = finished + next.Output }
                | None ->
                    { ShouldStartConversion = false; Output = romaji.Substring(0, 1); Remaining = romaji.Substring(1)
                      Diagnostic = Some $"No match in romaji table for: {romaji}. Flushing: {romaji.Substring(0, 1)}" }
            | None ->
                { ShouldStartConversion = false; Output = romaji.Substring(0, 1); Remaining = romaji.Substring(1)
                  Diagnostic = Some $"No match in romaji table for: {romaji}. Flushing: {romaji.Substring(0, 1)}" }

    let private kanaToKana (convertChar: char -> char) (text: string) =
        let result = StringBuilder()
        for c in text do result.Append(convertChar c) |> ignore
        result.ToString()

    let hiraToKatakana (text: string) = kanaToKana (fun c -> if c >= 'ぁ' && c <= 'ゖ' then char(int c + 0x60) else c) text
    let kataToHiragana (text: string) = kanaToKana (fun c -> if c >= 'ァ' && c <= 'ヶ' then char(int c - 0x60) else c) text
