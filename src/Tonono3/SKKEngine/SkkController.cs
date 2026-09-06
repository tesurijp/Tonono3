using System;
using System.Threading;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SkkEngine;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class SkkController( IConfigWatcher configWatcher, IKeyboardHook keyHandler, ISkkEngineSession skkEngineSession,
     CreateTononoUiFunc createTononoUi, CreateSystemMenuFunc createSystemMenu ) : ISkkController
{
    private static readonly Lock gate = new();
    private ITononoUi? ui;
    private ISystemMenu? menu;
    private PendingRuntime? pendingRuntime;
    private bool started;
    private bool disposed;
    private long uiVersion;
    public void Start()
    {
        lock (gate)
        {
            if (started || disposed)
            {
                throw new InvalidOperationException();
            }
            started = true;

            configWatcher.RegisterCallback(OnRuntimeReloaded);
            var (currentConfig, dictionary) = configWatcher.LoadRuntime();
            configWatcher.Start();
            keyHandler.RegisterCallback(ProcessCommand);
            keyHandler.Install();
            ui = createTononoUi();
            menu = createSystemMenu();
            skkEngineSession.ApplyRuntime(currentConfig, dictionary);
            var snapshot = skkEngineSession.CreateUiSnapshot(uiVersion);
            ui.ApplySnapshot(snapshot);
            menu.ApplySnapshot(snapshot);
        }
    }
    private void OnRuntimeReloaded(long generation, AppConfig config, DictionarySnapshot dictionary)
    {
        lock (gate)
        {
            if (disposed || (pendingRuntime?.Generation ?? -1) > generation)
            {
                return;
            }
            pendingRuntime = new(generation, config, dictionary);
        }
    }
    public bool ProcessCommand(KeyCommand command, string? activeProcessPath)
    {
        lock (gate)
        {
            if (disposed)
            {
                return false;
            }
            ApplyPendingRuntime();
            var result = skkEngineSession.Process(command, activeProcessPath);
            var snapshot = skkEngineSession.CreateUiSnapshot(++uiVersion);
            ui?.ApplySnapshot(snapshot);
            menu?.ApplySnapshot(snapshot);
            return result.IsHandled;
        }
    }
    private void ApplyPendingRuntime()
    {
        lock (gate)
        {
            if (pendingRuntime is not null)
            {
                skkEngineSession.ApplyRuntime(pendingRuntime.Config, pendingRuntime.Dictionary);
            }
            pendingRuntime = null;
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
        }

        menu?.Dispose();
        ui?.Close();
        keyHandler.Dispose();
        configWatcher.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed record PendingRuntime(
        long Generation,
        AppConfig Config,
        DictionarySnapshot Dictionary);
}
