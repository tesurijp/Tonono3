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
    ReloadConfigFunc reloadConfig,
    CreateUiSnapshotFunc createUiSnapshot,
    IEngineEffectDispatcher effectDispatcher ) : ISkkEngineSession
{
    private EngineState state = createInitialState();
    public AppConfig CurrentConfig { get; private set; } = reloadConfig();
    private EngineConfig config = null!;
    private DictionarySnapshot dictionary = null!;

    public void ApplyRuntime(AppConfig appconfig, SkkDictionarySnapshot dictionarySnapshot)
    {
        config = createConfig(appconfig.RomajiTable, appconfig.MoraModifier, appconfig.MoraAutoComplete, appconfig.ZenkakuTable, appconfig.ViCompatibleApps);
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
