using System;
using System.IO;
using tsr_di;

namespace Tonono3;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class ConfigPathProvider : IConfigPathProvider
{
    public string ConfigFileName => "config.yaml";
    public string SystemConfigFolder => AppContext.BaseDirectory;
    public string UserConfigFolder => Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tonono3");
    public string ConfigPath => Path.Combine(ConfigFolder, ConfigFileName);
#if DEBUG
    public string ConfigFolder => SystemConfigFolder;
#else
    private bool HasUserConfig => File.Exists(Path.Combine(UserConfigFolder, ConfigFileName));
    public string ConfigFolder => HasUserConfig ? UserConfigFolder : SystemConfigFolder;
#endif
}
