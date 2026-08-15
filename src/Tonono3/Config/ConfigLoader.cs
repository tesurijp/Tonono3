using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Tonono3.AutoDefined;
using tsr_di;
using VYaml.Serialization;

namespace Tonono3;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class ConfigLoader(IConfigPathProvider paths, WriteLogFunc writeLog)
{
    private readonly YamlSerializerOptions serializerOptions = CreateSerializerOptions();

    private static YamlSerializerOptions CreateSerializerOptions()
    {
        GeneratedResolver.Register(new ConfigYaml.ConfigYamlGeneratedFormatter());
        GeneratedResolver.Register(new RomajiTable.RomajiTableGeneratedFormatter());
        GeneratedResolver.Register(new Standard.StandardGeneratedFormatter());
        GeneratedResolver.Register(new ZenkakuTable.ZenkakuTableGeneratedFormatter());
        GeneratedResolver.Register(new ArrayFormatter<string>());
        GeneratedResolver.Register(new ListFormatter<string>());
        GeneratedResolver.Register(new DictionaryFormatter<string, string>());
        GeneratedResolver.Register(new DictionaryFormatter<string, string[]>());
        GeneratedResolver.Register(new DictionaryFormatter<string, List<string>>());
        var generatedResolver = new GeneratedResolver();

        return new YamlSerializerOptions
        {
            Resolver = CompositeResolver.Create([generatedResolver, BuiltinResolver.Instance, StandardResolver.Instance])
        };
    }

    [ServiceFunction(ServiceName = "ReloadConfigFunc")]
    public AppConfig Reload()
    {
        writeLog($"Loading config from: {paths.ConfigPath}");
        try
        {
            var yaml = File.ReadAllBytes(paths.ConfigPath);
            var yamlObj = YamlSerializer.Deserialize<ConfigYaml>(yaml, serializerOptions);
            var (romaji, mora, moraComp) = LoadRomajiTable(yamlObj);
            var zenkaku = LoadZenkakuTable(yamlObj);
            var (dics, userdic) = LoadDictionaryPath(yamlObj);
            var vicompatible = LoadViCompatibleApps(yamlObj);

            var appConfig = new AppConfig(romaji, mora, moraComp, zenkaku, dics, userdic, vicompatible);
            if (appConfig.HasError)
            {
                throw new InvalidDataException("Error loading config.yaml");
            }
            return appConfig;
        }
        catch (Exception ex)
        {
            writeLog($"Error loading config.yaml: {ex.Message}");
            throw;
        }
    }

    private string PathConvert(string path) => path.Length > 0 && path[0] == '.' ? Path.Combine(paths.ConfigFolder, path) : Path.GetFullPath(path);

    private (ImmutableArray<string> systemDic, string userDic) LoadDictionaryPath(ConfigYaml data) =>
        ([.. data.DictionaryPaths.Select(PathConvert)], PathConvert(data.UserDictionaryPath));

    private static ImmutableArray<KeyValuePair<char, string>> LoadZenkakuTable(ConfigYaml data)
    {
        var startVal = data.ZenkakuTable.Standard.Start;
        var endVal = data.ZenkakuTable.Standard.End;
        var offset = data.ZenkakuTable.Standard.Offset;

        var zenkaku = Enumerable.Sequence(startVal, endVal, (char)1).Select(i => (i, ((char)(i + offset)).ToString())).ToDictionary();
        foreach (var (key, value) in data.ZenkakuTable.Irregular)
        {
            zenkaku[key[0]] = value;
        }
        return ToSortedEntries(zenkaku, Comparer<char>.Default);
    }

    private static (ImmutableArray<KeyValuePair<string, string>> romaji, ImmutableArray<KeyValuePair<string, string>> mora, ImmutableArray<KeyValuePair<string, string>> moraCompete) LoadRomajiTable(ConfigYaml data)
    {
        var vowels = data.RomajiTable.Vowel;
        var rows = data.RomajiTable.Rows;

        var romaji = rows.SelectMany(row => vowels.Select((vowel, i) => (key: row.Key + vowel, Kana: row.Value[i])))
            .Where(x => !string.IsNullOrEmpty(x.Kana)).ToDictionary();
        foreach (var (key, value) in data.RomajiTable.Irregular)
        {
            romaji[key] = value;
        }

        var mora = data.RomajiTable.MoraModifier.SelectMany(k => k.Value.Select(item => (item, ch: k.Key))).ToDictionary();
        return (ToSortedEntries(romaji, StringComparer.Ordinal), ToSortedEntries(mora, StringComparer.Ordinal), ToSortedEntries(data.RomajiTable.MoraAutoComplete, StringComparer.Ordinal));
    }

    private static ImmutableArray<string> LoadViCompatibleApps(ConfigYaml data) => [.. data.ViCompatibleApps.Select(i => i.Replace('/', '\\'))];

    private static ImmutableArray<KeyValuePair<TKey, TValue>> ToSortedEntries<TKey, TValue>(
        Dictionary<TKey, TValue> source,
        IComparer<TKey> comparer)
        where TKey : notnull =>
        [.. source.OrderBy(pair => pair.Key, comparer)];
}
