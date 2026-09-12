using CapriKit.AssetPipeline;
using CapriKit.AssetPipeline.DirectX11;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Debug;
using CapriKit.IO;
using CapriKit.Tests.Tool.Tests;
using CapriKit.Tests.Tool.Tests.Framework;
using CapriKit.Win32;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CapriKit.Tests.Tool;

internal sealed class Program
{
    public static void Main()
    {
        Win32Application.Initialize("CapriKit.Tests.Tool", new WindowCreationOptions(0, 0, 1280, 1024, WindowOrigin.CenterOffset, WindowMeasure.ClientArea));

        var services = new ServiceCollection();
        LoadRenderDoc(services);

        services.AddLogging(b => b.AddDebug());
        services.AddLogging(b => b.AddConsole());
        services.AddSingleton(Win32Application.Window);
        services.AddSingleton(Win32Application.Keyboard);
        services.AddSingleton(Win32Application.Mouse);

        services.AddDirectX11();
        services.AddAssetPipeline(GetArg("--content-input"), GetArg("--content-output"));
        services.AddDirectX11AssetTranscoders();

        services.AddSingleton<GameLoop>();
        AddTestFactories(services);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        var gameLoop = provider.GetRequiredService<GameLoop>();
        gameLoop.Scene = new LoadingScene();
        gameLoop.Run();

        UnloadRenderDoc(provider);
    }

    private static void AddTestFactories(ServiceCollection services)
    {
        services.AddSingleton<ITestFactory>(provider => ShaderTest.CreateFactory(provider.GetRequiredService<AssetManager>()));
    }

    private static void LoadRenderDoc(ServiceCollection services)
    {
        if (CommandLineArguments.IsPresent("--renderdoc"))
        {
            var renderDoc = RenderDoc.TryLoad();
            if (renderDoc != null)
            {
                renderDoc.DisableOverlay();
                services.AddSingleton(renderDoc);
            }
        }
    }

    private static void UnloadRenderDoc(ServiceProvider provider)
    {
        var renderDoc = provider.GetService<RenderDoc>();
        renderDoc?.Dispose();
    }

    // Retrieves a required command line argument, or throws if it is missing
    private static string GetArg(string argument)
    {
        if (!CommandLineArguments.IsPresent(argument)) { throw new Exception($"Missing required command line argument {argument}"); }
        return CommandLineArguments.GetArgumentValue(argument);
    }
}
