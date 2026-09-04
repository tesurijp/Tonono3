using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Diagnostics;
using Tonono3.AutoDefined;
using Tonono3.SkkEngine;
using tsr_di;

namespace Tonono3.UI;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class UiFactory(
    IConfigPathProvider paths,
    GetTargetWindowPositionFunc getTargetWindowPosition,
    SetNonActiveWindowFunc setNonActiveWindow,
    [FromNamed("RestartApplication")] ExecUiActionFunc restart,
    [FromNamed("ShutdownApplication")] ExecUiActionFunc shutdown,
    [FromNamed("KeyHookEnable")] ExecUiActionFunc enableHook,
    [FromNamed("KeyHookDisable")] ExecUiActionFunc disableHook,
    KeyHookStateFunc getHookState,
    GetAppConfigFunc getAppConfig,
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
    public ISystemMenu CreateSystemMenu() => new SystemMenu(getAppConfig, icon, restart, shutdown, enableHook, disableHook, OpenConfigFile, getHookState, CreateInfoWindow);

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
