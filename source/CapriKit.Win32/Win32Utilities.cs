using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace CapriKit.Win32;

/// <summary>
/// Whether to define the size of a window from the border (outside) or client area (drawable area).
/// </summary>
public enum WindowMeasure
{
    Border,
    ClientArea
}

/// <summary>
/// Whether to define the position of a window absolutely or as an offset from the center of the primary monitor.
/// </summary>
public enum WindowOrigin
{
    Absolute,
    CenterOffset
}

public record WindowCreationOptions(int X = CW_USEDEFAULT, int Y = CW_USEDEFAULT, int Width = CW_USEDEFAULT, int Height = CW_USEDEFAULT, WindowOrigin Origin = WindowOrigin.Absolute, WindowMeasure Measure = WindowMeasure.Border);

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
        return CreateWindow(title, new WindowCreationOptions());
    }

    public static unsafe HWND CreateWindow(string title, WindowCreationOptions options)
    {
        RECT desired = new(0, 0, options.Width, options.Height);
        if (options.Measure == WindowMeasure.ClientArea)
        {
            AdjustWindowRectEx(ref desired, RegularWindowStyle, false, RegularWindowStyleEx);
        }

        var x = options.X;
        var y = options.Y;
        if (options.Origin == WindowOrigin.CenterOffset)
        {
            x += (GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN) - desired.Width) / 2;
            y += (GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN) - desired.Height) / 2;
        }

        return CreateWindowEx(
            RegularWindowStyleEx, WindowClassName, title, RegularWindowStyle,
            x, y, desired.Width, desired.Height,
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
    /// Resizes the window so that the smallest rectangle that contains the entire window, including menus, borders and title bars
    /// becomes the desired size.
    /// </summary>
    public static bool ResizeWindow(HWND hwnd, int width, int height)
    {
        // Set the new position/size, don't bother copying the contents as that will be redrawn by the app anyway
        return SetWindowPos(hwnd,
            HWND.Null,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            width,
            height,
            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOCOPYBITS | SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
    }

    /// <summary>
    /// Resizes the window so that the client area (the place where you can draw, outside of the menus, borders and title bars)
    /// becomes the desired size.
    /// </summary>
    public static bool ResizeClientArea(HWND hwnd, int clientAreaWidth, int clientAreaHeight)
    {
        var style = (WINDOW_STYLE)GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        var exStyle = (WINDOW_EX_STYLE)GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        var menu = GetMenu(hwnd) != HWND.Null;

        RECT desired = new(0, 0, clientAreaWidth, clientAreaHeight);
        AdjustWindowRectEx(ref desired, style, menu, exStyle);

        // Set the new position/size, don't bother copying the contents as that will be redrawn by the app anyway
        return SetWindowPos(hwnd,
            HWND.Null,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            desired.Width,
            desired.Height,
            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOCOPYBITS | SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
    }

    /// <summary>
    /// Transforms a borderless fullscreen window into a regular window with borders.
    /// </summary>
    public static bool MakeWindowed(HWND hwnd, int x, int y, int width, int height)
    {
        SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, unchecked((nint)RegularWindowStyle));
        SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, unchecked((nint)RegularWindowStyleEx));

        // Set the new position/size, don't bother copying the contents as that will be redrawn by the app anyway
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

            // Set the new position/size, don't bother copying the contents as that will be redrawn by the app anyway
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
