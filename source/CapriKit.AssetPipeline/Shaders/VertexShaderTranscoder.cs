using CapriKit.DirectX11;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.IO;
using System.Buffers;

namespace CapriKit.AssetPipeline.Shaders;

// TODO: I am not happy that this code is here, but I also do not want to put it in CapriKit.DirectX11
// maybe I can make a CapriKit.AssetPipeline.DirectX11 project that has all the DirectX11 specific bits?
// (shaders, textures)
internal sealed class VertexShaderTranscoder(Device device) : IAssetEncoder, IAssetDecoder<IVertexShader>
{
    public IReadOnlySet<string> SupportedExtensions => ShaderTranscoder.SupportedExtensions;
    public Guid Id => Guid.Parse("{CA3CB37D-9880-4B61-AB09-EBC17E7533E6}");
    public int Version => 1;

    public async Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        var source = await fileSystem.ReadAllText(id.Path);
        var includePath = id.Path.Directory;
        var bytes = ShaderCompiler.CompileVertexShader(fileSystem, includePath, source, id.Key, id.ToString());
        ShaderTranscoder.WriteCommon(bytes.Common, writer);
    }

    public IVertexShader Decode(AssetId id, ref SequenceReader<byte> reader)
    {
        var common = ShaderTranscoder.ReadCommon(ref reader);
        return ShaderCompiler.CreateVertexShader(new VertexShaderByteCode(common), device);
    }

    public void HotSwap(IVertexShader instance, IVertexShader replacement)
    {
        instance.HotSwap(replacement);
    }
}
