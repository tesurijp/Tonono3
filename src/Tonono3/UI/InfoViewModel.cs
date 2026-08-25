using System.Collections.Generic;
using Tonono3.SKKEngine;

namespace Tonono3.UI;

public record class InfoViewModel(AppConfig Config, string ConfigPath)
{
    public IEnumerable<KeyValuePair<string, string>> Romaji => Config.RomajiEntries;
    public IEnumerable<KeyValuePair<string, string>> Mora => Config.MoraEntries;
    public IEnumerable<KeyValuePair<string, string>> MoraComplete => Config.MoraCompleteEntries;
    public IEnumerable<KeyValuePair<char, string>> Zenkaku => Config.ZenkakuEntries;
    public static string VersionInfo => $"Version {BuildInfo.Version} ( SkkEngine version {SkkEngine.BuildInfo.Version}; Interop version {Interop.BuildInfo.Version} )";
}
