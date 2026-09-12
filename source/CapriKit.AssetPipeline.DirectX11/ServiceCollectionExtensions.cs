using CapriKit.AssetPipeline.DirectX11.Shaders;
using Microsoft.Extensions.DependencyInjection;

namespace CapriKit.AssetPipeline.DirectX11;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDirectX11AssetTranscoders(this IServiceCollection services)
    {
        return services.AddSingleton<VertexShaderTranscoder>()
                       .AddSingleton<PixelShaderTranscoder>();

    }
}
