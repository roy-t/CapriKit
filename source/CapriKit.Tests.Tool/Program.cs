using CapriKit.DirectX11;
using CapriKit.DirectX11.Debug;
using CapriKit.IO;
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
    static void Main()
    {
#if DEBUG // Ensure writes to console are redirected to Visual Studio
        Console.SetOut(new DebugOutputTextWriter());
#endif
        Win32Application.Initialize("CapriKit.Tests.Tool");

        // TODO: I don't like this construct, figure out how to mix async and sync methods
        // in the game loading and game loop. Be careful that a regular await
        // without a synchronization context will return on a different thread
        // so the Windows message pump doesn't work anymore.
        using var gameLoop = GameLoop.Create().GetAwaiter().GetResult();
        gameLoop.Run();
    }

    private sealed class GameLoop : IDisposable
    {
        public static async Task<GameLoop> Create()
        {
            var window = Win32Application.Window;
            var keyboard = Win32Application.Keyboard;
            var mouse = Win32Application.Mouse;

            var renderDoc = CommandLineArguments.IsPresent("--renderdoc")
                ? RenderDoc.TryLoad()
                : null;

            renderDoc?.DisableOverlay();

            var device = new Device();
            var swapChain = new SwapChain(device, window);
            var imGuiController = new ImGuiController(device, window, keyboard, mouse);
            var fileSystem = new FileSystem().ScopedToReadOnly(CommandLineArguments.GetArgumentValue("--content"));

            // TODO: Something here creates a live DirectX object that is not properly disposed of later!
            // probably the whole async mess, seperate async to only reading files?
            var shaderTest = await ShaderTest.Create(device, fileSystem);

            ITestScreen[] tests =
            [
                //shaderTest,
                new WindowStatesTest(window),
            ];

            return new GameLoop(window, mouse, keyboard, device, swapChain, renderDoc, imGuiController, tests);
        }


        private const double DELTA_TIME = 1.0 / 60.0; // constant tick rate of simulation

        private readonly Win32Window Window;
        private readonly Mouse Mouse;
        private readonly Keyboard Keyboard;

        private readonly Device Device;
        private readonly SwapChain SwapChain;
        private readonly RenderDoc? RenderDoc;
        private readonly ImGuiController ImGuiController;

        private readonly ITestScreen[] Tests;
        private ITestScreen CurrentTest;

        private bool running;

        private GameLoop(Win32Window window, Mouse mouse, Keyboard keyboard, Device device, SwapChain swapChain, RenderDoc? renderDoc, ImGuiController imGuiController, ITestScreen[] tests)
        {
            Window = window;
            Mouse = mouse;
            Keyboard = keyboard;
            Device = device;
            SwapChain = swapChain;
            RenderDoc = renderDoc;
            ImGuiController = imGuiController;
            Tests = tests;

            CurrentTest = Tests[0];
        }

        public void Run()  // TODO: main loop is getting a bit cluttered
        {
            running = true;
            var elapsed = DELTA_TIME;
            var stopwatch = Stopwatch.StartNew();

            while (running)
            {
                var context = Device.ImmediateDeviceContext;

                // Update
                ImGuiController.NewFrame((float)elapsed);
                HandleInput();
                HandleResize();

                // Render

                // here we decide where to render
                // (which surface and what part of the surface)                
                SwapChain.Clear(context);
                context.OM.SetRenderTargetToBackBuffer(SwapChain);
                context.RS.SetViewport(SwapChain.Viewport);
                context.RS.SetScissorRect(SwapChain.Viewport);


                UpdateMenu();

                // Individual components decide what and how to render
                // but for that they only need the device context
                CurrentTest.Render(context);
                ImGuiController.Render(context);

                context.OM.UnsetRenderTargets();
                SwapChain.Present();

                running &= Win32Application.PumpMessages();

                elapsed = stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();
            }

            AnalyzeRenderDocCaptures();
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

        public void Dispose()
        {
            foreach (var test in Tests)
            {
                test.Dispose();
            }
            ImGuiController.Dispose();
            SwapChain.Dispose();
            Device.Dispose();
            RenderDoc?.Dispose();
        }
    }
}
