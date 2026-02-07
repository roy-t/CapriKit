using System.Runtime.CompilerServices;
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
public static class Win32Application
{
    private const string WindowClassName = "CapriKit.Window";
    private static List<WindowEventProcessor> Processors = [];

    public static Window Initialize(string windowTitle)
    {
        RegisterWindowClass();
        var handle = CreateWindow(windowTitle);
        ShowWindow(handle);

        var window = new Window(handle);
        Processors.Add(new WindowEventProcessor(window));

        return window;
    }

    private static unsafe void RegisterWindowClass()
    {
        fixed (char* ptrClassName = WindowClassName)
        {
            var moduleHandle = GetModuleHandle(null);
            var cursor = LoadCursor((HINSTANCE)IntPtr.Zero, IDC_ARROW);

            var wndClass = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW,
                lpfnWndProc = &WndProc,
                hInstance = (HINSTANCE)moduleHandle.DangerousGetHandle(),
                hCursor = cursor,
                hbrBackground = HBRUSH.Null,
                hIcon = HICON.Null,
                lpszClassName = new PCWSTR(ptrClassName),
            };

            RegisterClassEx(wndClass);
        }
    }

    private static unsafe HWND CreateWindow(string title)
    {
        var style = WINDOW_STYLE.WS_OVERLAPPEDWINDOW;
        var styleEx = WINDOW_EX_STYLE.WS_EX_APPWINDOW | WINDOW_EX_STYLE.WS_EX_WINDOWEDGE;

        return CreateWindowEx(
            styleEx, WindowClassName, title, style,
            CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,
            (HWND)IntPtr.Zero, null, null, null);
    }

    private static void ShowWindow(HWND handle)
    {
        GetStartupInfo(out var startupInfo);

        SHOW_WINDOW_CMD showCmd =
        (startupInfo.dwFlags & STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW) != 0
            ? (SHOW_WINDOW_CMD)startupInfo.wShowWindow
            : SHOW_WINDOW_CMD.SW_NORMAL;

        PInvoke.ShowWindow(handle, showCmd);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    internal static LRESULT WndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        foreach (var processor in Processors)
        {
            processor.OnEvent(hWnd, msg, wParam, lParam);
        }

        switch (msg)
        {
            case WM_DESTROY:
                PostQuitMessage(0);
                break;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public static bool PumpMessages()
    {
        var @continue = true;
        while (PeekMessage(out var msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
        {
            TranslateMessage(msg);
            DispatchMessage(msg);
            @continue = @continue && (msg.message != WM_QUIT);
        }

        return @continue;
    }

    public static void SetMouseCursor(Cursor cursor)
    {
        if (cursor == Cursor.Default)
        {
            SetCursor(null);
        }

        unsafe
        {
            PCWSTR resource = (char*)(int)cursor;
            var hCursor = LoadCursor((HINSTANCE)IntPtr.Zero, resource);
            SetCursor(hCursor);
        }
    }
}
