using System;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SKKEngine;

[ServiceClass(Lifetime = Lifetime.Scoped)]
public sealed class KeyboardHook(IWriteLog writeLog, IInstallHook installHook ) : IKeyboardHook
{
    private IDisposable? keyHandler;
    public void Install(Func<int, bool > KeyIntercepted)
    {
        keyHandler = installHook(KeyIntercepted, writeLog.Invoke);
    }
    public void Dispose()
    {
        keyHandler?.Dispose();
        GC.SuppressFinalize(this);
    }
}
