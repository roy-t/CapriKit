using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.IO.Buffers;
using System.Buffers;
using System.Collections.Frozen;

namespace CapriKit.AssetPipeline.DirectX11.Shaders;

internal static class ShaderTranscoder
{
    public static IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>([".hlsl"]).ToFrozenSet();

    public static void WriteCommon(ShaderByteCode shader, IBufferWriter<byte> writer)
    {
        writer.Write(shader.EntryPoint);
        writer.Write(shader.Name);
        writer.Write(shader.Bytes.Length);
        writer.Write(shader.Bytes);
    }

    public static ShaderByteCode ReadCommon(ref SequenceReader<byte> reader)
    {
        var entryPoint = reader.ReadString();
        var name = reader.ReadString();
        var length = reader.ReadInt32();
        var bytes = reader.ReadBytes(length);

        return new ShaderByteCode(bytes, entryPoint, name);
    }
}
