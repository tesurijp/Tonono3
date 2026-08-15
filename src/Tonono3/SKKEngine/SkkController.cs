using System;
using System.Threading;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SKKEngine;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class SkkController : ISkkController
{
    private readonly Lock gate = new();
    private readonly CreateUserDictionaryWriterFunc createUserDictionaryWriter;
    private readonly IConfigWatcher configWatcher;
    private readonly ExecuteEngineEffectsFunc executeEngineEffectsOrg;
    private Action<TransitionResult> executeEngineEffects;
    private readonly ISkkKeyHandler keyHandler;
    private readonly CreateConfigFunc createConfig;
    private readonly CreateDictionaryFunc createDictionary;
    private readonly ProcessKeyFunc processKey;
    private readonly CreateUiSnapshotFunc createUiSnapshot;
    private EngineState currentState;
    private DictionarySnapshot currentDictionary;
    private EngineConfig currentEngineConfig;
    private AppConfig currentConfig;
    private PendingRuntime? pendingRuntime;
    private IUserDictionaryWriter dictionaryWriter;
    private bool started;
    private bool disposed;
    private long uiVersion;

    public SkkController(
        ReloadConfigFunc reloadConfig,
        LoadSkkDictionaryFunc loadSkkDictionary,
        CreateUserDictionaryWriterFunc createUserDictionaryWriter,
        IConfigWatcher configWatcher,
        ISkkKeyHandler keyHandler,
        ExecuteEngineEffectsFunc executeEngineEffects,
        CreateInitialStateFunc createInitialState,
        CreateConfigFunc createConfig,
        CreateDictionaryFunc createDictionary,
        ProcessKeyFunc processKey,
        CreateUiSnapshotFunc createUiSnapshot)
    {
        this.createUserDictionaryWriter = createUserDictionaryWriter;
        this.configWatcher = configWatcher;
        this.executeEngineEffectsOrg = executeEngineEffects;
        this.keyHandler = keyHandler;
        this.createConfig = createConfig;
        this.createDictionary = createDictionary;
        this.processKey = processKey;
        this.createUiSnapshot = createUiSnapshot;

        currentConfig = reloadConfig();
        var dictionary = loadSkkDictionary(
            currentConfig.DictionaryPaths,
            currentConfig.UserDictionaryPath);
        currentDictionary = ToEngineDictionary(dictionary);
        currentEngineConfig = ToEngineConfig(currentConfig);
        currentState = createInitialState();
        dictionaryWriter = createUserDictionaryWriter(currentConfig.UserDictionaryPath);
        this.executeEngineEffects = executeEngineEffectsOrg.Bind(dictionaryWriter);
    }

    public event Action<SkkUiSnapshot>? UiUpdated;

    public SkkUiSnapshot CurrentUiSnapshot
    {
        get
        {
            lock (gate)
            {
                return ToUiSnapshot(currentState, uiVersion);
            }
        }
    }

    public AppConfig CurrentConfig
    {
        get
        {
            lock (gate)
            {
                return currentConfig;
            }
        }
    }

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

        configWatcher.Start();
        keyHandler.Start(ProcessCommand);
    }

    private void OnRuntimeReloaded(
        long generation,
        AppConfig config,
        SkkDictionarySnapshot dictionary)
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
            var result = processKey(
                currentState,
                currentEngineConfig,
                currentDictionary,
                command,
                activeProcessPath!);
            currentState = result.State;
            currentDictionary = result.Dictionary;
            executeEngineEffects(result);
            UiUpdated?.Invoke(ToUiSnapshot(result.State, ++uiVersion));
            return result.IsHandled;
        }
    }

    private void ApplyPendingRuntime()
    {
        if (pendingRuntime is null)
        {
            return;
        }

        var pending = pendingRuntime;
        pendingRuntime = null;
        if (!string.Equals(
                currentConfig.UserDictionaryPath,
                pending.Config.UserDictionaryPath,
                StringComparison.Ordinal))
        {
            dictionaryWriter.Dispose();
            dictionaryWriter = createUserDictionaryWriter(pending.Config.UserDictionaryPath);
            executeEngineEffects = executeEngineEffectsOrg.Bind(dictionaryWriter);
        }
        currentConfig = pending.Config;
        currentEngineConfig = ToEngineConfig(pending.Config);
        currentDictionary = ToEngineDictionary(pending.Dictionary);
    }

    private EngineConfig ToEngineConfig(AppConfig config) =>
        createConfig(
            config.RomajiTable,
            config.MoraModifier,
            config.MoraAutoComplete,
            config.ZenkakuTable,
            config.ViCompatibleApps);

    private DictionarySnapshot ToEngineDictionary(SkkDictionarySnapshot dictionary) =>
        createDictionary(dictionary.Main, dictionary.User);

    private SkkUiSnapshot ToUiSnapshot(EngineState state, long version)
    {
        var snapshot = createUiSnapshot(state);
        return new(
            version,
            snapshot.IsVisible,
            snapshot.StatusText,
            snapshot.IsInRegistrationMode,
            snapshot.RegistrationReading,
            snapshot.RegistrationWord,
            snapshot.Composition,
            snapshot.CandidateList);
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
        dictionaryWriter.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed record PendingRuntime(
        long Generation,
        AppConfig Config,
        SkkDictionarySnapshot Dictionary);
}
