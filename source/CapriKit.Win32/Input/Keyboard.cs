using System.Numerics;
using System.Runtime.Versioning;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using static Windows.Win32.PInvoke;

namespace CapriKit.Win32.Input;

public sealed class Keyboard : InputDevice
{
    private string typed;
    private string nextTyped;

    internal Keyboard() : base(256)
    {
        typed = string.Empty;
        nextTyped = string.Empty;
    }

    /// <summary>
    /// Characters typed this frame
    /// </summary>
    public string Typed => typed;

    /// <summary>
    /// If the given button state changed to pressed this frame
    /// </summary>
    public bool Pressed(VirtualKeyCode key)
    {
        return State[(int)key] == InputState.Pressed;
    }

    /// <summary>
    /// If the given button was pressed both the previous and current frame
    /// </summary>  
    public bool Held(VirtualKeyCode key)
    {
        return State[(int)key] == InputState.Held;
    }

    /// <summary>
    /// If the given button state changed to released this frame
    /// </summary>
    public bool Released(VirtualKeyCode key)
    {
        return State[(int)key] == InputState.Released;
    }

    /// <summary>
    /// 1.0f if the button was an in the given state, 0.0f otherwise
    /// </summary>    
    public float AsFloat(InputState state, VirtualKeyCode key)
    {
        return State[(int)key] == state ? 1.0f : 0.0f;
    }

    /// <summary>
    /// A vector with for each given button 1.0f if the button was an in the given state, 0.0f otherwise
    /// </summary>
    public Vector2 AsVector(InputState state, VirtualKeyCode x, VirtualKeyCode y)
    {
        return new Vector2(AsFloat(state, x), AsFloat(state, y));
    }

    /// <summary>
    /// A vector with for each given button 1.0f if the button was an in the given state, 0.0f otherwise
    /// </summary>
    public Vector3 AsVector(InputState state, VirtualKeyCode x, VirtualKeyCode y, VirtualKeyCode z)
    {
        return new Vector3(AsFloat(state, x), AsFloat(state, y), AsFloat(state, z));
    }

    /// <summary>
    /// A vector with for each given button 1.0f if the button was an in the given state, 0.0f otherwise
    /// </summary>
    public Vector4 AsVector(InputState state, VirtualKeyCode x, VirtualKeyCode y, VirtualKeyCode z, VirtualKeyCode w)
    {
        return new Vector4(AsFloat(state, x), AsFloat(state, y), AsFloat(state, z), AsFloat(state, w));
    }

    public override void NextFrame()
    {
        typed = nextTyped;
        nextTyped = string.Empty;

        base.NextFrame();
    }

    [SupportedOSPlatform(WindowsVersions.WindowsXP)]
    public static uint GetScanCode(VirtualKeyCode key)
    {
        return MapVirtualKey((uint)key, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC);
    }

    [SupportedOSPlatform(WindowsVersions.WindowsXP)]
    public static VirtualKeyCode GetVirtualKeyCode(ushort scanCode)
    {
        return (VirtualKeyCode)MapVirtualKey(scanCode, MAP_VIRTUAL_KEY_TYPE.MAPVK_VSC_TO_VK);
    }

    internal void OnChar(char character)
    {
        nextTyped += character;
    }

    internal void OnKeyDown(VirtualKeyCode key)
    {
        NextState[(int)key] = InputState.Pressed;
    }

    internal void OnKeyUp(VirtualKeyCode key)
    {
        NextState[(int)key] = InputState.Released;
    }
}
