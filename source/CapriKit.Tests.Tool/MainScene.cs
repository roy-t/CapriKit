using CapriKit.DirectX11.Contexts;
using CapriKit.Tests.Tool.Tests.Framework;
using CapriKit.Win32.Input;
using ImGuiNET;

namespace CapriKit.Tests.Tool;

internal sealed class MainScene : IScene, IDisposable
{
    private readonly IReadOnlyList<ITestScreen> Tests;
    private readonly Keyboard Keyboard;
    private int currentTestIndex;

    public MainScene(Keyboard keyboard, IEnumerable<ITestScreen> tests)
    {
        Tests = [.. tests.OrderBy(t => t.Title)];
        currentTestIndex = 0;
        Keyboard = keyboard;
    }

    public void Update(DeviceContext context, float elapsed)
    {
        UpdateMenu();
        HandleInput();
        if (currentTestIndex >= 0 && currentTestIndex < Tests.Count)
        {
            Tests[currentTestIndex].Render(context, elapsed);
        }
    }
    private void HandleInput()
    {
        if (Keyboard.Pressed(VirtualKeyCode.VK_OEM_4)) // [
        {
            currentTestIndex = (currentTestIndex - 1 + Tests.Count) % Tests.Count;
        }
        if (Keyboard.Pressed(VirtualKeyCode.VK_OEM_6)) // ]
        {
            currentTestIndex = (currentTestIndex + 1) % Tests.Count;

        }
    }

    private void UpdateMenu()
    {
        ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Tests"))
            {
                for (var i = 0; i < Tests.Count; i++)
                {
                    var test = Tests[i];
                    if (ImGui.MenuItem($"{test.Title}", string.Empty, i == currentTestIndex))
                    {
                        currentTestIndex = i;
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
