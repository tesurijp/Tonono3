using System.Collections.Immutable;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Tonono3.AutoDefined;
using Tonono3.SKKEngine;

namespace Tonono3.Tests;

internal static class EngineFunctions
{
    internal static readonly CreateInitialStateFunc CreateInitialState = SkkEngineFacade.CreateInitialState;
    internal static readonly CreateKeyCommandFunc CreateKeyCommand = SkkEngineFacade.CreateKeyCommand;
    internal static readonly CompileConfigFunc CompileConfig = SkkEngineFacade.CompileConfig;
    internal static readonly LoadDictionaryFunc LoadDictionary = SkkEngineFacade.LoadDictionary;
    internal static readonly GetCandidatesFunc GetCandidates = SkkEngineFacade.GetCandidates;
    internal static readonly GetCompletionsFunc GetCompletions = SkkEngineFacade.GetCompletions;
    internal static readonly ProcessKeyFunc ProcessKey = SkkEngineFacade.ProcessKey;
    internal static readonly CreateUiSnapshotFunc CreateUiSnapshot = SkkEngineFacade.CreateUiSnapshot;
}

internal sealed class DummyLogger
{
    internal List<string> Messages { get; } = [];
    public void Log(string message) => Messages.Add(message);
}

internal sealed class StubConfigLoader(Func<AppConfig> reload)
{
    public AppConfig Reload() => reload();
}

internal sealed class StubDictionaryLoader(
    Func<IEnumerable<string>, string, DictionarySnapshot> load)
{
    public DictionarySnapshot Load(IEnumerable<string> mainPaths, string userPath) =>
        load(mainPaths, userPath);
}

internal sealed class FakeUserDictionaryWriter : IUserDictionaryWriter
{
    internal List<ImmutableDictionary<string, ImmutableArray<string>>> Values { get; } = [];
    internal int DisposeCount { get; private set; }

    public void Enqueue(ImmutableDictionary<string, ImmutableArray<string>> dictionary) =>
        Values.Add(dictionary);

    public void Dispose() => DisposeCount++;
}

internal sealed class FakeUserDictionaryWriterFactory
{
    internal List<(string Path, FakeUserDictionaryWriter Writer)> Created { get; } = [];

    public IUserDictionaryWriter Create(string path)
    {
        var writer = new FakeUserDictionaryWriter();
        Created.Add((path, writer));
        return writer;
    }
}

internal sealed class FakeConfigWatcher(AppConfig config, DictionarySnapshot dict) : IConfigWatcher
{
    public event Action<long, AppConfig, DictionarySnapshot>? RuntimeReloaded;
    internal int StartCount { get; private set; }
    internal int DisposeCount { get; private set; }

    public void Start() => StartCount++;
    public void Dispose() => DisposeCount++;

    internal void Publish(long generation, AppConfig config, DictionarySnapshot dictionary) =>
        RuntimeReloaded?.Invoke(generation, config, dictionary);

    public (AppConfig, DictionarySnapshot) LoadRuntime() => (config, dict);
}

internal sealed class FakeKeyboardHook : IKeyboardHook
{
    internal Func<int, bool>? func;
    internal int InstallCount { get; private set; }
    internal int DisposeCount { get; private set; }

    public void Install(Func<int, bool> KeyIntercepted)
    {
        func = KeyIntercepted;
        InstallCount++;
    }
    public void Dispose() => DisposeCount++;
    internal bool? Publish(int value) => func?.Invoke(value);
}

internal sealed class FakeKeyboardInput(IKeyboardHook hook) : ISkkKeyHandler
{

    public void Start(Func<KeyCommand, string?, bool> process)
    {
    }

    public void Dispose()
    {
        hook.Dispose();
    }
}

internal sealed class FakeActiveProcess
{
    public string GetActiveProcessPath() => "";
}

internal sealed class FakeEffectDispatcher(
    FakeUserDictionaryWriterFactory writerFactory) : IEngineEffectDispatcher
{
    internal List<TransitionResult> Results { get; } = [];
    private string? userDictionaryPath;
    private IUserDictionaryWriter? dictionaryWriter;

    public void ApplyUserDictionaryPath(string path)
    {
        if (string.Equals(userDictionaryPath, path, StringComparison.Ordinal))
        {
            return;
        }

        dictionaryWriter?.Dispose();
        dictionaryWriter = writerFactory.Create(path);
        userDictionaryPath = path;
    }

    public void Execute(TransitionResult result)
    {
        Results.Add(result);
    }

    public void Dispose()
    {
        dictionaryWriter?.Dispose();
    }
}

internal sealed class TestConfigPathProvider : IConfigPathProvider
{
    internal TestConfigPathProvider(string root)
    {
        SystemConfigFolder = Path.Combine(root, "system");
        UserConfigFolder = Path.Combine(root, "user");
        Directory.CreateDirectory(SystemConfigFolder);
        Directory.CreateDirectory(UserConfigFolder);
        File.WriteAllText(ConfigPath, "# test");
    }

    public string ConfigFileName => "config.yaml";
    public string ConfigPath => Path.Combine(SystemConfigFolder, ConfigFileName);
    public string ConfigFolder => SystemConfigFolder;
    public string SystemConfigFolder { get; }
    public string UserConfigFolder { get; }
}

internal sealed class ControllerTestContext : IDisposable
{
    internal ControllerTestContext(AppConfig config, DictionarySnapshot dictionary)
    {
        Watcher = new FakeConfigWatcher(config, dictionary);
        Hook = new FakeKeyboardHook();
        WriterFactory = new FakeUserDictionaryWriterFactory();
        EffectDispatcher = new FakeEffectDispatcher(WriterFactory);
        var keyboard = new FakeKeyboardInput(Hook);
        var session = new SkkEngineSession(
            EngineFunctions.CreateInitialState,
            EngineFunctions.ProcessKey,
            EngineFunctions.CreateUiSnapshot,
            EffectDispatcher);
        Controller = new SkkController(Watcher, keyboard, session);
    }

    internal SkkController Controller { get; }
    internal FakeConfigWatcher Watcher { get; }
    internal FakeKeyboardHook Hook { get; }
    internal FakeUserDictionaryWriterFactory WriterFactory { get; }
    internal FakeEffectDispatcher EffectDispatcher { get; }

    public void Dispose() => Controller.Dispose();
}

internal sealed class FakeApplicationControl
{
    internal int InitializeCount { get; private set; }
    public void Initialize(IControlledApplicationLifetime? lifetime) => InitializeCount++;
    public void Restart() { }
    public void Shutdown() { }
}

internal sealed class FakeTononoUi : ITononoUi
{
    public WindowIcon? Icon { get; set; }
    internal int CloseCount { get; private set; }
    internal List<SkkUiSnapshot> Snapshots { get; } = [];
    public void ApplySnapshot(SkkUiSnapshot snapshot) => Snapshots.Add(snapshot);
    public void Close() => CloseCount++;
}

internal sealed class FakeTononoUiFactory(FakeTononoUi ui)
{
    public ITononoUi Create() => ui;
}

internal sealed class FakeSystemMenu : ISystemMenu
{
    internal int DisposeCount { get; private set; }
    public void Dispose() => DisposeCount++;
}

internal sealed class FakeSystemMenuFactory(FakeSystemMenu menu)
{
    public ISystemMenu Create() => menu;
}

internal sealed class FakeWindowIconProvider
{
    public WindowIcon Load() => null!;
}
