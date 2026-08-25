using Avalonia.Controls;
using System;
using Tonono3.AutoDefined;

namespace Tonono3.UI;

public sealed class SystemMenu : ISystemMenu
{
    private readonly TrayIcon trayicon;
    private Window? infoWindow;
    private readonly Action showInfo;

    public SystemMenu(
        RestartApplicationFunc restart,
        ShutdownApplicationFunc shutdown,
        ISkkController controller,
        OpenConfigFileFunc openConfigFile,
        CreateInfoWindowFunc createInfoWindow,
        WindowIcon appIcon)
    {
        showInfo = () => ShowInfoWindow(createInfoWindow, controller);
        trayicon = new TrayIcon
        {
            Icon = appIcon,
            ToolTipText = "Tonono",
            Menu = CreateMenu(
                showInfo,
                () => restart(),
                () => shutdown(),
                () => openConfigFile()),
            IsVisible = true
        };
    }

    private static NativeMenu CreateMenu(
        Action showInfo,
        Action restartAction,
        Action shutdownAction,
        Action openConfigAction)
    {
        var menu = new NativeMenu
        {
            makeMenu("情報", showInfo),
            makeMenu("設定", openConfigAction),
            makeMenu("再起動", restartAction),
            new NativeMenuItemSeparator(),
            makeMenu("終了", shutdownAction)
        };
        return menu;
        static NativeMenuItem makeMenu(string header, Action act)
        {
            var menu = new NativeMenuItem { Header = header };
            menu.Click += (_, _) => act();
            return menu;
        }
    }

    private void ShowInfoWindow(CreateInfoWindowFunc createInfoWindow, ISkkController controller)
    {
        if (infoWindow is { IsVisible: true })
        {
            infoWindow.Activate();
        }
        else
        {
            infoWindow = createInfoWindow(controller.CurrentConfig);
            infoWindow.Closed += (_, _) => infoWindow = null;
            infoWindow.Show();
        }
    }

    public void Dispose()
    {
        infoWindow?.Close();
        trayicon.IsVisible = false;
        trayicon.Dispose();
    }
}
