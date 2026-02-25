using CapriKit.DirectX11;
using CapriKit.DirectX11.Debug;
using CapriKit.Tests.Tool.Tests;
using CapriKit.Tests.Tool.Tests.Framework;
using CapriKit.Win32;
using CapriKit.Win32.Input;
using ImGuiNET;
using System.Diagnostics;

namespace CapriKit.Tests.Tool;

public partial class Program
{
    [STAThread]
    static void Main() // TODO: main loop is getting a bit cluttered
    {
#if DEBUG // Ensure writes to console are redirected to Visual Studio
        Console.SetOut(new DebugOutputTextWriter());
#endif
        Win32Application.Initialize("CapriKit.Tests.Tool");
        using var gameLoop = new GameLoop();
        gameLoop.Run();
    }

    private sealed class GameLoop : IDisposable
    {
        private const double DELTA_TIME = 1.0 / 60.0; // constant tick rate of simulation

        private readonly Win32Window Window;
        private readonly Mouse Mouse;
        private readonly Keyboard Keyboard;

        private readonly Device Device;
        private readonly SwapChain SwapChain;
        private readonly RenderDoc? RenderDoc;
        private readonly ImGuiController ImGuiController;

        private readonly StateMachine StateMachine;
        private readonly IImmediateTest[] Tests;

        private bool running;

        public GameLoop()
        {
            Window = Win32Application.Window;
            Mouse = Win32Application.Mouse;
            Keyboard = Win32Application.Keyboard;

            RenderDoc = EnableRenderDoc()
                ? RenderDoc.TryLoad()
                : null;

            RenderDoc?.DisableOverlay();

            Device = new Device();
            SwapChain = new SwapChain(Device, Window);
            ImGuiController = new ImGuiController(Device, Window, Keyboard, Mouse);

            StateMachine = new StateMachine();
            Tests = [new ScreenStateTest(StateMachine, Window)];
        }

        public void Run()
        {
            running = true;
            var elapsed = DELTA_TIME;
            var stopwatch = Stopwatch.StartNew();

            while (running)
            {
                ImGuiController.NewFrame((float)elapsed);
                HandleInput();
                HandleResize();

                UpdateMenu(StateMachine, Tests);
                StateMachine.Update();

                SwapChain.Clear(Device.ImmediateDeviceContext);
                Device.ImmediateDeviceContext.OM.SetRenderTargetToBackBuffer(SwapChain);

                ImGuiController.Render();

                Device.ImmediateDeviceContext.OM.UnsetRenderTargets();
                SwapChain.Present();

                running &= Win32Application.PumpMessages();

                elapsed = stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();
            }

            AnalyzeRenderDocCaptures();
        }

        internal static void UpdateMenu(StateMachine stateMachine, IImmediateTest[] tests)
        {
            ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("Tests"))
                {
                    foreach (var test in tests)
                    {
                        if (ImGui.MenuItem($"Test #1 ({test.Result})"))
                        {
                            var state = test.Create(stateMachine);
                            stateMachine.PushState(state);
                        }
                    }

                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }
        }

        private void HandleResize()
        {
            if (!Window.IsMinimized && (Window.Width != SwapChain.Width || Window.Height != SwapChain.Height))
            {
                SwapChain.Resize(Device, Window.Width, Window.Height);
                ImGuiController.Resize(Window.Width, Window.Height);
            }
        }

        private void HandleInput()
        {
            if (Keyboard.Pressed(VirtualKeyCode.VK_ESCAPE))
            {
                running = false;
            }

            if (Keyboard.Pressed(VirtualKeyCode.VK_F1))
            {
                RenderDoc?.TriggerCapture();
            }
        }

        private void AnalyzeRenderDocCaptures()
        {
            // Open RenderDoc to analyze the last taken capture
            if (RenderDoc != null)
            {
                var numCaptures = RenderDoc.GetNumCaptures();
                if (numCaptures > 0)
                {
                    var capture = RenderDoc.GetCapture(numCaptures - 1);
                    RenderDoc.LaunchReplayUI(capture);
                }
            }
        }

        private static bool EnableRenderDoc()
        {
            var args = Environment.GetCommandLineArgs();
            return args.Any(s => s.Equals("--renderdoc", StringComparison.InvariantCultureIgnoreCase));
        }

        public void Dispose()
        {
            ImGuiController.Dispose();
            SwapChain.Dispose();
            Device.Dispose();
            RenderDoc?.Dispose();
        }
    }
}
