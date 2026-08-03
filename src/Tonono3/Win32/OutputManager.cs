using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using tsr_di;
using static Tonono3.Win32.NativeConstants;

namespace Tonono3.Win32;

public static class OutputManager
{
    [ServiceFunction(ServiceName = "SendText")]
    public static void SendString(string text)
    {
        var inputs = new List<NativeMethods.INPUT>();
        foreach (var c in text)
        {
            var down = new NativeMethods.INPUT { type = INPUT_KEYBOARD };
            down.U.ki = new() { wVk = 0, wScan = c, dwFlags = KEYEVENTF_UNICODE, time = 0, dwExtraInfo = IntPtr.Zero };
            
            var up = new NativeMethods.INPUT { type = INPUT_KEYBOARD };
            up.U.ki = new() { wVk = 0, wScan = c, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP, time = 0, dwExtraInfo = IntPtr.Zero };

            inputs.Add(down);
            inputs.Add(up);
        }
        NativeMethods.SendInput((uint)inputs.Count, [.. inputs], Marshal.SizeOf<NativeMethods.INPUT>());
    }
}
