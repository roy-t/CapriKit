using System.Drawing;
using System.Numerics;

namespace CapriKit.Win32.Input;

/// <summary>
/// Keeps track of Win32 events to capture the state of the mouse device in an easy to use wrapper.
/// </summary>
public sealed class Mouse : InputDevice
{
    private const int WHEEL_DELTA = 120;

    private int scrollState;
    private int nextScrollState;
    private int hScrollState;
    private int nextHScrollState;
    private Point position;
    private Point nextPostion;
    private Point movement;

    public Mouse() : base(Enum.GetValues<MouseButton>().Length) { }

    /// <summary>
    /// Relative movement per frame in pixels
    /// </summary>
    public Point Movement => movement;

    /// <summary>
    /// Relative moment per frame in pixels, convenience to get the exact same values from the Movement property as floats
    /// </summary>
    public Vector2 MovementF => new(movement.X, movement.Y);

    /// <summary>
    /// The position of the mouse cursor relative to the upper left corner of the client area of main window, in pixels
    /// </summary>
    public Point Position => position;

    /// <summary>
    /// The position of the mouse cursor, convenience to get the exact same values from the Position property as floats
    /// </summary>
    public Vector2 PositionF => new(position.X, position.Y);

    /// <summary>
    /// The steps (notches/clicks) the scroll wheel scrolled vertically in this frame.
    /// A positive value indicates that the wheel was rotated forward, away from the user;
    /// a negative value indicates that the wheel was rotated backward, towards the user.
    /// </summary>
    public int ScrollSteps => scrollState / WHEEL_DELTA;

    /// <summary>
    /// The steps (notches/clicks) the scroll wheel scrolled horizontally this frame.
    /// A positive value indicates that the wheel was rotated to the right;
    /// a negative value indicates that the wheel was rotated to the left.
    /// </summary>
    public int HorizontalScrollSteps => hScrollState / WHEEL_DELTA;    

    /// <summary>
    /// If the given button state changed to pressed this frame
    /// </summary>        
    public bool Pressed(MouseButton button)
    {
        return State[(int)button] == InputState.Pressed;
    }

    /// <summary>
    /// If the given button was pressed both the previous and current frame
    /// </summary>        
    public bool Held(MouseButton button)
    {
        return State[(int)button] == InputState.Held;
    }

    /// <summary>
    /// If the given button state changed to released this frame
    /// </summary>
    public bool Released(MouseButton button)
    {
        return State[(int)button] == InputState.Released;
    }

    internal void OnButtonDown(MouseButton button)
    {
        NextState[(int)button] = InputState.Pressed;
    }

    internal void OnButtonUp(MouseButton button)
    {
        NextState[(int)button] = InputState.Released;
    }

    internal void OnHScroll(int delta)
    {
        nextHScrollState += delta;
    }

    internal void OnScroll(int delta)
    {
        nextScrollState += delta;
    }

    internal void UpdatePosition(Point position)
    {
        nextPostion = position;
    }

    public override void NextFrame()
    {
        // TODO: we need to keep track of the increments for mice with smooth scrolling. Otherwise they can't scroll as they will report values less then WHEEL_DETLA per frame
        // be careful with modulo for negative numbers.
        scrollState = nextScrollState;
        nextScrollState = 0;

        hScrollState = nextHScrollState;
        nextHScrollState = 0;

        movement = new Point(nextPostion.X - position.X, nextPostion.Y - position.Y);
        position = nextPostion;

        base.NextFrame();
    }
}
