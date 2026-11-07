using CapriKit.AssetPipeline;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Debug;
using CapriKit.Win32;
using CapriKit.Win32.Input;
using System.Diagnostics;

namespace CapriKit.Tests.Tool;

internal sealed class GameLoop
{
    private readonly Win32Window Window;
    private readonly Keyboard Keyboard;
    private readonly Device Device;
    private readonly SwapChain SwapChain;
    private readonly ImGuiController Gui;
    private readonly AssetManager AssetManager;
    private readonly PerformanceDataCollector Performance;
    private readonly RenderDoc? RenderDoc;

    private DepthStencilBuffer depth;

    private IScene currentScene;
    private IScene? nextScene;

    public GameLoop(Win32Window window, Keyboard keyboard, Device device, SwapChain swapChain, ImGuiController gui, AssetManager assetManager, PerformanceDataCollector performance, RenderDoc? renderDoc = null)
    {
        Window = window;
        Keyboard = keyboard;
        Device = device;
        SwapChain = swapChain;
        Gui = gui;
        AssetManager = assetManager;
        currentScene = new DefaultScene();
        Performance = performance;
        RenderDoc = renderDoc;
        nextScene = null;

        depth = new DepthStencilBuffer(device, swapChain.Width, swapChain.Height);
    }

    public void Run()
    {
        var running = true;
        var timestamp = Stopwatch.GetTimestamp();
        var timespan = TimeSpan.FromSeconds(1.0 / 60.0);
        float elapsed;

        var context = Device.ImmediateDeviceContext;

        using var gpuProfiler = new GpuProfiler(Device, "Setup", "Scene", "Gui");
        var cpuProfiler = new CpuProfiler("Setup", "Scene", "Gui", "Present");
        while (running)
        {
            // CPU setup also includes switching scenes, starting the GUI frame and updating assets
            cpuProfiler.BeginFrame();
            if (nextScene != null)
            {
                currentScene.Dispose(); // TODO: this assumes that a scene cannot be reused
                currentScene = nextScene;
                nextScene = null;
            }
            elapsed = (float)timespan.TotalSeconds;

            // New frame prep
            Gui.NewFrame(elapsed);
            AssetManager.Update();

            gpuProfiler.BeginFrame(context);

            // Set render output
            SwapChain.Clear(context, depth);
            context.OM.SetRenderTargetToBackBuffer(SwapChain, depth);
            context.RS.SetViewport(SwapChain.Viewport);
            context.RS.SetScissorRect(SwapChain.Viewport);
            gpuProfiler.EndSection(context, 0); // Setup
            cpuProfiler.EndSection(0);

            // Draw scene
            currentScene.Update(context, elapsed);
            gpuProfiler.EndSection(context, 1); // Scene
            cpuProfiler.EndSection(1);

            // Draw GUI
            Gui.Render(context);
            gpuProfiler.EndSection(context, 2); // Gui
            cpuProfiler.EndSection(2);

            // Present
            context.OM.UnsetRenderTargets();
            if (gpuProfiler.EndFrame(context))
            {
                Performance.Collect(gpuProfiler);
            }
            SwapChain.Present();
            cpuProfiler.EndFrame(); // Present
            Performance.Collect(cpuProfiler);

            // Bookkeeping
            HandleResize();
            running &= HandleInput();
            running &= Win32Application.PumpMessages();
            timespan = Stopwatch.GetElapsedTime(timestamp);
            timestamp = Stopwatch.GetTimestamp();
        }
        depth.Dispose();
        currentScene.Dispose();
        AnalyzeRenderDocCaptures();
    }

    public void ChangeScene(IScene scene)
    {
        nextScene = scene;
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

    private void HandleResize()
    {
        if (!Window.IsMinimized && (Window.Width != SwapChain.Width || Window.Height != SwapChain.Height))
        {
            SwapChain.Resize(Device, Window.Width, Window.Height);
            Gui.Resize(Window.Width, Window.Height);

            depth.Dispose();
            depth = new DepthStencilBuffer(Device, Window.Width, Window.Height);
        }
    }

    private bool HandleInput()
    {
        if (Keyboard.Pressed(VirtualKeyCode.VK_ESCAPE))
        {
            return false;
        }

        if (Keyboard.Pressed(VirtualKeyCode.VK_F1))
        {
            RenderDoc?.TriggerCapture();
        }

        return true;
    }
}

internal interface IScene : IDisposable
{
    void Update(DeviceContext context, float elapsed);
}

internal sealed class DefaultScene : IScene
{
    public void Update(DeviceContext _, float __) { }

    public void Dispose() { }
}
