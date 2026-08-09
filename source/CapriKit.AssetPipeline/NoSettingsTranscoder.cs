using CapriKit.IO;
using System.Buffers;

namespace CapriKit.AssetPipeline;

public readonly struct NoSettings;

/// <summary>
/// Abstract transcoder that removes some of the boilerplate code for implementations that do not have any settings.
/// </summary>
public abstract class NoSettingsTranscoder<TAsset>(Guid id, int version) : IAssetTranscoder<TAsset, NoSettings>
    where TAsset : class
{
    public Guid Id { get; } = id;
    public int Version { get; } = version;

    public abstract Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer);

    public abstract TAsset Decode(AssetId id, ref SequenceReader<byte> reader);

    public abstract void HotSwap(TAsset instance, TAsset newParts);

    TAsset IAssetTranscoder<TAsset, NoSettings>.Decode(AssetId id, NoSettings settings, ref SequenceReader<byte> reader)
        => Decode(id, ref reader);

    Task IAssetTranscoder<TAsset, NoSettings>.Encode(AssetId id, NoSettings settings, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
        => Encode(id, fileSystem, writer);

    NoSettings IAssetTranscoder<TAsset, NoSettings>.ReadSettings(ref SequenceReader<byte> reader) => default;

    void IAssetTranscoder<TAsset, NoSettings>.WriteSettings(NoSettings settings, IBufferWriter<byte> writer) { }
}
