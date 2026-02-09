using CapriKit.Win32.Input;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace CapriKit.Win32;

/// <summary>
/// Decodes Win32 messages that contain additional data in the WPARAM and LPARAM parameters
/// </summary>
internal static class EventDecoder
{
    public static MouseButton GetMouseButton(uint msg, WPARAM wParam, LPARAM lParam)
    {
        return msg switch
        {
            WM_LBUTTONDOWN or WM_LBUTTONDBLCLK or WM_LBUTTONUP => MouseButton.Left,
            WM_RBUTTONDOWN or WM_RBUTTONDBLCLK or WM_RBUTTONUP => MouseButton.Right,
            WM_MBUTTONDOWN or WM_MBUTTONDBLCLK or WM_MBUTTONUP => MouseButton.Middle,
            WM_XBUTTONDOWN or WM_XBUTTONDBLCLK or WM_XBUTTONUP => GetXButtonWParam(wParam) == 1
                                ? MouseButton.Four
                                : MouseButton.Five,
            _ => throw new ArgumentOutOfRangeException(nameof(msg)),
        };
    }

    /// <summary>
    /// Note: that Microsoft defines one 'notch' on the scroll wheel as 120 units of movements
    /// </summary>
    public static int GetMouseWheelDelta(WPARAM wParam)
    {
        return Hiword((int)wParam.Value);
    }

    public static VirtualKeyCode GetKeyCode(WPARAM wParam)
    {
        return (VirtualKeyCode)wParam.Value;
    }

    private static int GetXButtonWParam(WPARAM wParam)
    {
        return Hiword((int)wParam.Value);
    }

    public static int Loword(int number)
    {
        return number & 0x0000FFFF;
    }

    public static int Hiword(int number)
    {
        return number >> 16;
    }
}
