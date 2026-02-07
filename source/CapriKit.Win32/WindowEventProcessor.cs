using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using static Windows.Win32.PInvoke;

namespace CapriKit.Win32;

[SupportedOSPlatform(WindowsVersions.WindowsXP)]
public sealed class Window
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    public bool IsMinimized => Width == 0 && Height == 0;

    public bool HasFocus { get; private set; }

    public bool HasCapturedMouse { get; private set; }

    // Top left is (0, 0)
    public Point GetCursorPosition()
    {
        if (!isCursorPositionKnown && GetCursorPos(out var pos) && ScreenToClient(Handle, ref pos))
        {
            cursorPosition = pos;
            isCursorPositionKnown = true;
        }

        return cursorPosition;
    }

    // Top left is (0.0, 0.0)
    public Vector2 GetCursorPositionF()
    {
        var point = GetCursorPosition();
        return new Vector2(point.X, point.Y);
    }

    // Top left is (0, 0)
    public void SetCursorPosition(Point position)
    {
        ClientToScreen(Handle, ref position);
        SetCursorPos(position.X, position.Y);
    }

    // Top left is (0.0, 0.0)
    public void SetCursorPosition(Vector2 position) => SetCursorPosition(new Point((int)position.X, (int)position.Y));

    private TRACKMOUSEEVENT trackMouseEventData;
    private bool isCursorPositionKnown;
    private bool isTrackingNextMouseLeave;
    private Point cursorPosition;

    internal Window(HWND handle)
    {
        trackMouseEventData = new TRACKMOUSEEVENT()
        {
            cbSize = (uint)Marshal.SizeOf<TRACKMOUSEEVENT>(),
            dwFlags = TRACKMOUSEEVENT_FLAGS.TME_LEAVE,
            hwndTrack = handle,
        };

        Handle = handle;
    }

    internal HWND Handle { get; }

    internal void OnSizeChanged(int width, int height)
    {
        Width = width;
        Height = height;
    }

    internal void OnFocusChanged(bool hasFocus)
    {
        HasFocus = hasFocus;
    }

    internal void OnMouseMove()
    {
        isCursorPositionKnown = false;
        if (!isTrackingNextMouseLeave)
        {
            // Get a message the next time (and only next time)
            // the mouse leaves the window area.
            TrackMouseEvent(ref trackMouseEventData);
            isTrackingNextMouseLeave = true;
            Win32Application.SetMouseCursor(Cursor.Arrow);
        }
    }

    internal void OnMouseLeave()
    {
        isCursorPositionKnown = false;
        isTrackingNextMouseLeave = false;
        Win32Application.SetMouseCursor(Cursor.Default);
    }

    internal void CaptureMouse()
    {
        SetCapture(Handle);
        HasCapturedMouse = true;
    }

    internal void ReleaseMouse()
    {
        ReleaseCapture();
        HasCapturedMouse = false;
    }
}

[SupportedOSPlatform(WindowsVersions.WindowsXP)]
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

        Debug.WriteLine($"W: {Target.Width}, H: {Target.Height}, Mi: {Target.IsMinimized}, F: {Target.HasFocus}, C: {Target.HasCapturedMouse}, P: {Target.GetCursorPosition()}");
    }
}
