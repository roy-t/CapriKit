using CapriKit.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CapriKit.AssetPipeline;

// For Microsoft.Extensions.DependencyInjection.Abstractions
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAssetPipeline(this IServiceCollection services, DirectoryPath assetDirectory, DirectoryPath outputDirectory)
    {
        return services.AddSingleton(sp =>
        {
            var logFactory = sp.GetRequiredService<ILoggerFactory>();
            var inputFileSystem = new FileSystem().ScopedToReadOnly(assetDirectory);
            var outputFileSystem = new FileSystem().ScopedTo(outputDirectory);
            return new AssetManager(logFactory, inputFileSystem, outputFileSystem);
        });
    }
}
