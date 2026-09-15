using CapriKit.DirectX11.Contexts;
using CapriKit.Tests.Tool.Tests.Framework;
using ImGuiNET;

namespace CapriKit.Tests.Tool;

internal sealed class MainScene : IScene, IDisposable
{
    private readonly IEnumerable<ITestScreen> Tests;
    private ITestScreen? CurrentTest;

    public MainScene(IEnumerable<ITestScreen> tests)
    {
        Tests = tests;
        CurrentTest = tests.FirstOrDefault();
    }

    public void Update(DeviceContext context, float elapsed)
    {
        UpdateMenu();
        CurrentTest?.Render(context);
    }

    private void UpdateMenu()
    {
        ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Tests"))
            {
                foreach (var test in Tests)
                {
                    if (ImGui.MenuItem($"{test.Title}", string.Empty, CurrentTest == test))
                    {
                        CurrentTest = test;
                    }
                }

                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }
    }

    public void Dispose()
    {
        foreach (var test in Tests)
        {
            test.Dispose();
        }
    }
}
