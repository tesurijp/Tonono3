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
    private readonly IGetTargetWindowPosition getTargetWindowPosition;
    private readonly ISetNonActiveWindow setNonActiveWindow;
    private bool nativeStyleApplied;
    private long appliedVersion = -1;
    private TextBlock statusTextBlock = null!;
    private StackPanel registrationPanel = null!;
    private TextBlock registrationReadingTextBlock = null!;
    private TextBlock registrationWordTextBlock = null!;
    private TextBlock compositionTextBlock = null!;
    private TextBlock candidateListTextBlock = null!;

    public TononoUI(
        IGetTargetWindowPosition getTargetWindowPosition,
        ISetNonActiveWindow setNonActiveWindow)
    {
        this.getTargetWindowPosition = getTargetWindowPosition;
        this.setNonActiveWindow = setNonActiveWindow;
        InitializeComponent();
        statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock")!;
        registrationPanel = this.FindControl<StackPanel>("RegistrationPanel")!;
        registrationReadingTextBlock = this.FindControl<TextBlock>("RegistrationReadingTextBlock")!;
        registrationWordTextBlock = this.FindControl<TextBlock>("RegistrationWordTextBlock")!;
        compositionTextBlock = this.FindControl<TextBlock>("CompositionTextBlock")!;
        candidateListTextBlock = this.FindControl<TextBlock>("CandidateListTextBlock")!;

        Opened += (_, _) =>
        {
            ApplyNativeWindowStyles();
            UpdatePosition();
        };
        PositionChanged += (_, _) => ApplyNativeWindowStyles();
        Resized += (_, _) =>
        {
            if (IsVisible) UpdatePosition();
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public void ApplySnapshot(SkkUiSnapshot snapshot)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (snapshot.Version < appliedVersion) return;
            appliedVersion = snapshot.Version;
            statusTextBlock.Text = snapshot.StatusText;
            registrationPanel.IsVisible = snapshot.IsInRegistrationMode;
            registrationReadingTextBlock.Text = snapshot.RegistrationReading;
            registrationWordTextBlock.Text = snapshot.RegistrationWord;
            compositionTextBlock.Text = snapshot.Composition;
            candidateListTextBlock.Text = snapshot.CandidateList;

            if (snapshot.IsVisible)
            {
                if (!IsVisible) Show();
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
        if (nativeStyleApplied) return;
        setNonActiveWindow(this);
        nativeStyleApplied = true;
    }

    private void UpdatePosition()
    {
        var scale = RenderScaling;
        var (posX, posY) = getTargetWindowPosition(scale, scale, Bounds.Width, Bounds.Height);
        if (double.IsNaN(posX) || double.IsNaN(posY)) return;
        Position = new PixelPoint((int)Math.Round(posX * scale), (int)Math.Round(posY * scale));
    }
}
