using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;

namespace Tonono3;

public partial class App(IApplicationCoordinator coordinator) : Application, IDisposable
{
    private bool disposed;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += (_, _) => Dispose();
            coordinator.Start(ApplicationLifetime as IControlledApplicationLifetime);
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        coordinator.Dispose();
        GC.SuppressFinalize(this);
    }
}
