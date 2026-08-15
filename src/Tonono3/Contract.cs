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
    event Action<long, AppConfig, SkkDictionarySnapshot>? RuntimeReloaded;
    (AppConfig, SkkDictionarySnapshot) LoadRuntime();
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

public interface ISkkController : IDisposable
{
    event Action<SkkUiSnapshot>? UiUpdated;
    SkkUiSnapshot CurrentUiSnapshot { get; }
    AppConfig CurrentConfig { get; }
    void Start();
    bool ProcessCommand(KeyCommand command, string? activeProcessPath);
}

public interface ITononoUi
{
    WindowIcon? Icon { get; set; }
    void ApplySnapshot(SkkUiSnapshot snapshot);
    void Close();
}

public interface ISystemMenu : IDisposable;

public interface IApplicationCoordinator : IDisposable
{
    void Start(IControlledApplicationLifetime? controlledLifetime);
}


