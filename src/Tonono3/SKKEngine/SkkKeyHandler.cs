using System;
using System.Diagnostics;
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
    public void Start(Func<KeyCommand, string?, bool> process)
    {
        ProcessCommand = process;
        hook.Install(OnKeyIntercepted);
    }
    private bool OnKeyIntercepted(int keyCode)
    {
        var command = ToKeyCommand(keyCode);
        return ProcessCommand(command, getActiveProcessPath());
    }
    private KeyCommand ToKeyCommand(int keyCode)
    {
        var (controlPressed, shiftPressed) = getMetaKeyState();
        var ch = keyCode == SkkKeyConstants.VkG && controlPressed ? '\0' : convertVirtualKeyToChar(keyCode, shiftPressed);
        return createKeyCommand(keyCode, shiftPressed, controlPressed, ch);
    }
    public void Dispose() => hook.Dispose();
}
