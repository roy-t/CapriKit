namespace CapriKit.Tests.Tool.Tests;


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
    public virtual void OnEnter() { }
    public virtual void OnExit() { }
}

internal sealed class StateMachine
{
    private readonly Stack<IState> States = new();

    public void PushState(IState state)
    {
        States.Push(state);
        state.OnEnter();
    }

    public void PopState()
    {
        var state = States.Pop();
        state.OnExit();
    }


    public void Update()
    {
        if (States.Count > 0)
        {
            var state = States.Peek();
            state.Main();
        }
    }
}
