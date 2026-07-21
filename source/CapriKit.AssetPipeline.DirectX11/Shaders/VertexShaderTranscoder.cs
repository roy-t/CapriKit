using CapriKit.DirectX11;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.IO;
using System.Buffers;

namespace CapriKit.AssetPipeline.DirectX11.Shaders;

public sealed class VertexShaderTranscoder(Device device)
    : NoSettingsTranscoder<IVertexShader>(Guid.Parse("{CA3CB37D-9880-4B61-AB09-EBC17E7533E6}"), 1)
{
    public async override Task Encode(AssetId id, NoSettings<IVertexShader> _, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        var source = await fileSystem.ReadAllText(id.Path);
        var includePath = id.Path.Directory;
        var bytes = ShaderCompiler.CompileVertexShader(fileSystem, includePath, source, id.Key, id.ToString());
        ShaderTranscoder.WriteCommon(bytes.Common, writer);
    }

    public override IVertexShader Decode(AssetId id, NoSettings<IVertexShader> _, ref SequenceReader<byte> reader)
    {
        var common = ShaderTranscoder.ReadCommon(ref reader);
        return ShaderCompiler.CreateVertexShader(new VertexShaderByteCode(common), device);
    }

    public override void HotSwap(IVertexShader instance, IVertexShader newParts)
    {
        instance.HotSwap(newParts);
    }
}
