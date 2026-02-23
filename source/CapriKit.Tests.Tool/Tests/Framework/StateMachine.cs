namespace CapriKit.Tests.Tool.Tests.Framework;


internal interface IState
{
    void Main();
    void OnEnter();
    void OnExit();
}

internal abstract class AState : IState
{
    protected readonly StateMachine StateMachine;

    protected AState(StateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public abstract void Main();

    /// <summary>
    /// Triggered everytime this state becomes active
    /// </summary>
    public virtual void OnEnter() { }

    /// <summary>
    /// Triggered everytime this state becomes inactive
    /// </summary>
    public virtual void OnExit() { }
}

internal sealed class StateMachine
{
    private readonly Stack<IState> States = new();
    private IState? activeState;

    public void PushState(IState state)
    {
        States.Push(state);
        state.OnEnter();
    }

    public void PopState()
    {
        var state = States.Pop();
        if (state == activeState)
        {
            activeState.OnExit();
            activeState = null;
        }
    }

    public void Update()
    {
        if (States.Count > 0)
        {
            var state = States.Peek();
            if (activeState == null || activeState != state)
            {
                activeState = state;
                state.OnEnter();
            }

            state.Main();
        }
    }
}
