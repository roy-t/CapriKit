namespace CapriKit.Win32.Input;

public abstract class InputDevice
{
    protected readonly InputState[] State;
    protected readonly InputState[] NextState;

    public InputDevice(int states)
    {
        State = new InputState[states];
        NextState = new InputState[states];
    }

    public virtual void NextFrame()
    {
        for (var i = 0; i < State.Length; i++)
        {
            State[i] = NextState[i];
            NextState[i] = NextState[i] switch
            {
                InputState.Pressed => InputState.Held,
                InputState.Held => InputState.Held,
                InputState.Released => InputState.None,
                InputState.None => InputState.None,
                _ => throw new ArgumentOutOfRangeException(nameof(InputState)),
            };
        }
    }
}
