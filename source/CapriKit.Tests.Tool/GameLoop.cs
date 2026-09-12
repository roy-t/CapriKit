using CapriKit.AssetPipeline;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Debug;
using CapriKit.Win32;
using System.Diagnostics;

namespace CapriKit.Tests.Tool;

internal sealed class GameLoop
{
    private readonly Device Device;
    private readonly SwapChain SwapChain;
    private readonly ImGuiController Gui;
    private readonly AssetManager AssetManager;
    private readonly RenderDoc? RenderDoc;

    public GameLoop(Device device, SwapChain swapChain, ImGuiController gui, AssetManager assetManager, RenderDoc? renderDoc = null)
    {
        Device = device;
        SwapChain = swapChain;
        Gui = gui;
        AssetManager = assetManager;
        Scene = new DefaultScene();
        RenderDoc = renderDoc;
    }

    public IScene Scene { get; set; }


    public void Run()
    {
        var running = true;
        var timestamp = Stopwatch.GetTimestamp();
        var timespan = TimeSpan.FromSeconds(1.0 / 60.0);
        float elapsed;

        var context = Device.ImmediateDeviceContext;

        while (running)
        {
            elapsed = (float)timespan.TotalSeconds;

            // New frame prep
            Gui.NewFrame(elapsed);
            AssetManager.Update();

            // Set render output
            SwapChain.Clear(context);
            context.OM.SetRenderTargetToBackBuffer(SwapChain);
            context.RS.SetViewport(SwapChain.Viewport);
            context.RS.SetScissorRect(SwapChain.Viewport);

            // Draw scene
            Scene.Update(elapsed);

            // Draw GUI
            Gui.Render(context);

            // Present
            context.OM.UnsetRenderTargets();
            SwapChain.Present();

            // Bookkeeping
            running &= Win32Application.PumpMessages();
            timespan = Stopwatch.GetElapsedTime(timestamp);
            timestamp = Stopwatch.GetTimestamp();
        }

        AnalyzeRenderDocCaptures();
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
}

internal interface IScene
{
    void Update(float elapsed);
}

internal sealed class DefaultScene : IScene
{
    public void Update(float _) { }
}
