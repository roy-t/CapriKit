using CapriKit.IO;
using System.Buffers;

namespace CapriKit.AssetPipeline;

public abstract class NoSettingsTranscoder<TAsset>(Guid id, int version) : IAssetTranscoder<TAsset, NoSettings<TAsset>>
{
    public Guid Id { get; } = id;
    public int Version { get; } = version;

    public abstract TAsset Decode(AssetId id, NoSettings<TAsset> settings, ref SequenceReader<byte> reader);
    public abstract Task Encode(AssetId id, NoSettings<TAsset> settings, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer);
    public abstract void HotSwap(TAsset instance, TAsset newParts);

    public NoSettings<TAsset> ReadSettings(ref SequenceReader<byte> reader)
    {
        return default;
    }

    public void WriteSettings(NoSettings<TAsset> settings, IBufferWriter<byte> writer)
    {
        // no-op
    }
}
