using System;
using System.IO;
using tsr_di;

namespace Tonono3;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class ConfigPathProvider : IConfigPathProvider
{
    public string ConfigFileName => "config.yaml";
    public string SystemConfigFolder { get; } = AppContext.BaseDirectory;
    public string UserConfigFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Tonono3");

    public string ConfigPath => HasUserConfig
        ? Path.Combine(UserConfigFolder, ConfigFileName)
        : Path.Combine(SystemConfigFolder, ConfigFileName);

    public string ConfigFolder => HasUserConfig ? UserConfigFolder : SystemConfigFolder;

    private bool HasUserConfig =>
        UserConfigEnabled && File.Exists(Path.Combine(UserConfigFolder, ConfigFileName));

#if DEBUG
    private const bool UserConfigEnabled = false;
#else
    private const bool UserConfigEnabled = true;
#endif
}
