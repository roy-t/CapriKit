using CapriKit.Win32;
using ImGuiNET;
using System.Numerics;

namespace CapriKit.Tests.Tool.Tests;

// TODO: instead use a state machine for the tests.
// The bottom state should have a list of all tests we want to run
// and walk through them one by one.
// Then each tests should just ask confirmation, or push more steps in the test to get through it
// results should be stored in a way that is accessible for the UI. Maybe as a TestDescription class
// that contains the last result and factory method to get the initial state machine for the test.

internal enum ImmediateResult
{
    Unknown,
    Success,
    Failure
}


internal abstract class ImmediateTest
{
    private bool openOnNextRender;

    public ImmediateResult LastResult { get; private set; }

    public virtual void Enter()
    {
        LastResult = ImmediateResult.Unknown;
        openOnNextRender = true;
    }
    public virtual void Exit() { }

    public ImmediateResult Verify()
    {
        LastResult = VerifyInternal();
        return LastResult;
    }

    protected abstract ImmediateResult VerifyInternal();

    protected ImmediateResult ShowPopup(string message)
    {
        if (openOnNextRender)
        {
            ImGui.OpenPopup(nameof(ImmediateTest));
            openOnNextRender = false;
        }

        var result = ImmediateResult.Unknown;

        if (ImGui.BeginPopupModal(nameof(ImmediateTest), ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text(message);
            ImGui.Spacing();

            if (ImGui.Button("Yes", new Vector2(120, 0)))
            {
                result = ImmediateResult.Success;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SetItemDefaultFocus();

            ImGui.SameLine();
            if (ImGui.Button("No", new Vector2(120, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        return result;
    }
}

internal sealed class BorderlessFullScreenTest : ImmediateTest
{
    private readonly Win32Window Window;

    public BorderlessFullScreenTest(Win32Window window)
    {
        Window = window;
    }

    public override void Enter()
    {
        Window.SwitchToBorderlessFullScreen();
        base.Enter();
    }

    public override void Exit()
    {
        Window.SwitchToWindowed();
        base.Exit();
    }

    protected override ImmediateResult VerifyInternal()
    {
        return ShowPopup("Did the window switch to borderless full screen?");
    }
}
