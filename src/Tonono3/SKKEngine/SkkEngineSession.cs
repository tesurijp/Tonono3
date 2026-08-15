using System;
using System.Collections.Generic;
using System.Text;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SKKEngine;

[ServiceClass]
public sealed class SkkEngineSession(
    CreateInitialStateFunc createInitialState,
    CreateConfigFunc createConfig,
    CreateDictionaryFunc createDictionary,
    ProcessKeyFunc processKey,
    CreateUiSnapshotFunc createUiSnapshot,
    IEngineEffectDispatcher effectDispatcher ) : ISkkEngineSession
{
    private EngineState state = createInitialState();
    //  SkkController の開始時にApplyRuntimeが呼ばれることで以下のフィールドは必ず初期化される。
    public AppConfig CurrentConfig { get; private set; } = null!;
    private EngineConfig config = null!;
    private DictionarySnapshot dictionary = null!;

    public void ApplyRuntime(AppConfig appconfig, SkkDictionarySnapshot dictionarySnapshot)
    {
        CurrentConfig = appconfig;
        config = createConfig(CurrentConfig.RomajiTable, CurrentConfig.MoraModifier, CurrentConfig.MoraAutoComplete, CurrentConfig.ZenkakuTable, CurrentConfig.ViCompatibleApps);
        dictionary = createDictionary(dictionarySnapshot.Main, dictionarySnapshot.User);
        effectDispatcher.ApplyUserDictionaryPath(CurrentConfig.UserDictionaryPath);
    }

    public TransitionResult Process(KeyCommand command, string? activeProcessPath)
    {
        var result = processKey(
            state,
            config,
            dictionary,
            command,
            activeProcessPath!);

        state = result.State;
        dictionary = result.Dictionary;
        effectDispatcher.Execute(result);
        return result;
    }


    public SkkUiSnapshot CreateUiSnapshot(long version)
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

    public void Dispose() => effectDispatcher.Dispose();
}
