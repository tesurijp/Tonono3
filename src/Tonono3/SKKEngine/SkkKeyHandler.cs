using System;
using Tonono3.AutoDefined;
using tsr_di;

namespace Tonono3.SKKEngine;

[ServiceClass(Lifetime = Lifetime.Singleton)]
public sealed class SkkKeyHandler(
        GetMetaKeyStateFunc getMetaKeyState,
        ConvertVirtualKeyToCharFunc convertVirtualKeyToChar,
        CreateKeyCommandFunc createKeyCommand,
        IKeyboardHook hook,
        GetActiveProcessPathFunc getActiveProcessPath) : ISkkKeyHandler
{
    Func<KeyCommand, string?, bool> ProcessCommand = (_, _) => false;
    public void RegisterCallback(Func<KeyCommand, string?, bool> process) => ProcessCommand = process;

    [ServiceFunction(ServiceType=typeof(ExecUiActionFunc), Name ="KeyHookEnable")]
    public void Enable() => hook.Install(OnKeyIntercepted);
    [ServiceFunction(ServiceType=typeof(ExecUiActionFunc), Name ="KeyHookDisable")]
    public void Disable() => hook.Uninstall();

    private bool OnKeyIntercepted(int keyCode)
    {
        var command = createKeyCommand(keyCode, getMetaKeyState(), convertVirtualKeyToChar.Invoke );
        return ProcessCommand(command, getActiveProcessPath());
    }
    public void Dispose() => hook.Dispose();
}
