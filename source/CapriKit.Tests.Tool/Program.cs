using CapriKit.DirectX11;
using CapriKit.DirectX11.Debug;
using CapriKit.IO;
using CapriKit.Tests.Tool.Tests;
using CapriKit.Tests.Tool.Tests.Framework;
using CapriKit.Win32;
using CapriKit.Win32.Input;
using ImGuiNET;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace CapriKit.Tests.Tool;

public partial class Program
{
    [STAThread]
    static void Main()
    {
#if DEBUG // Ensure writes to console are redirected to Visual Studio
        Console.SetOut(new DebugOutputTextWriter());
#endif
        Win32Application.Initialize("CapriKit.Tests.Tool", new WindowCreationOptions(0, 0, 1280, 1024, WindowOrigin.CenterOffset, WindowMeasure.ClientArea));
        using var gameLoop = new GameLoop();
        gameLoop.Run();
    }

    private sealed class GameLoop : IDisposable
    {
        private const double DELTA_TIME = 1.0 / 60.0; // constant tick rate of simulation

        private readonly Win32Window Window;
        private readonly Mouse Mouse;
        private readonly Keyboard Keyboard;

        private readonly IReadOnlyVirtualFileSystem FileSystem;

        private readonly Device Device;
        private readonly SwapChain SwapChain;
        private readonly RenderDoc? RenderDoc;
        private readonly ImGuiController ImGuiController;

        private readonly List<ITestScreen> Tests;
        private ITestScreen? CurrentTest;

        private bool running;

        public GameLoop()
        {
            Window = Win32Application.Window;
            Keyboard = Win32Application.Keyboard;
            Mouse = Win32Application.Mouse;

            RenderDoc = CommandLineArguments.IsPresent("--renderdoc")
                ? RenderDoc.TryLoad()
                : null;

            RenderDoc?.DisableOverlay();

            FileSystem = new FileSystem().ScopedToReadOnly(CommandLineArguments.GetArgumentValue("--content"));

            Device = new Device();
            SwapChain = new SwapChain(Device, Window);
            ImGuiController = new ImGuiController(Device, Window, Keyboard, Mouse);

            Tests = [];
            CurrentTest = null;
        }

        public void Run()  // TODO: main loop is getting a bit cluttered
        {
            using var loader = StartLoadingTests();

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
                CurrentTest?.Render(context);
                ImGuiController.Render(context);

                context.OM.UnsetRenderTargets();
                SwapChain.Present();

                running &= Win32Application.PumpMessages();

                elapsed = stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();

                InsertLoadedTests(loader);
            }

            // Cancel outstanding jobs, then wait for in-flight ones; they may still
            // be using the device, and their results would otherwise never be disposed
            loader.Cancel();
            loader.DrainAndDisposeRemaining();

            AnalyzeRenderDocCaptures();
        }


        private TestScreenLoader StartLoadingTests()
        {
            var loader = new TestScreenLoader();

            loader.StartWork([
                    new Job<ITestScreen>(nameof(ShaderTest), async token => await ShaderTest.Create(Device, FileSystem, token)),
                    new Job<ITestScreen>(nameof(WindowStatesTest), async token => new WindowStatesTest(Window)),
                ]);

            return loader;
        }

        private void InsertLoadedTests(TestScreenLoader loader)
        {
            while (loader.TryDequeue(out var loaded))
            {
                if (loaded.Exception != null)
                {
                    // Rethrow without destroying the original stack trace
                    ExceptionDispatchInfo.Capture(loaded.Exception).Throw();
                }

                if (loaded.Item != null)
                {
                    Tests.Add(loaded.Item);
                    // Hack to set preferred test independent of loading order
                    if (loaded.Id == nameof(ShaderTest))
                    {
                        CurrentTest = Tests[^1];
                    }
                }
            }
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
