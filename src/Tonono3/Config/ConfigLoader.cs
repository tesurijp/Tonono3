using System;
using System.Collections.Generic;
using System.IO;
using Tonono3.AutoDefined;
using Tonono3.SKKEngine;
using tsr_di;
using VYaml.Annotations;
using VYaml.Serialization;

namespace Tonono3;

[YamlObject]
public partial record class RomajiTable(
    string Vowel,
    Dictionary<string, string[]> Rows,
    Dictionary<string, string> Irregular,
    Dictionary<string, List<string>> MoraModifier,
    Dictionary<string, string> MoraAutoComplete);

[YamlObject]
public partial record class Standard(char Start, char End, int Offset);

[YamlObject]
public partial record class ZenkakuTable(Standard Standard, Dictionary<string, string> Irregular);

[YamlObject]
public partial record class ConfigYaml(
    string[] DictionaryPaths,
    string UserDictionaryPath,
    RomajiTable RomajiTable,
    ZenkakuTable ZenkakuTable,
    string[] ViCompatibleApps,
    string CandidateSelectionKeys);

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class ConfigLoader( IConfigPathProvider paths, CompileConfigFunc compileConfig, WriteLogFunc writeLog)
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
        var path = paths.ConfigPath;
        writeLog($"Loading config from: {path}");
        try
        {
            var yaml = File.ReadAllBytes(paths.ConfigPath);
            var yamlObj = YamlSerializer.Deserialize<ConfigYaml>(yaml, serializerOptions);
            return compileConfig(
                Path.GetFullPath(path),
                Path.GetDirectoryName(path)!,
                yamlObj.DictionaryPaths ?? [],
                yamlObj.UserDictionaryPath ?? "",
                yamlObj.RomajiTable.Vowel ?? "",
                yamlObj.RomajiTable.Rows ?? [],
                yamlObj.RomajiTable.Irregular ?? [],
                yamlObj.RomajiTable.MoraModifier ?? [],
                yamlObj.RomajiTable.MoraAutoComplete ?? [],
                yamlObj.ZenkakuTable.Standard.Start,
                yamlObj.ZenkakuTable.Standard.End,
                yamlObj.ZenkakuTable.Standard.Offset,
                yamlObj.ZenkakuTable.Irregular ?? [],
                yamlObj.ViCompatibleApps ?? [],
                yamlObj.CandidateSelectionKeys);
        }
        catch (Exception ex)
        {
            writeLog($"Error loading config.yaml: {ex.Message}");
            throw;
        }
    }
}
