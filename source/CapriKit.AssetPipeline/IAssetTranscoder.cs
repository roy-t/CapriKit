using CapriKit.IO;
using System.Buffers;

namespace CapriKit.AssetPipeline;

public readonly struct NoSettings<TAsset> : IAssetSettings<TAsset>
{
    public void Write(IBufferWriter<byte> writer)
    {
        // no-op
    }
}

public interface IAssetSettings<TAsset>
{
    void Write(IBufferWriter<byte> writer);
}

public interface IAssetTranscoder
{
    Guid Id { get; }
    int Version { get; }
}

public interface IAssetTranscoder<TAsset, TSettings> : IAssetTranscoder
    where TSettings : IAssetSettings<TAsset>
{
    // Asynchronous since we expect the encoder to read external files
    Task Encode(AssetId id, TSettings settings, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer);

    // Synchronous by design: the envelope owns all file IO and hands the decoder an
    // in-memory payload. The reader's buffer is only valid for the duration of the call,
    // decoders must copy out anything they want to keep.
    TAsset Decode(AssetId id, TSettings settings, ref SequenceReader<byte> reader);

    void HotSwap(TAsset instance, TAsset replacement);
}
