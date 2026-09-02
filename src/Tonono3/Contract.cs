using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
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
    void Enqueue(DictionarySnapshot dictionary);
}

public interface IConfigWatcher : IDisposable
{
    event Action<long, AppConfig, DictionarySnapshot>? RuntimeReloaded;
    (AppConfig, DictionarySnapshot) LoadRuntime();
    void Start();
}
public interface ISkkKeyHandler : IDisposable
{
    void RegisterCallback(Func<KeyCommand, string?, bool> process);
}

public interface IKeyboardHook : IDisposable
{
    void Install(Func<int, bool> KeyIntercepted);
    void Uninstall();
    bool IsEnabled();
}
public interface ISkkEngineSession
{
    void ApplyRuntime(AppConfig config, DictionarySnapshot dictionary);
    TransitionResult Process(KeyCommand command, string? activeProcessPath);
    UiSnapshot CreateUiSnapshot(long version);
    AppConfig CurrentConfig { get; }
}

public interface IEngineEffectDispatcher
{
    void ApplyUserDictionaryPath(string path);
    void Execute(TransitionResult result);
}

public interface ISkkController : IDisposable
{
    void Start();
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

public delegate void ExecUiActionFunc();
