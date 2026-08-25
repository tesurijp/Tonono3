namespace Tonono3.SKKEngine

open System
open System.Collections.Generic
open System.IO

module internal Config =
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
        rows
        |> Seq.collect (fun row ->
            vowels
            |> Seq.mapi (fun index vowel -> row.Key + string vowel, row.Value[index])
            |> Seq.filter (fun (_, kana) -> not(String.IsNullOrEmpty(kana))))
        |> mapUnique
        |> fun source -> irregularRomaji |> Seq.fold (fun state pair -> Map.add pair.Key pair.Value state) source

    let compileMora (moraModifiers: Dictionary<string, List<string>>) =
        moraModifiers
        |> Seq.collect (fun pair -> pair.Value |> Seq.map (fun item -> item, pair.Key))
        |> mapUnique

    let compileMoraComplete  (moraComplete: Dictionary<string, string>) =
        Map.ofSeq (moraComplete |> Seq.map (fun pair -> pair.Key, pair.Value))

    let compileZenkaku (zenkakuStart: char) (zenkakuEnd: char) (zenkakuOffset: int) (irregularZenkaku: Dictionary<string, string>)  =
        seq { for value in int zenkakuStart .. int zenkakuEnd -> char value, string(char(value + zenkakuOffset)) }
        |> mapUnique
        |> fun source -> irregularZenkaku |> Seq.fold (fun state pair -> Map.add pair.Key[0] pair.Value state) source

    let compileMainDictionaryPath configFolder (dictionaryPaths: string array) =
        dictionaryPaths |> Array.map (pathConvert configFolder)

    let compileUserDictionaryPath configFolder userDictionaryPath =
        pathConvert configFolder userDictionaryPath

    let compileViAppEntries (viApps:string array) =
        viApps |> Array.map (fun value -> value.Replace('/', '\\'))

    let compile
        configFolder (dictionaryPaths: string array) userDictionaryPath
        vowels (rows: Dictionary<string, string array>) (irregularRomaji: Dictionary<string, string>)
        (moraModifiers: Dictionary<string, List<string>>) (moraComplete: Dictionary<string, string>)
        (zenkakuStart: char) (zenkakuEnd: char) (zenkakuOffset: int)
        (irregularZenkaku: Dictionary<string, string>) (viApps: string array) =
        AppConfig(
            compileRomaji vowels rows irregularRomaji,
            compileMora moraModifiers ,
            compileMoraComplete moraComplete,
            compileZenkaku zenkakuStart zenkakuEnd zenkakuOffset irregularZenkaku,
            compileMainDictionaryPath configFolder dictionaryPaths,
            compileUserDictionaryPath configFolder userDictionaryPath,
            compileViAppEntries viApps)

