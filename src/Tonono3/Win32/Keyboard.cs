using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Tonono3.AutoDefined;
using Tonono3.SKKEngine;
using tsr_di;
using static Tonono3.Win32.NativeConstants;

namespace Tonono3.Win32;

[ServiceClass(Lifetime = Lifetime.Scoped)]
public sealed class KeyboardHook(IWriteLog writeLog) : IKeyboardHook
{
    private IntPtr hookId = IntPtr.Zero;
    private NativeMethods.LowLevelKeyboardProc? hookProc;
    public event Func<int,bool>? KeyIntercepted;

    public void Install()
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        hookProc = HookCallback;
        hookId = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, hookProc, NativeMethods.GetModuleHandle(curModule?.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                var msg = wParam.ToInt32();

                var isKeyDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;

                if (isKeyDown)
                {
                    var hook = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

                    if ((hook.flags & NativeMethods.KbdLlFlags.LLKHF_INJECTED) == 0)
                    {
                        var result = KeyIntercepted?.Invoke((int)hook.vkCode);

                        if (result ?? false)
                        {
                            return 1;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            writeLog($"{Environment.NewLine}{ex.GetType()}{Environment.NewLine}{ex.Message}");
            writeLog($"{Environment.NewLine}{ex.StackTrace}");
        }
        return NativeMethods.CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(hookId);
            hookId = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}
public static class Keyboard
{
    [ServiceFunction(ServiceName = "GetMetaKeyState")]
    public static (bool Control, bool Shift) GetMetaKeyState()
    {
        var ctrlPressed = (NativeMethods.GetKeyState(VK_CONTROL) & 0x8000) != 0;
        var shiftPressed = (NativeMethods.GetKeyState(VK_SHIFT) & 0x8000) != 0;
        return (ctrlPressed, shiftPressed);
    }

    [ServiceFunction(ServiceName = "ConvertVirtualKeyToChar")]
    public static char VkToChar(int vkCode, bool shift)
    {
        if (vkCode == SkkKeyConstants.VkG && (NativeMethods.GetKeyState(VK_CONTROL) & 0x8000) != 0)
        {
            return '\0';
        }

        var keyState = new byte[256];
        NativeMethods.GetKeyboardState(keyState);

        keyState[VK_SHIFT] = (byte)(shift ? 0x80 : 0);
        keyState[VK_CONTROL] = 0;
        keyState[VK_MENU] = 0;

        var sbbuf = new char[10];
        var scanCode = NativeMethods.MapVirtualKey((uint)vkCode, 0);
        var result = NativeMethods.ToUnicode((uint)vkCode, scanCode, keyState, sbbuf, 5, 0);

        if (result > 0)
        {
            var str = new string(sbbuf);
            if (string.IsNullOrEmpty(str))
            {
                return '\0';
            }
            return str[0];
        }

        return '\0';
    }

}
