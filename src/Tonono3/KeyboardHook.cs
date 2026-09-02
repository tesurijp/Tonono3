using System;
using System.Threading;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SKKEngine;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class KeyboardHook(WriteLogFunc writeLog, InstallHookFunc installHook ) : IKeyboardHook
{
    private static readonly Lock lockObj = new();
    private IDisposable? keyHandler;
    public void Install(Func<int, bool > KeyIntercepted)
    {
        lock (lockObj)
        {
            keyHandler ??= installHook(KeyIntercepted, writeLog.Invoke);
        }
    }

    public void Uninstall()
    {
        lock (lockObj)
        {
            keyHandler?.Dispose();
            keyHandler = null;
        }
    }
    [ServiceFunction(ServiceName ="KeyHookStateFunc")]
    public bool IsEnabled()
    {
        lock (lockObj)
        {
            return keyHandler != null;
        }
    }

    public void Dispose()
    {
        Uninstall();
        GC.SuppressFinalize(this);
    }
}
