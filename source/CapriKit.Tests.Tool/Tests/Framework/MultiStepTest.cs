namespace CapriKit.Tests.Tool.Tests.Framework;

internal abstract class MultiStepTest(StateMachine stateMachine)
    : AState(stateMachine), IImmediateTest
{
    private readonly List<ImmediateResult> Results = [];
    private int expectedResults = 0;

    public void AddResult(ImmediateResult result)
    {
        Results.Add(result);

    }

    public ImmediateResult Result
    {
        get
        {
            if (Results.Count == 0)
            {
                return ImmediateResult.Unknown;
            }
            return Results.Min();
        }
    }

    public override void OnEnter()
    {
        if (expectedResults == 0)
        {
            Results.Clear();
            var steps = CreateSteps();
            expectedResults = steps.Count;

            for (var i = steps.Count - 1; i >= 0; i--)
            {
                StateMachine.PushState(steps[i]);
            }
        }
    }

    public override void Main()
    {
        if (Results.Count == expectedResults)
        {
            expectedResults = 0;
            StateMachine.PopState();
        }
    }

    public IState Create(StateMachine stateMachine)
    {
        return this;
    }

    protected abstract IReadOnlyList<IState> CreateSteps();
}
