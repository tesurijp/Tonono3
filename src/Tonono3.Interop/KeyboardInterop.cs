using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using tsr_di;

namespace Tonono3.Interop;

public static partial class KeyboardInterop
{
    private static INPUT MakeKeyInput(ushort vk, ushort scan, bool up)
    {
        uint flag = (up ? KEYEVENTF_KEYUP : 0) + (scan != 0 ? KEYEVENTF_UNICODE : 0);
        var input = new INPUT { type = INPUT_KEYBOARD };
        input.U.ki = new() { wVk = vk, wScan = scan, dwFlags = flag, time = 0, dwExtraInfo = IntPtr.Zero };
        return input;
    }

    private sealed class KeyStateKeeper : IDisposable
    {
        private readonly Action disposeAction = () => { };
        public void Dispose() => disposeAction();
        internal KeyStateKeeper(List<INPUT> inputs, ushort vk)
        {
            var pressed = (GetKeyState(vk) & 0x8000) != 0;
            if (pressed)
            {
                inputs.Add(MakeKeyInput(vk, 0, true));
                disposeAction = () => inputs.Add(MakeKeyInput(vk, 0, false));
            }
        }
    }

    [ServiceFunction]
    public static void SendText(string text)
    {
        var inputs = new List<INPUT>();
        {
            using var _ctrl = new KeyStateKeeper(inputs, VK_CONTROL);
            using var _shift = new KeyStateKeeper(inputs, VK_SHIFT);
            using var _alt = new KeyStateKeeper(inputs, VK_MENU);

            foreach (var c in text)
            {
                inputs.Add(MakeKeyInput(0, c, true));
                inputs.Add(MakeKeyInput(0, c, false));
            }
        }
        SendInput((uint)inputs.Count, [.. inputs], Marshal.SizeOf<INPUT>());
    }
    private sealed class KeyHandler(Func<int, bool> KeyInterceptFunc, Action<string> writeLog) : IDisposable
    {
        public IntPtr HookId { get; set; }
        internal LowLevelKeyboardProc? HookProc { get; set; }
        public void Dispose()
        {
            if (HookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(HookId);
                HookId = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
        internal IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
                {
                    var hook = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    if ((hook.flags & KbdLlFlags.LLKHF_INJECTED) == 0)
                    {
                        if (KeyInterceptFunc((int)hook.vkCode))
                        {
                            return 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                writeLog($"{Environment.NewLine}{ex.GetType()}{Environment.NewLine}{ex.Message}");
                writeLog($"{Environment.NewLine}{ex.StackTrace}");
            }
            return CallNextHookEx(HookId, nCode, wParam, lParam);
        }
    }

    [ServiceFunction]
    public static IDisposable InstallHook(Func<int, bool> KeyInterceptFunc, Action<string> writeLog)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        var keyHandler = new KeyHandler(KeyInterceptFunc, writeLog);
        keyHandler.HookProc = keyHandler.HookCallback;
        keyHandler.HookId = SetWindowsHookEx(WH_KEYBOARD_LL, keyHandler.HookProc, GetModuleHandle(curModule?.ModuleName), 0);
        return keyHandler;
    }

    [ServiceFunction]
    public static (bool Control, bool Shift) GetMetaKeyState()
    {
        var ctrlPressed = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
        var shiftPressed = (GetKeyState(VK_SHIFT) & 0x8000) != 0;
        return (ctrlPressed, shiftPressed);
    }

    [ServiceFunction]
    public static char ConvertVirtualKeyToChar(int vkCode, bool shift)
    {
        var keyState = new byte[256];
        GetKeyboardState(keyState);

        keyState[VK_SHIFT] = (byte)(shift ? 0x80 : 0);
        keyState[VK_CONTROL] = 0;
        keyState[VK_MENU] = 0;

        var sbbuf = new char[10];
        var scanCode = MapVirtualKey((uint)vkCode, 0);
        var result = ToUnicode((uint)vkCode, scanCode, keyState, sbbuf, 5, 0);

        if (result > 0)
        {
            var str = new string(sbbuf);
            if (str.Length > 0)
            {
                return str[0];
            }
        }
        return '\0';
    }
}
