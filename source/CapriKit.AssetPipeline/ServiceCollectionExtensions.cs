using CapriKit.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CapriKit.AssetPipeline;

// For Microsoft.Extensions.DependencyInjection.Abstractions
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAssetPipeline(this IServiceCollection services, DirectoryPath assetDirectory)
    {
        return services.AddSingleton(sp =>
        {
            var logFactory = sp.GetRequiredService<ILoggerFactory>();
            var fileSystem = new FileSystem().ScopedTo(assetDirectory);
            return new AssetManager(logFactory, fileSystem);
        });
    }
}
