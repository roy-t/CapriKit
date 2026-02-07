using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using static Windows.Win32.PInvoke;

namespace CapriKit.Win32;

public sealed class Window
{
    internal HWND Handle { get; }
}

public sealed class WindowEventProcessor
{
    private readonly Window Target;

    public WindowEventProcessor(Window target)
    {
        Target = target;
    }

    internal void OnEvent(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (hWnd != Target.Handle)
            return;

        switch (msg)
        {
            case WM_SIZE:
                var width = EventDecoder.Loword((int)lParam.Value);
                var height = EventDecoder.Hiword((int)lParam.Value);

                switch (wParam.Value)
                {
                    case SIZE_RESTORED:
                    case SIZE_MAXIMIZED:
                    case SIZE_MINIMIZED:
                        window.Listener.OnSizeChanged(width, height);
                        break;
                }
                break;

            case WM_SETFOCUS:
                window.Listener.OnFocusChanged(true);
                break;

            case WM_KILLFOCUS:
                window.Listener.OnFocusChanged(false);
                break;

            case WM_ACTIVATE:
                window.Listener.OnFocusChanged(EventDecoder.Loword((int)wParam.Value) != 0);
                break;

            case WM_DESTROY:
                window.Listener.OnDestroyed();
                this.WindowEventListeners.RemoveAt(i);
                break;

            case WM_MOUSEMOVE:
                if (!window.HasMouseEntered)
                {
                    unsafe
                    {
                        var tme = new TRACKMOUSEEVENT()
                        {
                            cbSize = (uint)Marshal.SizeOf<TRACKMOUSEEVENT>(),
                            dwFlags = TRACKMOUSEEVENT_FLAGS.TME_LEAVE,
                            hwndTrack = hWnd,
                        };
                        TrackMouseEvent(ref tme);
                    }

                    window.Listener.OnMouseEnter();
                    window.HasMouseEntered = true;
                }
                window.Listener.OnMouseMove();
                break;

            case WM_MOUSELEAVE:
                window.Listener.OnMouseMove();
                window.Listener.OnMouseLeave();
                window.HasMouseEntered = false;

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
                SetCapture(hWnd);
                window.IsMouseCaptured = true;

                break;

            case WM_LBUTTONUP:
            case WM_RBUTTONUP:
            case WM_MBUTTONUP:
            case WM_XBUTTONUP:
                ReleaseCapture();
                window.IsMouseCaptured = false;

                break;

        }
    }
}
}
