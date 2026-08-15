using System;
using System.Runtime.InteropServices;
using tsr_di;

namespace Tonono3.Interop;

public static partial class ImeInterop
{
    private const uint WM_IME_CONTROL = 0x0283;
    private const nuint IMC_SETOPENSTATUS = 0x0006;

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("imm32.dll")]
    private static partial IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    private static partial nint SendMessage(IntPtr hWnd, uint message, nuint wParam, nint lParam);

    [ServiceFunction]
    public static void TurnOffIme()
    {
        var window = GetForegroundWindow();
        if (window == IntPtr.Zero)
        {
            return;
        }

        var imeWindow = ImmGetDefaultIMEWnd(window);
        if (imeWindow == IntPtr.Zero)
        {
            return;
        }

        SendMessage(imeWindow, WM_IME_CONTROL, IMC_SETOPENSTATUS, 0);
    }
}
