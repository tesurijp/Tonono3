using System;
using System.Threading;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SkkEngine;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class KeyboardHook(
    WriteLogFunc writeLog,
    InstallHookFunc installHook,
    GetMetaKeyStateFunc getMetaKeyState,
    ConvertVirtualKeyToCharFunc convertVirtualKeyToChar,
    CreateKeyCommandFunc createKeyCommand,
    GetActiveProcessPathFunc getActiveProcessPath
) : IKeyboardHook
{
    private static readonly Lock lockObj = new();
    private IDisposable? keyHandler;

    [ServiceFunction(ServiceType = typeof(ExecUiActionFunc), Name = "KeyHookEnable")]
    public void Install()
    {
        lock (lockObj)
        {
            keyHandler ??= installHook(OnKeyIntercepted, writeLog.Invoke);
        }
    }

    [ServiceFunction(ServiceType = typeof(ExecUiActionFunc), Name = "KeyHookDisable")]
    public void Uninstall()
    {
        lock (lockObj)
        {
            keyHandler?.Dispose();
            keyHandler = null;
        }
    }
    [ServiceFunction(ServiceName = "KeyHookStateFunc")]
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

    Func<KeyCommand, string?, bool> ProcessCommand = (_, _) => false;
    public void RegisterCallback(Func<KeyCommand, string?, bool> process) => ProcessCommand = process;

    private bool OnKeyIntercepted(int keyCode)
    {
        var command = createKeyCommand(keyCode, getMetaKeyState(), convertVirtualKeyToChar.Invoke);
        return ProcessCommand(command, getActiveProcessPath());
    }
}
