using CapriKit.DirectX11;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.IO;
using System.Buffers;

namespace CapriKit.AssetPipeline.DirectX11.Shaders;

public sealed class VertexShaderTranscoder(Device device) : IAssetTranscoder<IVertexShader, NoSettings<IVertexShader>>
{
    public Guid Id => Guid.Parse("{CA3CB37D-9880-4B61-AB09-EBC17E7533E6}");
    public int Version => 1;

    public async Task Encode(AssetId id, NoSettings<IVertexShader> _, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        var source = await fileSystem.ReadAllText(id.Path);
        var includePath = id.Path.Directory;
        var bytes = ShaderCompiler.CompileVertexShader(fileSystem, includePath, source, id.Key, id.ToString());
        ShaderTranscoder.WriteCommon(bytes.Common, writer);
    }

    public IVertexShader Decode(AssetId id, NoSettings<IVertexShader> _, ref SequenceReader<byte> reader)
    {
        var common = ShaderTranscoder.ReadCommon(ref reader);
        return ShaderCompiler.CreateVertexShader(new VertexShaderByteCode(common), device);
    }

    public void HotSwap(IVertexShader instance, IVertexShader replacement)
    {
        instance.HotSwap(replacement);
    }

    public void WriteSettings(NoSettings<IVertexShader> settings, IBufferWriter<byte> writer)
    {
        // no-op
    }

    public NoSettings<IVertexShader> ReadSettings(ref SequenceReader<byte> reader)
    {
        return default;
    }
}
