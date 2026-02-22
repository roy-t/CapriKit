using CapriKit.DirectX11;
using CapriKit.DirectX11.Debug;
using CapriKit.Tests.Tool.Tests;
using CapriKit.Win32;
using CapriKit.Win32.Input;
using ImGuiNET;
using System.Diagnostics;

namespace CapriKit.Tests.Tool;

public partial class Program
{

    private const double DELTA_TIME = 1.0 / 60.0; // constant tick rate of simulation

    private static ImmediateTest? currentTest = null;

    [STAThread]
    static void Main() // TODO: main loop is getting a bit cluttered
    {
#if DEBUG // Ensure writes to console are redirected to Visual Studio
        Console.SetOut(new DebugOutputTextWriter());
#endif
        Win32Application.Initialize("CapriKit.Tests.Tool");

        var window = Win32Application.Window;
        var mouse = Win32Application.Mouse;
        var keyboard = Win32Application.Keyboard;

        using var rd = RenderDoc.TryLoad();
        rd?.DisableOverlay();

        using var device = new Device(window);
        using var imgui = new ImGuiController(device, window, keyboard, mouse);
        ImmediateTest[] tests = [new BorderlessFullScreenTest(window)];

        var running = true;
        var elapsed = DELTA_TIME;
        var stopwatch = Stopwatch.StartNew();
        while (running)
        {
            imgui.NewFrame((float)elapsed);
            if (keyboard.Pressed(VirtualKeyCode.VK_ESCAPE))
            {
                running = false;
            }

            if (keyboard.Pressed(VirtualKeyCode.VK_F1))
            {
                rd?.TriggerCapture();
            }

            if (!window.IsMinimized && (window.Width != device.Width || window.Height != device.Height))
            {
                device.Resize(window.Width, window.Height);
                imgui.Resize(window.Width, window.Height);
            }


            device.Clear();
            ShowMenu(tests);
            if (currentTest != null && currentTest.LastResult == ImmediateResult.Unknown)
            {
                currentTest.Verify();
                if (currentTest.LastResult != ImmediateResult.Unknown)
                {
                    currentTest.Exit();
                    currentTest = null;
                }
            }

            imgui.Render();
            device.Present();

            running &= Win32Application.PumpMessages();

            elapsed = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();
        }

        // Open RenderDoc to analyze the last taken capture
        if (rd != null)
        {
            var numCaptures = rd.GetNumCaptures();
            if (numCaptures > 0)
            {
                var capture = rd.GetCapture(numCaptures - 1);
                rd.LaunchReplayUI(capture);
            }
        }
    }

    internal static void ShowMenu(ImmediateTest[] tests)
    {
        ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Tests"))
            {
                foreach (var test in tests)
                {
                    if (ImGui.MenuItem($"Test #1 ({test.LastResult})"))
                    {
                        currentTest = test;
                        test.Enter();
                    }
                }

                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }
    }
}
