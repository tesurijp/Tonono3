using Avalonia.Controls;
using Avalonia.Platform;
using System;
using Tonono3.AutoDefined;
using Tonono3.SkkEngine;

namespace Tonono3.UI;

public sealed class SystemMenu : ISystemMenu
{
    private readonly TrayIcon trayicon;
    private static UiSnapshot? lastSnapshot;
    private static readonly WindowIcon trayIcon_hiragana;
    private static readonly WindowIcon trayIcon_katakana;
    private static readonly WindowIcon trayIcon_zenkaku;
    private static readonly WindowIcon trayIcon_direct;
    private static readonly WindowIcon trayIcon_disable;
    private Window? infoWindow;

    static SystemMenu()
    {
        trayIcon_hiragana = LoadIcon("avares://Tonono3/assets/TONONO_hiragana.ICO");
        trayIcon_katakana = LoadIcon("avares://Tonono3/assets/TONONO_katakana.ICO");
        trayIcon_zenkaku = LoadIcon("avares://Tonono3/assets/TONONO_zenkaku.ICO");
        trayIcon_direct = LoadIcon("avares://Tonono3/assets/TONONO_direct.ICO");
        trayIcon_disable = LoadIcon("avares://Tonono3/assets/TONONO_disable.ICO");
        static WindowIcon LoadIcon(string resourcePath)
        {
            using var stream = AssetLoader.Open(new Uri(resourcePath));
            return new WindowIcon(stream);
        }
    }

    public SystemMenu(
        GetAppConfigFunc getAppConfig,
        ExecUiActionFunc restart,
        ExecUiActionFunc shutdown,
        ExecUiActionFunc hookEnable,
        ExecUiActionFunc hookDisable,
        ExecUiActionFunc openConfigFile,
        KeyHookStateFunc getKeyHookState,
        CreateInfoWindowFunc createInfoWindow)
    {
        trayicon = new TrayIcon
        {
            Icon = trayIcon_direct,
            ToolTipText = "Tonono",
            Menu = [
                makeMenu("情報",  () => ShowInfoWindow(createInfoWindow, getAppConfig)),
                makeMenu("設定", openConfigFile),
                MakeEnablerItem(),
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

        NativeMenuItem MakeEnablerItem()
        {
            var menu = new NativeMenuItem("無効");
            menu.Click += (_, _) =>
            {
                var state = getKeyHookState();
                if (state)
                {
                    hookDisable();
                    menu.Header = "有効";
                    trayicon!.Icon = trayIcon_disable;
                }
                else
                {
                    hookEnable();
                    menu.Header = "無効";
                    ApplySnapshot(trayicon!, lastSnapshot);
                }
            };
            return menu;
        }
    }

    public void ApplySnapshot(UiSnapshot snapshot) => ApplySnapshot(trayicon, snapshot);

    private static void ApplySnapshot(TrayIcon tray, UiSnapshot? snapshot)
    {
        tray.Icon = (snapshot?.InputMode ?? InputMode.Direct) switch
        {
            InputMode.Hiragana => trayIcon_hiragana,
            InputMode.Katakana => trayIcon_katakana,
            InputMode.Zenkaku => trayIcon_zenkaku,
            InputMode.Direct => trayIcon_direct,
            _ => trayIcon_disable
        };

        lastSnapshot = snapshot;
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
