using CapriKit.AssetPipeline.DirectX11.Shaders;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Resources.Shaders;

namespace CapriKit.AssetPipeline.DirectX11;

public static class AssetManagerExtensions
{
    public static Task<IVertexShader> LoadVertexShader(this AssetManager assetManager, Device device, AssetId id)
    {
        var transcoder = new VertexShaderTranscoder(device);
        return assetManager.Load(id, transcoder, default);
    }
}
