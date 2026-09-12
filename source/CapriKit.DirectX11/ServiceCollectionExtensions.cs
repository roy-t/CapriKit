using CapriKit.DirectX11.Debug;
using Microsoft.Extensions.DependencyInjection;

namespace CapriKit.DirectX11;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDirectX11(this IServiceCollection services)
    {
        return services.AddSingleton<Device>()
                       .AddSingleton<SwapChain>()
                       .AddSingleton<ImGuiController>();
    }
}
