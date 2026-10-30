using CapriKit.AssetPipeline.DirectX11.Models;
using CapriKit.AssetPipeline.DirectX11.Shaders;
using CapriKit.AssetPipeline.DirectX11.Textures;
using CapriKit.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CapriKit.AssetPipeline.DirectX11;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDirectX11ShaderTranscoders(this IServiceCollection collection)
    {
        collection.TryAddEnumerable(ServiceDescriptor.Singleton<IAssetTranscoder, VertexShaderTranscoder>());
        collection.TryAddEnumerable(ServiceDescriptor.Singleton<IAssetTranscoder, PixelShaderTranscoder>());
        return collection;
    }

    public static IServiceCollection AddDirectX11TextureTranscoders(this IServiceCollection collection)
    {
        collection.TryAddEnumerable(ServiceDescriptor.Singleton<IAssetTranscoder, Texture2DTranscoder>());
        return collection;
    }

    public static IServiceCollection AddDirectX11ModelTranscoders(this IServiceCollection collection, DirectoryPath? pathToBlenderDirectory = null)
    {
        if (string.IsNullOrEmpty(pathToBlenderDirectory))
        {
            collection.AddSingleton(BlenderConfiguration.AutoDetect());
        }
        else
        {
            var config = new BlenderConfiguration(pathToBlenderDirectory);
            if (!File.Exists(config.PathToBlenderExecutable))
            {
                throw new FileNotFoundException($"Parameter {nameof(pathToBlenderDirectory)} is set to '{pathToBlenderDirectory}' but that folder does not contain the blender executable", config.PathToBlenderExecutable);
            }
            collection.AddSingleton(new BlenderConfiguration(pathToBlenderDirectory));
        }

        collection.TryAddEnumerable(ServiceDescriptor.Singleton<IAssetTranscoder, CKTModelTranscoder>());
        return collection;
    }
}
