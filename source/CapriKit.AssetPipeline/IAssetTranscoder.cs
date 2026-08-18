using CapriKit.IO;
using System.Buffers;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Interface for classes that build assets (such as texture, models and sound effects) and load them
/// when the program needs them. Implementers must take care that most access needs to be thread-safe.
/// </summary>
public interface IAssetTranscoder
{
    Guid Id { get; }
    int Version { get; }
}

/// <inheritdoc cref="IAssetTranscoder"/>
public interface IAssetTranscoder<TAsset, TSettings> : IAssetTranscoder
    where TAsset : class
{
    // Asynchronous since we expect the encoder to read external files

    /// <summary>
    /// Loads the raw asset data from the file system and build/encodes it into a format optimized for loading.
    /// Threading: thread-safe, encoding happens asynchronously and can happen on any thread.
    /// </summary>
    public Task Encode(AssetId id, TSettings settings, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer);

    /// <summary>
    /// Decodes the file created the <see cref="Encode"/> into an object by reading bytes from the given reader.
    /// Threading: thread-safe, Though decoding itself is synchronous, it can happen as part of
    /// a multi-threaded or async operation.
    /// </summary>
    public TAsset Decode(AssetId id, TSettings settings, ref SequenceReader<byte> reader);

    /// <summary>
    /// Encodes the settings required to encode/decode the asset into the stream.
    /// Threading: thread-safe, though this is by itself a synchronous action,
    /// it can happen as part of a multi-threaded or async operation.
    /// </summary>
    public void WriteSettings(TSettings settings, IBufferWriter<byte> writer);


    /// <summary>
    /// Decodes the settings required to encode/decode the asset into the stream.
    /// Threading: Though this is by itself synchronous,
    /// it can happen as part of a multi-threaded or async operation.
    /// </summary>
    public TSettings ReadSettings(ref SequenceReader<byte> reader);

    /// <summary>
    /// Moves the contents of <paramref name="newParts"/> into <paramref name="instance"/>.
    /// <paramref name="instance"/> keeps its identity and stays the live object that callers
    /// already hold references to. The transcoder is responsible for cleaning-up any
    /// orphaned resources. After calling this method <paramref name="newParts"/> must no longer
    /// be used or referenced.
    /// Threading: must be called by the main thread.
    /// </summary>
    void HotSwap(TAsset instance, TAsset newParts);
}
