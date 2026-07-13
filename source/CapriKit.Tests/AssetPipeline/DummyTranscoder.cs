using CapriKit.AssetPipeline;
using CapriKit.IO;
using CapriKit.IO.Buffers;
using System.Buffers;

namespace CapriKit.Tests.AssetPipeline;

/// <summary>
/// Uppercases a text file
/// </summary>
internal sealed class DummyTranscoder : IAssetEncoder, IAssetDecoder<string>
{
    public IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>([".txt"]);
    public Guid Id => Guid.Parse("{B87F41E3-6C33-46E4-802A-3E1E82800E7A}");
    public int Version => 1;

    public async Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        var text = await fileSystem.ReadAllText(id.Path);
        writer.Write(text.ToUpperInvariant());
    }

    public string Decode(AssetId id, ref SequenceReader<byte> reader)
    {
        return reader.ReadString();
    }

    public void HotSwap(string instance, string replacement)
    {
        throw new NotImplementedException();
    }
}
