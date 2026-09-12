using CapriKit.AssetPipeline;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Debug;
using CapriKit.Tests.Tool.Tests;
using CapriKit.Win32;
using System.Diagnostics;

namespace CapriKit.Tests.Tool;

internal sealed class Bootstrapper
{
    private readonly Device Device;
    private readonly SwapChain SwapChain;
    private readonly ImGuiController Gui;
    private readonly AssetManager AssetManager;
    private readonly List<AssetBundle> Bundles;

    public Bootstrapper(Device device, SwapChain swapChain, ImGuiController gui, AssetManager assetManager)
    {
        Device = device;
        SwapChain = swapChain;
        Gui = gui;
        AssetManager = assetManager;
        ShaderTestBundle = ShaderTest.LoadBundle(assetManager);
        Bundles = [ShaderTestBundle];
    }

    public AssetBundle<ShaderTestBundle> ShaderTestBundle { get; }

    // TODO: convert to a proper frame loop with Update and Render steps
    // that take care of most of the boilerplate so that the loading screen
    // and later game loop just handle the relevant drawing/updates.
    public void Run()
    {
        var running = true;
        var elapsed = TimeSpan.FromSeconds(1.0 / 60.0);
        var timestamp = Stopwatch.GetTimestamp();

        while (running)
        {
            var context = Device.ImmediateDeviceContext;

            Gui.NewFrame((float)elapsed.TotalSeconds);
            SwapChain.Clear(context);
            context.OM.SetRenderTargetToBackBuffer(SwapChain);
            context.RS.SetViewport(SwapChain.Viewport);
            context.RS.SetScissorRect(SwapChain.Viewport);

            AssetManager.Update();

            Gui.Render(context);
            context.OM.UnsetRenderTargets();
            SwapChain.Present();

            running &= Win32Application.PumpMessages();

            elapsed = Stopwatch.GetElapsedTime(timestamp);
            timestamp = Stopwatch.GetTimestamp();


            if (Bundles.All(b => b.LoadingComplete))
            {
                return;
            }
        }
    }
}
