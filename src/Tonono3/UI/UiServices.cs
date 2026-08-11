using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Diagnostics;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.UI;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class ConfigFileLauncher(
    IConfigPathProvider paths,
    IWriteLog writeLog) 
{
    [ServiceFunction(ServiceName = "OpenConfigFile")]
    public void Open()
    {
        try
        {
            Process.Start(new ProcessStartInfo(paths.ConfigPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            writeLog($"Failed to open config.yaml: {ex.Message}");
        }
    }
}

public static class WindowIconProvider
{
    [ServiceFunction(ServiceName = "LoadWindowIcon")]
    public static WindowIcon Load()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Tonono3/TONONO.ICO"));
        return new WindowIcon(stream);
    }
}

[ServiceClass(Lifetime = Lifetime.Scoped)]
public sealed class InfoWindowFactory(
    ILoadWindowIcon loadWindowIcon,
    IConfigPathProvider paths)
{
    [ServiceFunction(ServiceName = "CreateInfoWindow")]
    public Window Create(AppConfig config) => new InfoWindow
    {
        Icon = loadWindowIcon(),
        DataContext = new InfoViewModel(config, paths.ConfigPath)
    };
}

[ServiceClass(Lifetime = Lifetime.Scoped)]
public sealed class TononoUiFactory(
    IGetTargetWindowPosition getTargetWindowPosition,
    ISetNonActiveWindow setNonActiveWindow)
{
    [ServiceFunction(ServiceName = "CreateTononoUi")]
    public ITononoUi Create() => new TononoUI(getTargetWindowPosition, setNonActiveWindow);
}

[ServiceClass(Lifetime = Lifetime.Scoped)]
public sealed class SystemMenuFactory(
    IRestartApplication restartApplication,
    IShutdownApplication shutdownApplication,
    ISkkController controller,
    IOpenConfigFile openConfigFile,
    ICreateInfoWindow createInfoWindow,
    ILoadWindowIcon loadWindowIcon)
{
    [ServiceFunction(ServiceName = "CreateSystemMenu")]
    public ISystemMenu Create() => new SystemMenu(
        restartApplication,
        shutdownApplication,
        controller,
        openConfigFile,
        createInfoWindow,
        loadWindowIcon);
}
