using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Diagnostics;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.UI;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class UiFactory(
    IConfigPathProvider paths,
    GetTargetWindowPositionFunc getTargetWindowPosition,
    SetNonActiveWindowFunc setNonActiveWindow,
    RestartApplicationFunc restart,
    ShutdownApplicationFunc shutdown,
    ISkkController controller,
    WriteLogFunc writeLog)
{
    static UiFactory()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Tonono3/TONONO.ICO"));
        icon = new WindowIcon(stream);
    }
    private static readonly WindowIcon icon;
    [ServiceFunction]
    public Window CreateInfoWindow(AppConfig config) => new InfoWindow { Icon = icon, DataContext = new InfoViewModel(config, paths.ConfigPath) };
    [ServiceFunction]
    public ITononoUi CreateTononoUi() => new TononoUI(getTargetWindowPosition, setNonActiveWindow) { Icon = icon };

    [ServiceFunction]
    public ISystemMenu CreateSystemMenu() => new SystemMenu(restart, shutdown, controller, OpenConfigFile, CreateInfoWindow, icon);

    [ServiceFunction]
    public void OpenConfigFile()
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
