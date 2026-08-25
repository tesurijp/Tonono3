using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Collections.Immutable;
using Tonono3.SKKEngine;

namespace Tonono3;
public interface IConfigPathProvider
{
    string ConfigFileName { get; }
    string ConfigPath { get; }
    string ConfigFolder { get; }
    string SystemConfigFolder { get; }
    string UserConfigFolder { get; }
}

public interface IUserDictionaryWriter : IDisposable
{
    void Enqueue(ImmutableDictionary<string, ImmutableArray<string>> dictionary);
}

public interface IConfigWatcher : IDisposable
{
    event Action<long, AppConfig, DictionarySnapshot>? RuntimeReloaded;
    (AppConfig, DictionarySnapshot) LoadRuntime();
    void Start();
}
public interface ISkkKeyHandler : IDisposable
{
    void Start(Func<KeyCommand, string?, bool> process);
}

public interface IKeyboardHook : IDisposable
{
    void Install(Func<int, bool> KeyIntercepted);
}
public interface ISkkEngineSession : IDisposable
{
    void ApplyRuntime(AppConfig config, DictionarySnapshot dictionary);
    TransitionResult Process(KeyCommand command, string? activeProcessPath);
    UiSnapshot CreateUiSnapshot(long version);
    AppConfig CurrentConfig { get; }
}

public interface IEngineEffectDispatcher : IDisposable
{
    void ApplyUserDictionaryPath(string path);
    void Execute(TransitionResult result);
}

public interface ISkkController : IDisposable
{
    event Action<UiSnapshot>? UiUpdated;
    UiSnapshot CurrentUiSnapshot { get; }
    AppConfig CurrentConfig { get; }
    void Start();
    bool ProcessCommand(KeyCommand command, string? activeProcessPath);
}

public interface ITononoUi
{
    WindowIcon? Icon { get; set; }
    void ApplySnapshot(UiSnapshot snapshot);
    void Close();
}

public interface ISystemMenu : IDisposable;

public interface IApplicationCoordinator : IDisposable
{
    void Start(IControlledApplicationLifetime? controlledLifetime);
}


