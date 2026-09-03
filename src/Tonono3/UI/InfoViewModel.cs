using Tonono3.SKKEngine;

namespace Tonono3.UI;

public record class InfoViewModel(AppConfig Config, string ConfigPath)
{
    public static string VersionInfo => $"Version {BuildInfo.Version} ( SkkEngine version {SkkEngine.BuildInfo.Version}; Interop version {Interop.BuildInfo.Version} )";
}
