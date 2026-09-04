using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SkkEngine;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class EngineEffectExecutor(
    SendTextFunc sendText,
    TurnOffImeFunc turnOffIme,
    WriteLogFunc writeLog,
    CreateUserDictionaryWriterFunc createUserDictionaryWriter
    ) : IEngineEffectDispatcher
{
    private string userDicPath = "";
    public void ApplyUserDictionaryPath(string path) => userDicPath = path;
    public void Execute(TransitionResult result)
    {
        IUserDictionaryWriter? dictionaryWriter = null;
        foreach (var effect in result.Effects)
        {
            switch (effect)
            {
                case CommitTextEffect commit:
                    sendText(commit.Text);
                    break;
                case PersistUserDictionaryEffect:
                    dictionaryWriter ??= createUserDictionaryWriter(userDicPath);
                    dictionaryWriter.Enqueue(result.Dictionary);
                    break;
                case TurnOffImeEffect:
                    turnOffIme();
                    break;
                case WriteLogEffect log:
                    writeLog(log.Message);
                    break;
            }
        }
        dictionaryWriter?.Dispose();
    }
}
