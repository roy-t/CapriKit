using CapriKit.IO;
using CapriKit.IO.Buffers;
using System.Buffers;

using static CapriKit.AssetPipeline.AssetUtilities;

namespace CapriKit.AssetPipeline;

public interface IAssetDecoder
{
    Guid Id { get; }
    int Version { get; }

}

public interface IAssetDecoder<TAsset> : IAssetDecoder
{
    // Synchronous by design: the envelope owns all file IO and hands the decoder an
    // in-memory payload. The reader's buffer is only valid for the duration of the call,
    // decoders must copy out anything they want to keep.
    TAsset Decode(AssetId id, ref SequenceReader<byte> reader);
    void HotSwap(TAsset instance, TAsset replacement);
}

internal static class AssetDecoder
{
    public static async Task<Asset<T>> Decode<T>(AssetId id, IVirtualFileSystem fileSystem, IAssetDecoder<T> decoder)
    {
        var inputPath = ToEncodedFilePath(id.Path);
        ThrowOnFileNotFound(inputPath, fileSystem);

        using var input = fileSystem.OpenRead(inputPath);

        var payloadLength = await ReadHeader(input, decoder, inputPath);
        var asset = await ReadPayload(input, payloadLength, decoder, id);
        var dependencies = await ReadDependencies(input);

        return new Asset<T>(asset, dependencies);
    }

    private static async Task<int> ReadHeader<T>(Stream input, IAssetDecoder<T> decoder, FilePath path)
    {
        // Encoder id (16 bytes) + encoder version (4 bytes) + payload length (4 bytes)
        const int HeaderSizeInBytes = 24;
        var buffer = ArrayPool<byte>.Shared.Rent(HeaderSizeInBytes);
        try
        {
            await input.ReadExactlyAsync(buffer.AsMemory(0, HeaderSizeInBytes));
            var reader = SequenceReaders.Create(buffer, 0, HeaderSizeInBytes);
            var id = reader.ReadGuid();
            var version = reader.ReadInt32();
            var payloadLength = reader.ReadInt32();

            if (id != decoder.Id || version != decoder.Version)
            {
                throw new InvalidDataException(
                    $"Cannot decode {path}, it was encoded by {id} v{version} but the decoder is {decoder.Id} v{decoder.Version}");
            }

            return payloadLength;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task<T> ReadPayload<T>(Stream input, int payloadLength, IAssetDecoder<T> decoder, AssetId id)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(payloadLength);
        try
        {
            await input.ReadExactlyAsync(buffer.AsMemory(0, payloadLength));
            var reader = SequenceReaders.Create(buffer, 0, payloadLength);
            return decoder.Decode(id, ref reader);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task<HashSet<FilePath>> ReadDependencies(Stream input)
    {
        var length = (int)(input.Length - input.Position);
        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            await input.ReadExactlyAsync(buffer.AsMemory(0, length));
            var reader = SequenceReaders.Create(buffer, 0, length);

            var count = reader.ReadInt32();
            var dependencies = new HashSet<FilePath>(count);
            for (var i = 0; i < count; i++)
            {
                dependencies.Add(reader.ReadString());
            }
            return dependencies;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
