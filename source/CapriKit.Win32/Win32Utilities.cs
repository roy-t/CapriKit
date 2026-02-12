using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace CapriKit.Win32;

[SupportedOSPlatform(WindowsVersions.WindowsXP)]
internal static class Win32Utilities
{
    private const string WindowClassName = "CapriKit.Win32.WindowClass";

    public static unsafe void RegisterWindowClass(delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT> lpfnWndProc)
    {
        fixed (char* ptrClassName = WindowClassName)
        {
            var moduleHandle = GetModuleHandle(null);
            var cursor = LoadCursor((HINSTANCE)IntPtr.Zero, IDC_ARROW);

            var wndClass = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW,
                lpfnWndProc = lpfnWndProc,
                hInstance = (HINSTANCE)moduleHandle.DangerousGetHandle(),
                hCursor = cursor,
                hbrBackground = HBRUSH.Null,
                hIcon = HICON.Null,
                lpszClassName = new PCWSTR(ptrClassName),
            };

            RegisterClassEx(wndClass);
        }
    }

    public static unsafe HWND CreateWindow(string title)
    {
        var style = WINDOW_STYLE.WS_OVERLAPPEDWINDOW;
        var styleEx = WINDOW_EX_STYLE.WS_EX_APPWINDOW | WINDOW_EX_STYLE.WS_EX_WINDOWEDGE;

        return CreateWindowEx(
            styleEx, WindowClassName, title, style,
            CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,
            (HWND)IntPtr.Zero, null, null, null);
    }

    public static void ShowWindow(HWND handle)
    {
        GetStartupInfo(out var startupInfo);

        SHOW_WINDOW_CMD showCmd =
        (startupInfo.dwFlags & STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW) != 0
            ? (SHOW_WINDOW_CMD)startupInfo.wShowWindow
            : SHOW_WINDOW_CMD.SW_NORMAL;

        PInvoke.ShowWindow(handle, showCmd);
    }

    public static void MakeBorderlessFullscreen(HWND hwnd)
    {
        var hmon = MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
        var monitorInfo = new MONITORINFO() { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (GetMonitorInfo(hmon, ref monitorInfo))
        {
            var width = monitorInfo.rcMonitor.Width;
            var height = monitorInfo.rcMonitor.Height;

            //// Unset overlapped window and 
            //var style = (uint)GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            //style &= ~(uint)WINDOW_STYLE.WS_OVERLAPPEDWINDOW;
            //style |= (uint)(WINDOW_STYLE.WS_POPUP | WINDOW_STYLE.WS_VISIBLE);

            //SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, unchecked((nint)style));

            // Override the complete style of the window instead of just switchin on/off a few bits
            var style = WINDOW_STYLE.WS_POPUP | WINDOW_STYLE.WS_VISIBLE;
            var styleEx = WINDOW_EX_STYLE.WS_EX_APPWINDOW;

            SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, unchecked((nint)style));
            SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, unchecked((nint)styleEx));

            // Set the new position, don't bother copying the contents as that will be redrawn by the app anyway
            SetWindowPos(hwnd,
                HWND.Null,
                monitorInfo.rcMonitor.X,
                monitorInfo.rcMonitor.Y,
                monitorInfo.rcMonitor.Width,
                monitorInfo.rcMonitor.Height,
                SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOCOPYBITS);
        }

    }
}
