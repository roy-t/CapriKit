using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using static Windows.Win32.PInvoke;

namespace CapriKit.Win32;

/// <summary>
/// State and helper functions for a Win32 Window
/// </summary>
[SupportedOSPlatform(WindowsVersions.WindowsXP)]
public sealed class Win32Window
{
    private int restoreX, restoreY, restoreWidth, restoreHeight;
    public nint Handle => Hwnd;

    public int X { get; private set; }
    public int Y { get; private set; }

    /// <summary>
    /// The width of the client area of the window.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// The height of the client area of the window.
    /// </summary>
    public int Height { get; private set; }

    public bool IsMinimized => Width == 0 && Height == 0;

    public bool IsBorderlessFullScreen { get; private set; }

    public bool HasFocus { get; private set; }

    /// <summary>
    /// The mouse is captured from the moment the user pressed a mouse button while over the window until the user releases the mouse button.
    /// </summary>
    public bool HasCapturedMouse { get; private set; }

    /// <summary>
    /// Retrieve the cursor position relative to the top left corner of the client area.
    /// </summary>
    public Point GetCursorPosition()
    {
        if (!isCursorPositionKnown && GetCursorPos(out var pos) && ScreenToClient(Hwnd, ref pos))
        {
            cursorPosition = pos;
            isCursorPositionKnown = true;
        }

        return cursorPosition;
    }

    /// <summary>
    /// Retrieve the cursor position relative to the top left corner of the client area.
    /// Convenience method to get the exact same values as `GetCursorPosition()` as floats. Values are NOT scaled to [0..1].
    /// </summary>
    public Vector2 GetCursorPositionF()
    {
        var point = GetCursorPosition();
        return new Vector2(point.X, point.Y);
    }

    /// <summary>
    /// Sets the cursor position relative to the top left corner of the client area.
    /// </summary>
    public void SetCursorPosition(Point position)
    {
        ClientToScreen(Hwnd, ref position);
        SetCursorPos(position.X, position.Y);
    }

    /// <summary>
    /// Sets the cursor position relative to the top left corner of the client area.
    /// Convenience method with same behavior as SetCursorPosition. Values should not be NOT scaled to [0..1].
    /// </summary>
    public void SetCursorPosition(Vector2 position) => SetCursorPosition(new Point((int)position.X, (int)position.Y));

    public void SwitchToBorderlessFullScreen()
    {
        restoreX = X; restoreY = Y; restoreWidth = Width; restoreHeight = Height;
        IsBorderlessFullScreen = Win32Utilities.MakeBorderlessFullscreen(Hwnd);
    }

    public void SwitchToWindowed()
    {
        IsBorderlessFullScreen = !Win32Utilities.MakeWindowed(Hwnd, restoreX, restoreY, restoreWidth, restoreHeight);
    }

    private TRACKMOUSEEVENT trackMouseEventData;
    private bool isCursorPositionKnown;
    private bool isTrackingNextMouseLeave;
    private Point cursorPosition;

    internal Win32Window()
    {
        UpdateHandle(HWND.Null);
    }

    internal void UpdateHandle(HWND handle)
    {
        trackMouseEventData = new TRACKMOUSEEVENT()
        {
            cbSize = (uint)Marshal.SizeOf<TRACKMOUSEEVENT>(),
            dwFlags = TRACKMOUSEEVENT_FLAGS.TME_LEAVE,
            hwndTrack = handle,
        };
        Hwnd = handle;
    }

    internal HWND Hwnd { get; private set; }

    internal void OnMove(int x, int y)
    {
        X = x;
        Y = y;
    }
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
            Win32Application.SetMouseCursor(CursorStyle.Arrow);
        }
    }

    internal void OnMouseLeave()
    {
        isCursorPositionKnown = false;
        isTrackingNextMouseLeave = false;
        Win32Application.SetMouseCursor(CursorStyle.Default);
    }

    internal void CaptureMouse()
    {
        SetCapture(Hwnd);
        HasCapturedMouse = true;
    }

    internal void ReleaseMouse()
    {
        ReleaseCapture();
        HasCapturedMouse = false;
    }
}
