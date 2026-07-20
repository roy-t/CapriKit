using CapriKit.AssetPipeline;
using CapriKit.IO;
using CapriKit.IO.Streams;
using System.Buffers;

namespace CapriKit.Tests.AssetPipeline;

internal readonly record struct RepeatSettings(int Count) : IAssetSettings<string>
{
    public void Write(IBufferWriter<byte> writer)
    {
        writer.Write(Count);
    }
}

/// <summary>
/// Repeats the text in a text file <see cref="RepeatSettings.Count"/> times
/// </summary>
internal sealed class RepeatTranscoder : IAssetTranscoder<string, RepeatSettings>
{
    public Guid Id => Guid.Parse("{0F1F51E7-2F2B-4E3B-9C93-15BBB61C1AF4}");
    public int Version => 1;

    public async Task Encode(AssetId id, RepeatSettings settings, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        var text = await fileSystem.ReadAllText(id.Path);
        writer.Write(string.Concat(Enumerable.Repeat(text, settings.Count)));
    }

    public string Decode(AssetId id, RepeatSettings settings, ref SequenceReader<byte> reader)
    {
        return reader.ReadString();
    }

    public void HotSwap(string instance, string replacement)
    {
        throw new NotImplementedException();
    }

    public RepeatSettings ReadSettings(ref SequenceReader<byte> reader)
    {
        return new RepeatSettings(reader.ReadInt32());
    }

    public void WriteSettings(RepeatSettings settings, IBufferWriter<byte> writer)
    {
        writer.Write(settings.Count);
    }
}
