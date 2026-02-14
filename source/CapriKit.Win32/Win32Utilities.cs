using System.Drawing;
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
    private const WINDOW_STYLE RegularWindowStyle = WINDOW_STYLE.WS_OVERLAPPEDWINDOW;
    private const WINDOW_EX_STYLE RegularWindowStyleEx = WINDOW_EX_STYLE.WS_EX_APPWINDOW | WINDOW_EX_STYLE.WS_EX_WINDOWEDGE;
    private const WINDOW_STYLE BorderlessWindowStyle = WINDOW_STYLE.WS_POPUP | WINDOW_STYLE.WS_VISIBLE;
    private const WINDOW_EX_STYLE BorderlessWindowStyleEx = WINDOW_EX_STYLE.WS_EX_APPWINDOW;

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
        return CreateWindowEx(
            RegularWindowStyleEx, WindowClassName, title, RegularWindowStyle,
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

    /// <summary>
    /// Transforms a borderless fullscreen window into a regular window with borders.
    /// </summary>
    public static bool MakeWindowed(HWND hwnd, int x, int y, int width, int height)
    {
        SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, unchecked((nint)RegularWindowStyle));
        SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, unchecked((nint)RegularWindowStyleEx));

        // Set the new position, don't bother copying the contents as that will be redrawn by the app anyway
        return SetWindowPos(hwnd,
            HWND.Null,
            x,
            y,
            width,
            height,
            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOCOPYBITS);
    }

    /// <summary>
    /// Transforms a regular window to a borderless fullscreen window that fills the monitor the window was on.
    /// </summary>
    public static bool MakeBorderlessFullscreen(HWND hwnd)
    {
        var hmon = MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
        var monitorInfo = new MONITORINFO() { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (GetMonitorInfo(hmon, ref monitorInfo))
        {
            SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, unchecked((nint)BorderlessWindowStyle));
            SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, unchecked((nint)BorderlessWindowStyleEx));

            // Set the new position, don't bother copying the contents as that will be redrawn by the app anyway
            return SetWindowPos(hwnd,
                HWND.Null,
                monitorInfo.rcMonitor.X,
                monitorInfo.rcMonitor.Y,
                monitorInfo.rcMonitor.Width,
                monitorInfo.rcMonitor.Height,
                SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOCOPYBITS);
        }
        return false;
    }
}
