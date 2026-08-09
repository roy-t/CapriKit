using CapriKit.IO;
using System.Buffers;

namespace CapriKit.AssetPipeline.v2;


public interface IAssetTranscoder
{
    Guid Id { get; }
    int Version { get; }
}

// TODO: improve documentation
public interface IAssetTranscoder<TAsset, TSettings> : IAssetTranscoder
    where TAsset : class
{
    // Asynchronous since we expect the encoder to read external files
    public Task Encode(AssetId id, TSettings settings, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer);

    // Synchronous by design: the envelope owns all file IO and hands the decoder an
    // in-memory payload. The reader's buffer is only valid for the duration of the call,
    // decoders must copy out anything they want to keep.
    public TAsset Decode(AssetId id, TSettings settings, ref SequenceReader<byte> reader);

    public void WriteSettings(TSettings settings, IBufferWriter<byte> writer);

    public TSettings ReadSettings(ref SequenceReader<byte> reader);

    /// <summary>
    /// Moves the contents of <paramref name="newParts"/> into <paramref name="instance"/>.
    /// <paramref name="instance"/> keeps its identity and stays the live object that callers
    /// already hold references to. The transcoder is responsible for cleaning-up any
    /// orphaned resources. After calling this method <paramref name="newParts"/> must no longer
    /// be used or referenced.
    /// </summary>
    void HotSwap(TAsset instance, TAsset newParts);
}
