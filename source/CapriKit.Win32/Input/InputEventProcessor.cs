using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace CapriKit.Win32.Input;

[SupportedOSPlatform(WindowsVersions.WindowsXP)]
public sealed class InputEventProcessor(Win32Window Target, Mouse Mouse, Keyboard Keyboard)
{
    public void NextFrame()
    {
        var position = Target.GetCursorPosition();
        Mouse.UpdatePosition(position);

        Mouse.NextFrame();        
        Keyboard.NextFrame();        
    }

    internal void OnEvent(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (Target == null || Target.Handle != hWnd || Mouse == null || Keyboard == null)
        {
            return;
        }

        switch (msg)
        {
            case WM_MOUSEWHEEL:
                Mouse.OnScroll(EventDecoder.GetMouseWheelDelta(wParam));
                break;

            case WM_MOUSEHWHEEL:
                Mouse.OnHScroll(EventDecoder.GetMouseWheelDelta(wParam));
                break;

            case WM_CHAR:
                Keyboard.OnChar((char)wParam);
                break;

            case WM_KEYDOWN:
            case WM_SYSKEYDOWN:
                Keyboard.OnKeyDown(EventDecoder.GetKeyCode(wParam));
                break;

            case WM_KEYUP:
            case WM_SYSKEYUP:
                Keyboard.OnKeyUp(EventDecoder.GetKeyCode(wParam));
                break;

            case WM_LBUTTONDOWN:
            case WM_LBUTTONDBLCLK:
            case WM_RBUTTONDOWN:
            case WM_RBUTTONDBLCLK:
            case WM_MBUTTONDOWN:
            case WM_MBUTTONDBLCLK:
            case WM_XBUTTONDOWN:
            case WM_XBUTTONDBLCLK:
                Mouse.OnButtonDown(EventDecoder.GetMouseButton(msg, wParam, lParam));
                break;

            case WM_LBUTTONUP:
            case WM_RBUTTONUP:
            case WM_MBUTTONUP:
            case WM_XBUTTONUP:
                Mouse.OnButtonUp(EventDecoder.GetMouseButton(msg, wParam, lParam));
                break;
        }
    }
}
