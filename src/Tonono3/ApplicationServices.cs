using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Diagnostics;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class ApplicationControl(WriteLogFunc writeLog)
{
    private IControlledApplicationLifetime? lifetime;

    [ServiceFunction(ServiceName = "InitializeApplicationLifetimeFunc")]
    public void Initialize(IControlledApplicationLifetime? lifetime) => this.lifetime = lifetime;

    [ServiceFunction(ServiceType =typeof(ExecUiActionFunc), Name = "RestartApplication")]
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

    [ServiceFunction(ServiceType =typeof(ExecUiActionFunc), Name = "ShutdownApplication")]
    public void Shutdown() => lifetime?.Shutdown();
}

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class ApplicationCoordinator(
    ISkkController controller,
     CreateTononoUiFunc createTononoUi,
     CreateSystemMenuFunc createSystemMenu,
    [FromNamed("KeyHookEnable")] ExecUiActionFunc enableHook,
    InitializeApplicationLifetimeFunc initializeApplicationLifetime) : IApplicationCoordinator
{
    private ITononoUi? ui;
    private ISystemMenu? menu;
    private bool disposed;

    public void Start(IControlledApplicationLifetime? controlledLifetime)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        initializeApplicationLifetime(controlledLifetime);

        ui = createTononoUi();
        controller.UiUpdated += ui.ApplySnapshot;
        menu = createSystemMenu();
        var snapshot = controller.Start();
        enableHook();
        ui.ApplySnapshot(snapshot);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        if (ui is not null)
        {
            controller.UiUpdated -= ui.ApplySnapshot;
        }
        menu?.Dispose();
        ui?.Close();
        controller.Dispose();
        GC.SuppressFinalize(this);
    }
}
