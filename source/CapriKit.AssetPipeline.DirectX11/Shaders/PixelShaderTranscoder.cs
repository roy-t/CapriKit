using CapriKit.DirectX11;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.IO;
using System.Buffers;

namespace CapriKit.AssetPipeline.DirectX11.Shaders;

public sealed class PixelShaderTranscoder(Device device)
    : NoSettingsTranscoder<IPixelShader>(Guid.Parse("{443FC1F6-A831-4CD8-95F7-791573DA2709}"), 1)
{
    public override async Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        var source = await fileSystem.ReadAllText(id.Path);
        var includePath = id.Path.Directory;
        var bytes = ShaderCompiler.CompilePixelShader(fileSystem, includePath, source, id.Key, id.ToString());
        ShaderTranscoder.WriteCommon(bytes.Common, writer);
    }

    public override IPixelShader Decode(AssetId id, ref SequenceReader<byte> reader)
    {
        var common = ShaderTranscoder.ReadCommon(ref reader);
        return ShaderCompiler.CreatePixelShader(new PixelShaderByteCode(common), device);
    }

    public override void HotSwap(IPixelShader instance, IPixelShader newParts)
    {
        instance.HotSwap(newParts);
    }
}
