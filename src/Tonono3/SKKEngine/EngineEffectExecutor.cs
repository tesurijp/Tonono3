using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SKKEngine;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class EngineEffectExecutor(
    SendTextFunc sendText,
    TurnOffImeFunc turnOffIme,
    WriteLogFunc writeLog,
    CreateUserDictionaryWriterFunc createUserDictionaryWriter
    ) : IEngineEffectDispatcher
{
    private string userDicPath = "";
    private IUserDictionaryWriter? dictionaryWriter;

    public void ApplyUserDictionaryPath(string path)
    {
        if (userDicPath != path)
        {
            userDicPath = path;
            dictionaryWriter?.Dispose();
            dictionaryWriter = createUserDictionaryWriter(userDicPath);
        }
    }

    public void Execute(TransitionResult result)
    {
        foreach (var effect in result.Effects)
        {
            switch (effect)
            {
                case CommitTextEffect commit:
                    sendText(commit.Text);
                    break;
                case PersistUserDictionaryEffect:
                    dictionaryWriter?.Enqueue(result.Dictionary.User);
                    break;
                case TurnOffImeEffect:
                    turnOffIme();
                    break;
                case WriteLogEffect log:
                    writeLog(log.Message);
                    break;
            }
        }
    }
    public void Dispose() => dictionaryWriter?.Dispose();
}
