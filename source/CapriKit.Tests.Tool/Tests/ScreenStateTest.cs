using CapriKit.Tests.Tool.Tests.Framework;
using CapriKit.Win32;

namespace CapriKit.Tests.Tool.Tests;

internal sealed class ScreenStateTest : MultiStepTest
{
    private readonly Win32Window window;

    public ScreenStateTest(StateMachine stateMachine, Win32Window Window) : base(stateMachine)
    {
        window = Window;
    }

    protected override IReadOnlyList<IState> CreateSteps()
    {
        return [
                new EnterImmediateFullScreenState(StateMachine, this, window),
                new ExitImmediateFullScreenState(StateMachine, this, window)
            ];
    }

    private sealed class EnterImmediateFullScreenState(StateMachine stateMachine, ScreenStateTest test, Win32Window window) : AState(stateMachine)
    {
        private bool openOnNextRender;

        public override void OnEnter()
        {
            window.SwitchToBorderlessFullScreen();
            openOnNextRender = true;
        }

        public override void Main()
        {
            var result = TestUtilities.ShowModal("Did the window switch to borderless full screen?", openOnNextRender);
            openOnNextRender = false;

            if (result != ImmediateResult.Unknown)
            {
                test.AddResult(result);
                StateMachine.PopState();
            }
        }
    }

    private sealed class ExitImmediateFullScreenState(StateMachine stateMachine, ScreenStateTest test, Win32Window window) : AState(stateMachine)
    {
        private bool openOnNextRender;

        public override void OnEnter()
        {
            window.SwitchToWindowed();
            openOnNextRender = true;
        }

        public override void Main()
        {
            var result = TestUtilities.ShowModal("Did the window switch to windowed mode?", openOnNextRender);
            openOnNextRender = false;

            if (result != ImmediateResult.Unknown)
            {
                test.AddResult(result);
                StateMachine.PopState();
            }
        }
    }
}
