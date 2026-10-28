using CapriKit.AssetPipeline;
using Microsoft.Extensions.DependencyInjection;

namespace CapriKit.Tests.Tool.Tests.Framework;

internal static class TestRegistration
{
    public static IServiceCollection AddTest<TTest>(this IServiceCollection services)
        where TTest : class, ITestScreen
    {
        return services.AddSingleton<ITestScreen, TTest>();
    }

    public static IServiceCollection AddTest<TTest, TContent>(this IServiceCollection services, Func<AssetManager, AssetBundle<TContent>> create)
        where TTest : class, ITestScreen
        where TContent : class
    {
        return services.AddBundle(typeof(TTest).Name, create)
                       .AddSingleton<ITestScreen, TTest>();
    }

    /// <summary>
    /// Adds a factory that lazily creates an asset bundle. Key must be set to the class name of the class using this specific bundle so that it
    /// can be injected using `[FromKeyedServices(key)]...`
    /// </summary>
    public static IServiceCollection AddBundle<TContent>(this IServiceCollection services, string key, Func<AssetManager, AssetBundle<TContent>> create)
        where TContent : class
    {
        // Create a generic bundle factory that uses the asset bundle registered by key so the loading screen can use it
        // to check if all assets in the bundle are ready.
        services.AddSingleton<AssetBundle>(sp => sp.GetRequiredKeyedService<AssetBundle<TContent>>(key));

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
            throw new InvalidOperationException("Cannot access bundle content when bundle is not ready!");
        });

        return services;
    }
}
