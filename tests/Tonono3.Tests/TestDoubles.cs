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
    internal static readonly CreateConfigFunc CreateConfig = SkkEngineFacade.CreateConfig;
    internal static readonly CreateDictionaryFunc CreateDictionary = SkkEngineFacade.CreateDictionary;
    internal static readonly ParseDictionaryLineFunc ParseDictionaryLine = SkkEngineFacade.ParseDictionaryLine;
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
    Func<IEnumerable<string>, string, SkkDictionarySnapshot> load)
{
    public SkkDictionarySnapshot Load(IEnumerable<string> mainPaths, string userPath) =>
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

internal sealed class FakeConfigWatcher : IConfigWatcher
{
    public event Action<long, AppConfig, SkkDictionarySnapshot>? RuntimeReloaded;
    internal int StartCount { get; private set; }
    internal int DisposeCount { get; private set; }

    public void Start() => StartCount++;
    public void Dispose() => DisposeCount++;

    internal void Publish(long generation, AppConfig config, SkkDictionarySnapshot dictionary) =>
        RuntimeReloaded?.Invoke(generation, config, dictionary);
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

internal sealed class FakeKeyboardInput
{
    public (bool Control, bool Shift) GetMetaKeyState() => (false, false);
    public char VkToChar(int vkCode, bool shift) => '\0';
}

internal sealed class FakeActiveProcess
{
    public string GetActiveProcessPath() => "";
}

internal sealed class FakeEffectExecutor
{
    internal List<TransitionResult> Results { get; } = [];
    public void Execute(IUserDictionaryWriter dictionaryWriter, TransitionResult result) =>
        Results.Add(result);
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
    internal ControllerTestContext(AppConfig config, SkkDictionarySnapshot dictionary)
    {
        Watcher = new FakeConfigWatcher();
        Hook = new FakeKeyboardHook();
        WriterFactory = new FakeUserDictionaryWriterFactory();
        EffectExecutor = new FakeEffectExecutor();
        var configLoader = new StubConfigLoader(() => config);
        var dictionaryLoader = new StubDictionaryLoader((_, _) => dictionary);
        var keyboard = new FakeKeyboardInput();
        var activeProcess = new FakeActiveProcess();
        Controller = new SkkController(
            configLoader.Reload,
            dictionaryLoader.Load,
            WriterFactory.Create,
            Watcher,
            Hook,
            keyboard.GetMetaKeyState,
            keyboard.VkToChar,
            activeProcess.GetActiveProcessPath,
            EffectExecutor.Execute,
            EngineFunctions.CreateInitialState,
            EngineFunctions.CreateKeyCommand,
            EngineFunctions.CreateConfig,
            EngineFunctions.CreateDictionary,
            EngineFunctions.ProcessKey,
            EngineFunctions.CreateUiSnapshot);
    }

    internal SkkController Controller { get; }
    internal FakeConfigWatcher Watcher { get; }
    internal FakeKeyboardHook Hook { get; }
    internal FakeUserDictionaryWriterFactory WriterFactory { get; }
    internal FakeEffectExecutor EffectExecutor { get; }

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
