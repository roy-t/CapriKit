using CapriKit.AssetPipeline;
using CapriKit.IO;
using CapriKit.IO.Buffers;
using System.Buffers;

namespace CapriKit.Tests.AssetPipeline;

/// <summary>
/// Uppercases a text file
/// </summary>
internal sealed class DummyTranscoder : IAssetTranscoder<string, NoSettings<string>>
{
    public Guid Id => Guid.Parse("{B87F41E3-6C33-46E4-802A-3E1E82800E7A}");
    public int Version => 1;

    public async Task Encode(AssetId id, NoSettings<string> settings, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        var text = await fileSystem.ReadAllText(id.Path);
        writer.Write(text.ToUpperInvariant());
    }



    public string Decode(AssetId id, NoSettings<string> settings, ref SequenceReader<byte> reader)
    {
        return reader.ReadString();
    }

    public void HotSwap(string instance, string replacement)
    {
        throw new NotImplementedException();
    }

    public NoSettings<string> ReadSettings(ref SequenceReader<byte> reader)
    {
        return default;
    }

    public void WriteSettings(NoSettings<string> settings, IBufferWriter<byte> writer)
    {
        
    }
}
