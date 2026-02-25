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
    /// Triggered when the state machine transitions to this state.
    /// </summary>
    public virtual void OnEnter() { }

    /// <summary>
    /// Triggered after Main runs for the last time
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
    }

    public void PopState()
    {
        var state = States.Pop();
    }

    public void Update()
    {
        if (States.Count > 0)
        {
            var currentState = States.Peek();

            if (activeState != currentState)
            {
                activeState?.OnExit();
                currentState.OnEnter();
                activeState = currentState;
            }

            currentState.Main();
        }
        // Ensure OnExit also triggers for the last one
        else if (activeState != null)
        {
            activeState.OnExit();
            activeState = null;
        }
    }
}
