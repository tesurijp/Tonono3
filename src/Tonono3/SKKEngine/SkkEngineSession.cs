using Microsoft.FSharp.Core;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SKKEngine;

[ServiceClass(Lifetime=Lifetime.Singleton)]
public sealed class SkkEngineSession(
    CreateInitialStateFunc createInitialState,
    ProcessKeyFunc processKey,
    CreateUiSnapshotFunc createUiSnapshot,
    IEngineEffectDispatcher effectDispatcher ) : ISkkEngineSession
{
    private EngineState state = createInitialState();
    //  SkkController の開始時にApplyRuntimeが呼ばれることで以下のフィールドは必ず初期化される。
    public AppConfig CurrentConfig { get; private set; } = null!;
    private DictionarySnapshot dictionary = null!;

    [ServiceFunction]
    public AppConfig GetAppConfig() => CurrentConfig;

    public void ApplyRuntime(AppConfig config, DictionarySnapshot dictionary)
    {
        CurrentConfig = config;
        this.dictionary = dictionary;
        effectDispatcher.ApplyUserDictionaryPath(CurrentConfig.UserDictionaryPath);
    }

    public TransitionResult Process(KeyCommand command, string? activeProcessPath)
    {
        var result = processKey(
            state,
            CurrentConfig,
            dictionary,
            command,
            activeProcessPath!);

        state = result.State;
        dictionary = result.Dictionary;
        effectDispatcher.Execute(result);
        return result;
    }
    public UiSnapshot CreateUiSnapshot(long version) => createUiSnapshot(state, version);
}
