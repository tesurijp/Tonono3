using System;
using System.Threading;
using tsr_di;

namespace Tonono3.SKKEngine;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class SkkController( IConfigWatcher configWatcher, ISkkKeyHandler keyHandler, ISkkEngineSession skkEngineSession) : ISkkController
{
    private static readonly Lock gate = new();
    private PendingRuntime? pendingRuntime;
    private bool started;
    private bool disposed;
    private long uiVersion;

    public event Action<SkkUiSnapshot>? UiUpdated;

    private static T GetField<T>(Func<T> getter)
    {
        lock (gate)
        {
            return getter();
        }
    }
    public SkkUiSnapshot CurrentUiSnapshot => GetField(() => skkEngineSession.CreateUiSnapshot(uiVersion));
    public AppConfig CurrentConfig => GetField(() => skkEngineSession.CurrentConfig);
    public void Start()
    {
        lock (gate)
        {
            if (started || disposed)
            {
                return;
            }
            started = true;
            configWatcher.RuntimeReloaded += OnRuntimeReloaded;
        }

        var (currentConfig, dictionary) = configWatcher.LoadRuntime();
        skkEngineSession.ApplyRuntime(currentConfig, dictionary);
        configWatcher.Start();
        keyHandler.Start(ProcessCommand);
    }

    private void OnRuntimeReloaded( long generation, AppConfig config, DictionarySnapshot dictionary)
    {
        lock (gate)
        {
            if (disposed || pendingRuntime is not null && pendingRuntime.Generation > generation)
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
            UiUpdated?.Invoke(skkEngineSession.CreateUiSnapshot(++uiVersion));
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
            configWatcher.RuntimeReloaded -= OnRuntimeReloaded;
            UiUpdated = null;
        }

        keyHandler.Dispose();
        configWatcher.Dispose();
        skkEngineSession.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed record PendingRuntime(
        long Generation,
        AppConfig Config,
        DictionarySnapshot Dictionary);
}
