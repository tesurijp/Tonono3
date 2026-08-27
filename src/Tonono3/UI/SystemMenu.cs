using Avalonia.Controls;
using Tonono3.AutoDefined;

namespace Tonono3.UI;

public sealed class SystemMenu : ISystemMenu
{
    private readonly TrayIcon trayicon;
    private Window? infoWindow;
    public SystemMenu(
        GetAppConfigFunc getAppConfig,
        WindowIcon appIcon,
        ExecUiActionFunc restart,
        ExecUiActionFunc shutdown,
        ExecUiActionFunc openConfigFile,
        CreateInfoWindowFunc createInfoWindow)
    {
        trayicon = new TrayIcon
        {
            Icon = appIcon,
            ToolTipText = "Tonono",
            Menu = [
                makeMenu("情報",  () => ShowInfoWindow(createInfoWindow, getAppConfig)),
                makeMenu("設定", openConfigFile),
                makeMenu("再起動", restart),
                new NativeMenuItemSeparator(),
                makeMenu("終了", shutdown)
            ],
            IsVisible = true
        };

        static NativeMenuItem makeMenu(string header, ExecUiActionFunc act)
        {
            var menu = new NativeMenuItem(header);
            menu.Click += (_, _) => act();
            return menu;
        }
    }

    private void ShowInfoWindow(CreateInfoWindowFunc createInfoWindow, GetAppConfigFunc getAppConfig)
    {
        if (infoWindow is { IsVisible: true })
        {
            infoWindow.Activate();
        }
        else
        {
            infoWindow = createInfoWindow(getAppConfig());
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
