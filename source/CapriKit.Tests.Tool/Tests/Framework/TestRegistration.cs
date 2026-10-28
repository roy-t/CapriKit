using CapriKit.AssetPipeline;
using CapriKit.Tests.Tool.Tests.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace CapriKit.Tests.Tool.Tests.Framework2;

internal static class TestRegistration
{
    public static IServiceCollection AddTest<TTest>(this IServiceCollection services)
        where TTest : class, ITestScreen
    {
        return services.AddSingleton<ITestScreen, TTest>();
    }

    public static IServiceCollection AddBundle<TContent>(this IServiceCollection services, string key, Func<AssetManager, AssetBundle<TContent>> create)
        where TContent : class
    {
        // Create a generic bundle factory that the loading screen can use to check if all assets in the bundle are ready
        // these can be discarded immediately after loading since bundles use reference counting internally
        services.AddSingleton<AssetBundle>(sp => sp.GetRequiredService<AssetBundle<TContent>>());

        // Create a keyed bundle factory so that each dependent get its own bundle instance.
        services.AddKeyedSingleton(key, (sp, _) => create(sp.GetRequiredService<AssetManager>()));

        // Create a keyed factory for the instance of the bundle that we can only resolve once the bundle is ready.
        services.AddKeyedSingleton(key, (sp, _) =>
        {
            var bundle = sp.GetRequiredKeyedService<AssetBundle<TContent>>(key);
            if (bundle.IsReady(out var content))
            {
                return content;
            }
            throw new InvalidOperationException("Cannot access content when bundle has not completed loading yet");
        });

        return services;
    }
}
