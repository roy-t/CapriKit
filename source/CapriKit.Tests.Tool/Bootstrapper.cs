using CapriKit.AssetPipeline;
using CapriKit.DirectX11;
using CapriKit.IO;
using CapriKit.Win32;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CapriKit.Tests.Tool;

internal sealed class Bootstrapper
{
    public void Run()
    {
        Win32Application.Initialize("CapriKit.Tests.Tool", new WindowCreationOptions(0, 0, 1280, 1024, WindowOrigin.CenterOffset, WindowMeasure.ClientArea));

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddLogging(b => b.AddConsole());
        services.AddSingleton(Win32Application.Window);
        services.AddSingleton(Win32Application.Keyboard);
        services.AddSingleton(Win32Application.Mouse);

        services.AddSingleton<Device>();
        services.AddSingleton<SwapChain>();
        
        services.AddAssetPipeline(CLA("--content-input"), CLA("--content-output"));
        services.AddSingleton<GameLoop>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        var loop = provider.GetRequiredService<GameLoop>();

    }

    private class GameLoop(Device Device, SwapChain SwapChain) { }


    // Retrieves a required command line argument, or throws if it is missing
    private static string CLA(string argument)
    {
        if (!CommandLineArguments.IsPresent(argument)) { throw new Exception($"Missing required command line argument {argument}"); }
        return CommandLineArguments.GetArgumentValue(argument);
    }
}
