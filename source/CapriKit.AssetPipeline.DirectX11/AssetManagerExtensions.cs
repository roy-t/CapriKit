using CapriKit.DirectX11.Resources.Shaders;

namespace CapriKit.AssetPipeline.DirectX11;

public static class AssetManagerExtensions
{
    public static void EncodeVertexShader(this AssetManager assetManager, AssetId id)
    {
        assetManager.Encode<IVertexShader, NoSettings<IVertexShader>>(id, default);
    }

    public static IVertexShader DecodeVertexShader(this AssetManager assetManager, AssetId id)
    {
        return assetManager.Decode<IVertexShader, NoSettings<IVertexShader>>(id, default);
    }
}
