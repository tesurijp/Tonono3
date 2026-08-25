using System.Collections.Generic;
using System.Linq;
using Tonono3.SKKEngine;

namespace Tonono3.UI;

public sealed class InfoStringPairRow
{
    public required string Key { get; init; }
    public required string Value { get; init; }
}

public sealed class InfoCharStringPairRow
{
    public required char Key { get; init; }
    public required string Value { get; init; }
}

public class InfoViewModel
{
    public InfoViewModel(AppConfig config, string configPath)
    {
        ConfigPath = configPath;
        RomajiEntries = config.RomajiEntries
            .Select(kv => new InfoStringPairRow { Key = kv.Key, Value = kv.Value })
            .ToArray();
        ZenkakuEntries = config.ZenkakuEntries
            .Select(kv => new InfoCharStringPairRow { Key = kv.Key, Value = kv.Value })
            .ToArray();
        MoraModifierEntries = config.MoraEntries
            .Select(kv => new InfoStringPairRow { Key = kv.Key, Value = kv.Value })
            .ToArray();
        MoraAutoCompleteEntries = config.MoraCompleteEntries
            .Select(kv => new InfoStringPairRow { Key = kv.Key, Value = kv.Value })
            .ToArray();
        DictionaryPaths = config.DictionaryPaths;
        ViCompatibleApps = config.ViCompatibleApps;
    }

    public static string VersionInfo => $"Version {BuildInfo.Version} ( SkkEngine version {SkkEngine.BuildInfo.Version}; Interop version {Interop.BuildInfo.Version} )";
    public string ConfigPath { get; }
    public IEnumerable<InfoStringPairRow> RomajiEntries { get; }
    public IEnumerable<InfoCharStringPairRow> ZenkakuEntries { get; }
    public IEnumerable<InfoStringPairRow> MoraModifierEntries { get; }
    public IEnumerable<InfoStringPairRow> MoraAutoCompleteEntries { get; }
    public IEnumerable<string> DictionaryPaths { get; }
    public IEnumerable<string> ViCompatibleApps { get; }
}
