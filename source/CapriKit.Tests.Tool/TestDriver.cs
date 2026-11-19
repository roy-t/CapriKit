using CapriKit.AssetPipeline;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Debug;
using CapriKit.Tests.Tool.Performance;
using CapriKit.Tests.Tool.Tests.Framework;
using CapriKit.Win32;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CapriKit.Tests.Tool;

internal sealed partial class TestDriver
{
    private readonly IServiceProvider Provider;
    private readonly ILogger<TestDriver> Logger;

    private readonly AssetManager AssetManager;
    private readonly Device Device;
    private readonly SwapChain SwapChain;

    public TestDriver(ILoggerFactory logger, IServiceProvider provider, AssetManager assetManager,
        Device device, SwapChain swapChain)
    {
        Logger = logger.CreateLogger<TestDriver>();
        Provider = provider;

        AssetManager = assetManager;
        Device = device;
        SwapChain = swapChain;
    }


    public void Run(string testClass)
    {
        // Tests without assets have no bundle
        if (Provider.GetKeyedService<AssetBundle>(testClass) is { } bundle)
        {
            while (!bundle.LoadingComplete)
            {
                LogLoadingAssets(Logger, testClass, bundle.Loaded, bundle.Total);
                AssetManager.Update();
                Thread.Sleep(100);
            }
        }

        var test = Provider.GetRequiredKeyedService<ITestScreen>(testClass);
        var start = Stopwatch.GetTimestamp();

        var context = Device.ImmediateDeviceContext;
        using var depth = new DepthStencilBuffer(Device, SwapChain.Width, SwapChain.Height);

        var collector = new PerformanceDataCollector();
        using var gpuProfiler = new GpuProfiler(Device, "Scene");
        var cpuProfiler = new CpuProfiler("Scene");

        while (Win32Application.PumpMessages())
        {
            cpuProfiler.BeginFrame();
            gpuProfiler.BeginFrame(context);

            SwapChain.Clear(context, depth);
            context.OM.SetRenderTargetToBackBuffer(SwapChain, depth);
            context.RS.SetViewport(SwapChain.Viewport);
            context.RS.SetScissorRect(SwapChain.Viewport);

            test.Render(context, 1.0f / 60.0f);

            context.OM.UnsetRenderTargets();
            SwapChain.Present();

            cpuProfiler.EndFrame();
            gpuProfiler.EndFrame(context);
            collector.Collect(cpuProfiler);
            collector.Collect(gpuProfiler);

            var now = Stopwatch.GetTimestamp();
            if (Stopwatch.GetElapsedTime(start, now) > TimeSpan.FromSeconds(5))
            {
                return;
            }

            // TODO: add some warm-up time, reset measurements, run for a while, export data
        }
    }


    [LoggerMessage(Level = LogLevel.Information, Message = "Loading asset bundle for {className} {completed}/{total}")]
    private static partial void LogLoadingAssets(ILogger logger, string className, int completed, int total);
}
