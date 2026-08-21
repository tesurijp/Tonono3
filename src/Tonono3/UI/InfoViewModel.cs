using System.Collections.Generic;
using System.Linq;

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

public class InfoViewModel(AppConfig cfg, string configPath)
{
    public static string VersionInfo => $"Version {BuildInfo.Version} ( SkkEngine version {SkkEngine.BuildInfo.Version}; Interop version {Interop.BuildInfo.Version} )";
    public string ConfigPath => configPath;
    public IEnumerable<InfoStringPairRow> RomajiEntries => cfg.RomajiTable
        .Select(kv => new InfoStringPairRow { Key = kv.Key, Value = kv.Value });
    public IEnumerable<InfoCharStringPairRow> ZenkakuEntries => cfg.ZenkakuTable
        .Select(kv => new InfoCharStringPairRow { Key = kv.Key, Value = kv.Value });
    public IEnumerable<InfoStringPairRow> MoraModifierEntries => cfg.MoraModifier
        .Select(kv => new InfoStringPairRow { Key = kv.Key, Value = kv.Value });
    public IEnumerable<InfoStringPairRow> MoraAutoCompleteEntries => cfg.MoraAutoComplete
        .Select(kv => new InfoStringPairRow { Key = kv.Key, Value = kv.Value });
    public IEnumerable<string> DictionaryPaths => cfg.DictionaryPaths;
    public IEnumerable<string> ViCompatibleApps => cfg.ViCompatibleApps;
}
