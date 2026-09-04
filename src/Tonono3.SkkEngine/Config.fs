namespace Tonono3.SkkEngine

open System
open System.Collections.Generic
open System.IO

module internal Config =
    type ResultBuilder() =
        member _.Return(x) = Ok x
        member _.ReturnFrom(m: Result<'T, 'E>) = m
        member _.Bind(m: Result<'T, 'E>, f: 'T -> Result<'U, 'E>) = Result.bind f m
        member _.Zero() = Ok ()

    let private mapUnique entries =
        entries
        |> Seq.fold (fun state (key, value) ->
            if Map.containsKey key state then
                invalidArg (nameof entries) $"Duplicate config key: {key}"
            Map.add key value state) Map.empty

    let private pathConvert configFolder path =
        if not(String.IsNullOrEmpty(path)) && path[0] = '.' then Path.Combine(configFolder, path)
        else Path.GetFullPath(path)

    let compileRomaji vowels (rows: Dictionary<string, string array>) (irregularRomaji: Dictionary<string, string>) =
        let roman = 
            rows
            |> Seq.collect (fun row ->
                vowels
                |> Seq.mapi (fun index vowel -> row.Key + string vowel, row.Value[index])
                |> Seq.filter (fun (_, kana) -> not(String.IsNullOrEmpty(kana))))
            |> mapUnique
            |> fun source -> irregularRomaji |> Seq.fold (fun state pair -> Map.add pair.Key pair.Value state) source
        if Map.isEmpty roman then Error "Empty Romaji table"  else Ok roman

    let compileMora (moraModifiers: Dictionary<string, List<string>>) =
        let mora =
            moraModifiers
            |> Seq.collect (fun pair -> pair.Value |> Seq.map (fun item -> item, pair.Key))
            |> mapUnique
        if Map.isEmpty mora then Error "Empty Mora table" else Ok mora

    let compileMoraComplete  (moraComplete: Dictionary<string, string>) =
        Map.ofSeq (moraComplete |> Seq.map (fun pair -> pair.Key, pair.Value))

    let compileZenkaku (zenkakuStart: char) (zenkakuEnd: char) (zenkakuOffset: int) (irregularZenkaku: Dictionary<string, string>)  =
        let zenkaku =
            seq { for value in int zenkakuStart .. int zenkakuEnd -> char value, string(char(value + zenkakuOffset)) }
            |> mapUnique
            |> fun source -> irregularZenkaku |> Seq.fold (fun state pair -> Map.add pair.Key[0] pair.Value state) source
        if Map.isEmpty zenkaku then Error "Empty Zenkaku table" else Ok zenkaku

    let compileMainDictionaryPath configFolder (dictionaryPaths: string array) =
        let paths = dictionaryPaths |> Array.map (pathConvert configFolder)
        if Array.isEmpty paths then Error "Empty dictionary" else Ok paths

    let compileUserDictionaryPath configFolder userDictionaryPath =
        pathConvert configFolder userDictionaryPath

    let compileViAppEntries (viApps:string array) =
        viApps |> Array.map (fun value -> value.Replace('/', '\\'))

    let compileCandidateSelectionKeys keys =
        let value = if String.IsNullOrEmpty(keys) then ConfigDefaults.CandidateSelectionKeys else keys
        if value |> Seq.exists (fun key -> key < 'A' || key > 'Z') then
            Error "Candidate selection keys must contain only uppercase ASCII letters"
        elif value |> Seq.distinct |> Seq.length <> value.Length then
            Error "Candidate selection keys must not contain duplicates"
        else Ok value

    let compile path
        configFolder (dictionaryPaths: string array) userDictionaryPath
        vowels (rows: Dictionary<string, string array>) (irregularRomaji: Dictionary<string, string>)
        (moraModifiers: Dictionary<string, List<string>>) (moraComplete: Dictionary<string, string>)
        (zenkakuStart: char) (zenkakuEnd: char) (zenkakuOffset: int)
        (irregularZenkaku: Dictionary<string, string>) (viApps: string array) candidateSelectionKeys =

        let result = ResultBuilder()

        result {
            let! romaji = compileRomaji vowels rows irregularRomaji
            let! mora = compileMora moraModifiers 
            let moraComp = compileMoraComplete moraComplete
            let! zenkaku = compileZenkaku zenkakuStart zenkakuEnd zenkakuOffset irregularZenkaku
            let! mainDictionaryPath = compileMainDictionaryPath configFolder dictionaryPaths
            let userDictionaryPath = compileUserDictionaryPath configFolder userDictionaryPath
            let viAppEntries = compileViAppEntries viApps
            let! selectionKeys = compileCandidateSelectionKeys candidateSelectionKeys
            return AppConfig(path, romaji, mora, moraComp, zenkaku, mainDictionaryPath, userDictionaryPath, viAppEntries, selectionKeys)
        }
