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
    private string? activeConfigPath = Path.GetFullPath(paths.ConfigPath);

    public event Action<long, AppConfig, SkkDictionarySnapshot>? RuntimeReloaded;

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
            activeConfigPath = Path.GetFullPath(paths.ConfigPath);
            started = true;
        }
    }

    public (AppConfig, SkkDictionarySnapshot) LoadRuntime()
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
        watcher.Renamed += OnRenamed;
        watcher.Deleted += OnChanged;
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Deleted && IsActiveConfigPath(e.FullPath))
        {
            writeLog($"Active config file was deleted: {e.FullPath}");
            return;
        }
        ScheduleReload();
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (IsActiveConfigPath(e.OldFullPath) && !IsActiveConfigPath(e.FullPath))
        {
            writeLog($"Active config file was renamed: {e.OldFullPath}");
            return;
        }
        ScheduleReload();
    }

    private bool IsActiveConfigPath(string path) =>
        activeConfigPath is not null &&
        string.Equals(Path.GetFullPath(path), activeConfigPath, StringComparison.OrdinalIgnoreCase);

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
            var expectedConfigPath = activeConfigPath;
            if (expectedConfigPath is not null && !File.Exists(expectedConfigPath))
            {
                throw new FileNotFoundException("The active config file does not exist.", expectedConfigPath);
            }
            var (conf, dict) = await Task.Run(LoadRuntime, token).ConfigureAwait(false);
            lock (gate)
            {
                if (disposed || token.IsCancellationRequested || scheduledGeneration != generation)
                {
                    return;
                }
                if (systemWatcher is not null)
                {
                    activeConfigPath = Path.GetFullPath(paths.ConfigPath);
                }
            }
            RuntimeReloaded?.Invoke(scheduledGeneration, conf, dict);
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
            RuntimeReloaded = null;
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
