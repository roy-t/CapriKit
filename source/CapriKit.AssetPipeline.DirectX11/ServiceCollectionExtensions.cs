using CapriKit.AssetPipeline.DirectX11.Shaders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CapriKit.AssetPipeline.DirectX11;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDirectX11AssetTranscoders(this IServiceCollection collection)
    {
        collection.TryAddEnumerable(ServiceDescriptor.Singleton<IAssetTranscoder, VertexShaderTranscoder>());
        collection.TryAddEnumerable(ServiceDescriptor.Singleton<IAssetTranscoder, PixelShaderTranscoder>());
        return collection;
    }
}
