using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SKKEngine;

public interface IEngineEffectExecutor;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class EngineEffectExecutor(ISendText sendText, IWriteLog writeLog) : IEngineEffectExecutor
{
    [ServiceFunction(ServiceName = "ExecuteEngineEffects")]
    public void Execute(TransitionResult result, IUserDictionaryWriter dictionaryWriter)
    {
        foreach (var effect in result.Effects)
        {
            switch (effect)
            {
                case CommitTextEffect commit:
                    sendText(commit.Text);
                    break;
                case PersistUserDictionaryEffect:
                    dictionaryWriter.Enqueue(result.Dictionary.User);
                    break;
                case WriteLogEffect log:
                    writeLog(log.Message);
                    break;
            }
        }
    }
}
