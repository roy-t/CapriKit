namespace CapriKit.Win32.Input;

/// <summary>
/// Mouse button enum, via the Windows API we can only get the state of the five most important mouse buttons
/// </summary>
public enum MouseButton : ushort
{
    Left = 0,
    Right = 1,
    Middle = 2,
    Four = 3,
    Five = 4
}
