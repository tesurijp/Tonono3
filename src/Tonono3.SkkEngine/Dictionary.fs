namespace Tonono3.SKKEngine

open System
open System.Collections.Generic

module internal Dictionary =
    let parseLine (line: string) =
        if String.IsNullOrWhiteSpace(line) || line.StartsWith(';') then None else
        match line.IndexOf(' ') with
        | index when index < 0 -> None
        | index ->
            let reading = line.Substring(0, index)
            let candidates = line.Substring(index).Trim()
            if candidates.StartsWith('/') && candidates.EndsWith('/') then
                candidates.Split('/', StringSplitOptions.RemoveEmptyEntries)
                |> Array.map (fun candidate -> candidate.Split(';')[0])
                |> Array.distinct
                |> Array.toList
                |> fun values -> Some(reading, values)
            else None

    let candidates (reading: string) (dictionary: DictionarySnapshot) =
        let user = Map.tryFind reading dictionary.UserMap |> Option.defaultValue []
        let main = Map.tryFind reading dictionary.MainMap |> Option.defaultValue []
        List.append user main |> List.distinct

    let private isOkuriEntry (key: string) =
        key.Length > 1 && Char.IsLower(key[key.Length - 1]) &&
        (key |> Seq.exists (fun c -> int c > 0x7f || not(Char.IsLower(c))))

    let completions (prefix: string) (dictionary: DictionarySnapshot) =
        if String.IsNullOrEmpty(prefix) then [] else
        Seq.append (dictionary.UserMap |> Map.keys) (dictionary.MainMap |> Map.keys)
        |> Seq.filter (fun key -> key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && not(isOkuriEntry key))
        |> Seq.distinct
        |> Seq.sortWith (fun a b -> let length = compare a.Length b.Length in if length <> 0 then length else StringComparer.Ordinal.Compare(a,b))
        |> Seq.toList

    let addWord (reading: string) (word: string) (dictionary: DictionarySnapshot) =
        let previous = Map.tryFind reading dictionary.UserMap |> Option.defaultValue []
        let next = word :: List.filter ((<>) word) previous
        if previous = next then None
        else Some(DictionarySnapshot(dictionary.MainMap, Map.add reading next dictionary.UserMap))

    let removeWord (reading: string) (word: string) (dictionary: DictionarySnapshot) =
        match Map.tryFind reading dictionary.UserMap with
        | None -> None
        | Some previous ->
            let next = List.filter ((<>) word) previous
            if previous = next then None
            else Some(DictionarySnapshot(dictionary.MainMap, Map.add reading next dictionary.UserMap))

    let load (lines: IEnumerable<string>) =
        let entries = System.Collections.Generic.Dictionary<string, ResizeArray<string>>()
        for line in lines do
            match parseLine line with
            | Some(reading, candidates) ->
                match entries.TryGetValue(reading) with
                | true, previous ->
                    for candidate in candidates do
                        if not(previous.Contains(candidate)) then
                            previous.Add(candidate)
                | false, _ -> entries[reading] <- ResizeArray(candidates)
            | None -> ()
        entries
        |> Seq.map (fun pair -> pair.Key, List.ofSeq pair.Value)
        |> Map.ofSeq

    let loadAll (mainSources: IEnumerable<string>) (userSource: IEnumerable<string>) =
        DictionarySnapshot(load mainSources, load userSource)
