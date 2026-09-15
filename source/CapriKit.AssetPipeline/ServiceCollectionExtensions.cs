using CapriKit.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CapriKit.AssetPipeline;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAssetPipeline(this IServiceCollection services, DirectoryPath assetDirectory, DirectoryPath outputDirectory)
    {
        services.TryAddSingleton<AssetManager>(sp =>
        {
            var logFactory = sp.GetRequiredService<ILoggerFactory>();
            var transcoders = sp.GetServices<IAssetTranscoder>();
            var inputFileSystem = new FileSystem().ScopedToReadOnly(assetDirectory);
            var outputFileSystem = new FileSystem().ScopedTo(outputDirectory);
            return new AssetManager(logFactory, inputFileSystem, outputFileSystem, transcoders);
        });
        return services;
    }
}
