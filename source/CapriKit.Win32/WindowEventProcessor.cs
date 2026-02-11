using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace CapriKit.Win32;

/// <summary>
/// Processes Win32 messages to update the information we have on the target window
/// </summary>
/// <param name="target"></param>
[SupportedOSPlatform(WindowsVersions.WindowsXP)]
public sealed class WindowEventProcessor(Win32Window target)
{
    private readonly Win32Window Target = target;

    internal void OnEvent(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (Target.Hwnd != hWnd)
        {
            return;
        }

        switch (msg)
        {
            case WM_SHOWWINDOW:
                GetClientRect(Target.Hwnd, out var rect);
                Target.OnSizeChanged(rect.Width, rect.Height);
                break;
            case WM_SIZE:
                var width = EventDecoder.Loword((int)lParam.Value);
                var height = EventDecoder.Hiword((int)lParam.Value);

                switch (wParam.Value)
                {
                    case SIZE_RESTORED:
                    case SIZE_MAXIMIZED:
                    case SIZE_MINIMIZED:
                        Target.OnSizeChanged(width, height);
                        break;
                }
                break;

            case WM_SETFOCUS:
                Target.OnFocusChanged(true);
                break;

            case WM_KILLFOCUS:
                Target.OnFocusChanged(false);
                break;

            case WM_ACTIVATE:
                Target.OnFocusChanged(EventDecoder.Loword((int)wParam.Value) != 0);
                break;

            case WM_MOUSEMOVE:
                Target.OnMouseMove();
                break;

            case WM_MOUSELEAVE:
                Target.OnMouseLeave();
                break;

            // Mouse
            case WM_LBUTTONDOWN:
            case WM_LBUTTONDBLCLK:
            case WM_RBUTTONDOWN:
            case WM_RBUTTONDBLCLK:
            case WM_MBUTTONDOWN:
            case WM_MBUTTONDBLCLK:
            case WM_XBUTTONDOWN:
            case WM_XBUTTONDBLCLK:
                Target.CaptureMouse();
                break;

            case WM_LBUTTONUP:
            case WM_RBUTTONUP:
            case WM_MBUTTONUP:
            case WM_XBUTTONUP:
                Target.ReleaseMouse();
                break;

        }
    }
}
