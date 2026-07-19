using CapriKit.IO;
using CapriKit.IO.Buffers;
using System.Buffers;
using System.Buffers.Binary;
using static CapriKit.AssetPipeline.AssetUtilities;

namespace CapriKit.AssetPipeline;

internal static class AssetDecoder
{
    public static async Task<Asset<TAsset>> Decode<TAsset>(AssetId id,
        IAssetTranscoder<TAsset> decoder, IVirtualFileSystem fileSystem)
    {
        var inputPath = ToEncodedFilePath(id);
        ThrowOnFileNotFound(inputPath, fileSystem);

        using var input = fileSystem.OpenRead(inputPath);

        await ReadHeader(input, decoder, inputPath);
        var settings = await ReadSettings(input, decoder);
        var asset = await ReadPayload(input, id, settings, decoder);
        var dependencies = await ReadDependencies(input);

        return new Asset<TAsset>(asset, dependencies);
    }

    private static async Task ReadHeader(Stream input, IAssetTranscoder decoder, FilePath path)
    {
        // Encoder id (16 bytes) + encoder version (4 bytes)
        const int HeaderSizeInBytes = 20;
        var buffer = ArrayPool<byte>.Shared.Rent(HeaderSizeInBytes);
        try
        {
            await input.ReadExactlyAsync(buffer.AsMemory(0, HeaderSizeInBytes));
            var reader = SequenceReaders.Create(buffer, 0, HeaderSizeInBytes);
            var id = reader.ReadGuid();
            var version = reader.ReadInt32();

            if (id != decoder.Id || version != decoder.Version)
            {
                throw new InvalidDataException(
                    $"Cannot decode {path}, it was encoded by {id} v{version} but the decoder is {decoder.Id} v{decoder.Version}");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task<IAssetSettings<TAsset>> ReadSettings<TAsset>(Stream input,
        IAssetTranscoder<TAsset> decoder)
    {
        var settingsLength = await ReadInt32(input);
        var buffer = ArrayPool<byte>.Shared.Rent(settingsLength);
        try
        {
            await input.ReadExactlyAsync(buffer.AsMemory(0, settingsLength));
            var reader = SequenceReaders.Create(buffer, 0, settingsLength);
            return decoder.ReadSettings(ref reader);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task<TAsset> ReadPayload<TAsset>(Stream input,
        AssetId id, IAssetSettings<TAsset> settings, IAssetTranscoder<TAsset> decoder)
    {
        var payloadLength = await ReadInt32(input);
        var buffer = ArrayPool<byte>.Shared.Rent(payloadLength);
        try
        {
            await input.ReadExactlyAsync(buffer.AsMemory(0, payloadLength));
            var reader = SequenceReaders.Create(buffer, 0, payloadLength);
            return decoder.Decode(id, settings, ref reader);
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

    // TODO: move to an extension method in CapriKit.IO
    private static async Task<int> ReadInt32(Stream input)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(int));
        try
        {
            await input.ReadExactlyAsync(buffer.AsMemory(0, sizeof(int)));
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
