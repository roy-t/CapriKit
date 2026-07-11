using CapriKit.IO;
using System.Buffers;

namespace CapriKit.AssetPipeline;

public record AssetId(string Key, FilePath Path);

public interface IAssetEncoder
{
    IReadOnlySet<string> SupportedExtensions { get; }
    Guid Id { get; }
    int Version { get; }

    Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer);
}

// TODO: images need settings and we could even say shaders do (the entry point) if we do not want to
// rely on AssetId.Key
public interface IAssetEncoder<TSettings> : IAssetEncoder
{
    Task Encode(AssetId id, TSettings settings, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer);
}

public interface IAssetDecoder<TAsset>
{
    Guid Id { get; }
    int Version { get; }

    // Synchronous by design: the envelope owns all file IO and hands the decoder an
    // in-memory payload. The reader's buffer is only valid for the duration of the call,
    // decoders must copy out anything they want to keep.
    TAsset Decode(AssetId id, ref SequenceReader<byte> reader);
    void HotSwap(TAsset instance, TAsset replacement);
}
