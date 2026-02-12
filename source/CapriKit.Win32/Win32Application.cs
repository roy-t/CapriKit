using CapriKit.Win32.Input;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace CapriKit.Win32;

/// <summary>
/// Class for creating a Win32 application with a single window.
/// Usage: run `Initialize` first. Call `PumpMessages` every frame. Use the `Window`, `Mouse`, and `Keyboard` properties to work with the input devices and window.
/// </summary>
[SupportedOSPlatform(WindowsVersions.WindowsXP)]
public static class Win32Application
{
    private static WindowEventProcessor? WindowEventProcessor;
    private static InputEventProcessor? InputEventProcessor;

    public static void Initialize(string windowTitle)
    {
        unsafe
        {
            Win32Utilities.RegisterWindowClass(&WndProc);
        }

        var handle = Win32Utilities.CreateWindow(windowTitle);
        Window.UpdateHandle(handle);
        WindowEventProcessor = new(Window);
        InputEventProcessor = new(Window, Mouse, Keyboard);

        Win32Utilities.ShowWindow(handle);
    }

    public static Keyboard Keyboard { get; } = new();
    public static Mouse Mouse { get; } = new();

    public static Win32Window Window { get; } = new Win32Window();

    public static bool PumpMessages()
    {
        var @continue = true;
        while (PeekMessage(out var msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
        {
            TranslateMessage(msg);
            DispatchMessage(msg);
            @continue = @continue && (msg.message != WM_QUIT);
        }

        InputEventProcessor!.NextFrame();
        return @continue;
    }

    public static void SetMouseCursor(CursorStyle cursor)
    {
        if (cursor == CursorStyle.Default)
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


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    internal static LRESULT WndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        WindowEventProcessor?.OnEvent(hWnd, msg, wParam, lParam);
        InputEventProcessor?.OnEvent(hWnd, msg, wParam, lParam);

        switch (msg)
        {
            case WM_DESTROY:
                PostQuitMessage(0);
                break;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }
}
