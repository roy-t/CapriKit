namespace CapriKit.Tests.Tool.Tests.Framework;

internal interface IImmediateTest
{
    public ImmediateResult Result { get; }

    public IState Create(StateMachine stateMachine);
}
