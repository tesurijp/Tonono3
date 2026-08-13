using System;
using System.Runtime.InteropServices;
using tsr_di;

namespace Tonono3.Interop;

public static partial class WindowBehaviorInterop
{
    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const int GWL_EXSTYLE = -20;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const uint WS_EX_NOACTIVATE = 0x08000000;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GUITHREADINFO
    {
        internal int cbSize;
        internal int flags;
        internal IntPtr hwndActive;
        internal IntPtr hwndFocus;
        internal IntPtr hwndCapture;
        internal IntPtr hwndMenuOwner;
        internal IntPtr hwndMoveSize;
        internal IntPtr hwndCaret;
        internal RECT rcCaret;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        internal int cbSize;
        internal RECT rcMonitor;
        internal RECT rcWork;
        internal uint dwFlags;
    }

    [LibraryImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [LibraryImport("user32.dll")]
    private static partial IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [LibraryImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "GetMonitorInfoW")] [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr newValue);

    [LibraryImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out POINT lpPoint);

    [ServiceFunction]
    public static (double X, double Y) GetTargetWindowPosition(double dpiScaleX, double dpiScaleY, double actualWidth, double actualHeight)
    {
        var gui = new GUITHREADINFO { cbSize = Marshal.SizeOf<GUITHREADINFO>() };
        var pt = new POINT();
        var hasCaret = false;

        if (GetGUIThreadInfo(0, ref gui) && gui.hwndCaret != IntPtr.Zero)
        {
            pt = new POINT { X = gui.rcCaret.Left, Y = gui.rcCaret.Bottom };
            ClientToScreen(gui.hwndCaret, ref pt);
            hasCaret = true;
        }
        else
        {
            GetCursorPos(out pt);
        }

        var hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        var mi = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };

        if (GetMonitorInfo(hMonitor, ref mi))
        {
            var x = pt.X / dpiScaleX;
            var y = (pt.Y + (hasCaret ? 5 : 0)) / dpiScaleY;

            var workLeft = mi.rcWork.Left / dpiScaleX;
            var workTop = mi.rcWork.Top / dpiScaleY;
            var workRight = mi.rcWork.Right / dpiScaleX;
            var workBottom = mi.rcWork.Bottom / dpiScaleY;

            if (x + actualWidth > workRight)
            {
                x = workRight - actualWidth;
            }
            if (x < workLeft)
            {
                x = workLeft;
            }

            if (y + actualHeight > workBottom)
            {
                y = (pt.Y / dpiScaleY) - actualHeight - (hasCaret ? 5 : 0);
            }
            if (y < workTop)
            {
                y = workTop;
            }
            return (x, y);
        }

        return (double.NaN, double.NaN);
    }
    [ServiceFunction]
    public static void SetNonActiveWindow(IntPtr handle)
    {
        var currentStyle = GetWindowLongPtr(handle, GWL_EXSTYLE);
        var newStyle = new IntPtr(currentStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
        SetWindowLongPtr(handle, GWL_EXSTYLE, newStyle);
    }
}
