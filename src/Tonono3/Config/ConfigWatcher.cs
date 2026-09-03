using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tonono3.AutoDefined;
using Tonono3.SKKEngine;
using tsr_di;

namespace Tonono3;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class ConfigWatcher(
    IConfigPathProvider paths,
    ReloadConfigFunc reloadConfig,
    LoadSkkDictionaryFunc loadSkkDictionary,
    WriteLogFunc writeLog) : IConfigWatcher
{
    private readonly Lock gate = new();
    private readonly TimeSpan debounceDelay = TimeSpan.FromMilliseconds(400);
    private FileSystemWatcher? systemWatcher;
    private FileSystemWatcher? userWatcher;
    private CancellationTokenSource? debounceCancellation;
    private long generation;
    private bool started;
    private bool disposed;
    private string? lastLoadedPath = Path.GetFullPath(paths.ConfigPath);

    private Action<long, AppConfig, DictionarySnapshot> RuntimeReloaded = (_, _, _) => { };
    public void RegisterCallback(Action<long, AppConfig, DictionarySnapshot> reload) => RuntimeReloaded = reload;
    public void Start()
    {
        lock (gate)
        {
            if (started || disposed)
            {
                return;
            }
            systemWatcher = CreateWatcher(paths.SystemConfigFolder);
            userWatcher = CreateWatcher(paths.UserConfigFolder);
            started = true;
        }
    }

    public (AppConfig, DictionarySnapshot) LoadRuntime()
    {
        var config = reloadConfig();
        return (config, loadSkkDictionary(config.DictionaryPaths, config.UserDictionaryPath));
    }

    private FileSystemWatcher CreateWatcher(string folder)
    {
        Directory.CreateDirectory(folder);
        var watcher = new FileSystemWatcher(folder, paths.ConfigFileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            IncludeSubdirectories = false
        };
        watcher.Created += OnChanged;
        watcher.Changed += OnChanged;
        watcher.Renamed += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (e is RenamedEventArgs { OldFullPath: var oldpath })
        {
            if (IsSameLastLoaded(oldpath) && !IsSameLastLoaded(e.FullPath))
            {
                writeLog($"Last loaded config file was renamed: {oldpath}");
            }
        }
        if (e.ChangeType == WatcherChangeTypes.Deleted && IsSameLastLoaded(e.FullPath))
        {
            writeLog($"Last loaded config file was deleted: {e.FullPath}");
        }
        ScheduleReload();
    }
    private bool IsSameLastLoaded(string path) => string.Equals(Path.GetFullPath(path), lastLoadedPath, StringComparison.OrdinalIgnoreCase);
    internal void ScheduleReload()
    {
        CancellationToken token;
        long scheduledGeneration;
        lock (gate)
        {
            if (disposed)
            {
                return;
            }
            scheduledGeneration = ++generation;
            debounceCancellation?.Cancel();
            debounceCancellation?.Dispose();
            debounceCancellation = new CancellationTokenSource();
            token = debounceCancellation.Token;
        }
        _ = ReloadAfterDelayAsync(scheduledGeneration, token);
    }

    private async Task ReloadAfterDelayAsync(long scheduledGeneration, CancellationToken token)
    {
        try
        {
            await Task.Delay(debounceDelay, token).ConfigureAwait(false);
            if (!File.Exists(paths.ConfigPath))
            {
                writeLog($"The last loaded config file does not exist. {paths.ConfigPath}");
                return;
            }
            var (conf, dict) = await Task.Run(LoadRuntime, token).ConfigureAwait(false);
            lock (gate)
            {
                if (disposed || token.IsCancellationRequested || scheduledGeneration != generation)
                {
                    return;
                }
                lastLoadedPath = conf.Path;
            }
            RuntimeReloaded(scheduledGeneration, conf, dict);
            writeLog("config.yaml update success.");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            writeLog($"Failed to reload config.yaml: {ex.Message}");
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
            ++generation;
            debounceCancellation?.Cancel();
        }
        DisposeWatcher(systemWatcher);
        DisposeWatcher(userWatcher);
        lock (gate)
        {
            debounceCancellation?.Dispose();
            debounceCancellation = null;
        }
        GC.SuppressFinalize(this);
    }

    private static void DisposeWatcher(FileSystemWatcher? watcher)
    {
        watcher?.EnableRaisingEvents = false;
        watcher?.Dispose();
    }
}
