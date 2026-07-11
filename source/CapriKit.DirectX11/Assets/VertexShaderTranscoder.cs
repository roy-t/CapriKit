using CapriKit.AssetPipeline;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.IO;
using CapriKit.IO.Buffers;
using System.Buffers;

namespace CapriKit.DirectX11.Assets;

public sealed class VertexShaderTranscoder(Device device) : IAssetEncoder, IAssetDecoder<IVertexShader>
{
    public IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>([".hlsl"]);
    public Guid Id => Guid.Parse("{CA3CB37D-9880-4B61-AB09-EBC17E7533E6}");
    public int Version => 1;

    public async Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        var source = await fileSystem.ReadAllText(id.Path);
        var includePath = id.Path.Directory;
        var bytes = ShaderCompiler.CompileVertexShader(fileSystem, includePath, source, id.Key, id.ToString());

        writer.Write(bytes.EntryPoint);
        writer.Write(bytes.Name);
        writer.Write(bytes.Bytes.Length);
        writer.Write(bytes.Bytes);
    }

    public IVertexShader Decode(AssetId id, ref SequenceReader<byte> reader)
    {
        var entryPoint = reader.ReadString();
        var name = reader.ReadString();
        var length = reader.ReadInt32();
        var bytes = reader.ReadBytes(length);

        var byteCode = new VertexShaderByteCode(bytes, entryPoint, name);
        return ShaderCompiler.CreateVertexShader(byteCode, device);
    }

    public void HotSwap(IVertexShader instance, IVertexShader replacement)
    {
        var original = instance.ID3D11VertexShader;
        instance.ID3D11VertexShader = replacement.ID3D11VertexShader;
        original.Dispose();
    }
}
