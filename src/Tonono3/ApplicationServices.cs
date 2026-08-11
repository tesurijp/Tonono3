using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Diagnostics;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3;

[ServiceClass(Lifetime = Lifetime.Scoped)]
public sealed class ApplicationControl(IWriteLog writeLog)
{
    private IControlledApplicationLifetime? lifetime;

    [ServiceFunction(ServiceName = "InitializeApplicationLifetime")]
    public void Initialize(IControlledApplicationLifetime? lifetime) => this.lifetime = lifetime;

    [ServiceFunction(ServiceName = "RestartApplication")]
    public void Restart()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Environment.ProcessPath!,
                UseShellExecute = false,
                WorkingDirectory = AppContext.BaseDirectory,
                CreateNoWindow = true,
                Arguments = Program.RestartArgument
            });
        }
        catch (Exception ex)
        {
            writeLog($"Failed to restart application: {ex.Message}");
            return;
        }

        Shutdown();
    }

    [ServiceFunction(ServiceName = "ShutdownApplication")]
    public void Shutdown() => lifetime?.Shutdown();
}

[ServiceClass(Lifetime = Lifetime.Scoped)]
public sealed class ApplicationCoordinator(
    ISkkController controller,
    ICreateTononoUi createTononoUi,
    ICreateSystemMenu createSystemMenu,
    IInitializeApplicationLifetime initializeApplicationLifetime,
    ILoadWindowIcon loadWindowIcon) : IApplicationCoordinator
{
    private ITononoUi? ui;
    private ISystemMenu? menu;
    private bool disposed;

    public void Start(IControlledApplicationLifetime? controlledLifetime)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        initializeApplicationLifetime(controlledLifetime);

        ui = createTononoUi();
        ui.Icon = loadWindowIcon();
        controller.UiUpdated += ui.ApplySnapshot;
        menu = createSystemMenu();
        controller.Start();
        ui.ApplySnapshot(controller.CurrentUiSnapshot);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (ui is not null) controller.UiUpdated -= ui.ApplySnapshot;
        menu?.Dispose();
        ui?.Close();
        controller.Dispose();
        GC.SuppressFinalize(this);
    }
}
