using Avalonia.Controls;
using System;
using Tonono3.AutoDefined;

namespace Tonono3.UI;

public sealed class SystemMenu : ISystemMenu
{
    private readonly TrayIcon trayicon;
    private Window? infoWindow;
    private readonly ISkkController controller;
    private readonly ICreateInfoWindow createInfoWindow;

    public SystemMenu(
        IRestartApplication restartApplication,
        IShutdownApplication shutdownApplication,
        ISkkController controller,
        IOpenConfigFile openConfigFile,
        ICreateInfoWindow createInfoWindow,
        ILoadWindowIcon loadWindowIcon)
    {
        this.controller = controller;
        this.createInfoWindow = createInfoWindow;
        trayicon = new TrayIcon
        {
            Icon = loadWindowIcon(),
            ToolTipText = "Tonono",
            Menu = CreateMenu(
                () => restartApplication(),
                () => shutdownApplication(),
                () => openConfigFile()),
            IsVisible = true
        };
    }

    private NativeMenu CreateMenu(
        Action restartAction,
        Action shutdownAction,
        Action openConfigAction)
    {
        var menu = new NativeMenu
        {
            makeMenu("情報", ShowInfoWindow),
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

    private void ShowInfoWindow()
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
