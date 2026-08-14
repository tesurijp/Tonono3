using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Tonono3.AutoDefined;
using Tonono3.SKKEngine;

namespace Tonono3.UI;

public partial class TononoUI : Window, ITononoUi
{
    private readonly GetTargetWindowPositionFunc getTargetWindowPosition;
    private readonly SetNonActiveWindowFunc setNonActiveWindow;
    private bool nativeStyleApplied;
    private long appliedVersion = -1;

    public TononoUI(
        GetTargetWindowPositionFunc getTargetWindowPosition,
        SetNonActiveWindowFunc setNonActiveWindow)
    {
        this.getTargetWindowPosition = getTargetWindowPosition;
        this.setNonActiveWindow = setNonActiveWindow;
        InitializeComponent();

        Opened += (_, _) =>
        {
            ApplyNativeWindowStyles();
            UpdatePosition();
        };
        PositionChanged += (_, _) => ApplyNativeWindowStyles();
        Resized += (_, _) =>
        {
            if (IsVisible)
            {
                UpdatePosition();
            }
        };
    }

    public void ApplySnapshot(SkkUiSnapshot snapshot)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (snapshot.Version < appliedVersion)
            {
                return;
            }
            appliedVersion = snapshot.Version;
            StatusTextBlock.Text = snapshot.StatusText;
            RegistrationPanel.IsVisible = snapshot.IsInRegistrationMode;
            RegistrationReadingTextBlock.Text = snapshot.RegistrationReading;
            RegistrationWordTextBlock.Text = snapshot.RegistrationWord;
            CompositionTextBlock.Text = snapshot.Composition;
            CandidateListTextBlock.Text = snapshot.CandidateList;

            if (snapshot.IsVisible)
            {
                if (!IsVisible)
                {
                    Show();
                }
                ApplyNativeWindowStyles();
                UpdatePosition();
            }
            else if (IsVisible)
            {
                Hide();
            }
        });
    }

    private void ApplyNativeWindowStyles()
    {
        if (nativeStyleApplied)
        {
            return;
        }

        var handle = TryGetPlatformHandle();
        if (handle != null)
        {
            setNonActiveWindow(handle.Handle);
            nativeStyleApplied = true;
        }
    }

    private void UpdatePosition()
    {
        var scale = RenderScaling;
        var (posX, posY) = getTargetWindowPosition(scale, scale, Bounds.Width, Bounds.Height);
        if (double.IsNaN(posX) || double.IsNaN(posY))
        {
            return;
        }
        Position = new PixelPoint((int)Math.Round(posX * scale), (int)Math.Round(posY * scale));
    }
}
