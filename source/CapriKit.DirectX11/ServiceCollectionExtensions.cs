using CapriKit.DirectX11.Debug;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CapriKit.DirectX11;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDirectX11(this IServiceCollection services)
    {
        services.TryAddSingleton<Device>();
        services.TryAddSingleton<SwapChain>();
        services.TryAddSingleton<ImGuiController>();

        return services;
    }
}
