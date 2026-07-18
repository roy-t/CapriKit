using CapriKit.IO;
using CapriKit.IO.Buffers;
using System.Buffers;

using static CapriKit.AssetPipeline.AssetUtilities;

namespace CapriKit.AssetPipeline;

internal static class AssetDecoder
{
    public static async Task<Asset<TAsset>> Decode<TAsset, TSettings>(AssetId id, TSettings settings,
        IAssetTranscoder<TAsset, TSettings> decoder, IVirtualFileSystem fileSystem)
        where TSettings : IAssetSettings<TAsset>
    {
        var inputPath = ToEncodedFilePath(id);
        ThrowOnFileNotFound(inputPath, fileSystem);

        using var input = fileSystem.OpenRead(inputPath);

        var payloadLength = await ReadHeader(input, settings, decoder, inputPath);

        // TODO: read settings so we can do a byte-for-byte check they have stayed the same

        var asset = await ReadPayload(input, payloadLength, id, settings, decoder);
        var dependencies = await ReadDependencies(input);

        return new Asset<TAsset>(asset, dependencies);
    }

    private static async Task<int> ReadHeader<TAsset, TSettings>(Stream input,
        TSettings settings, IAssetTranscoder<TAsset, TSettings> decoder, FilePath path)
        where TSettings : IAssetSettings<TAsset>
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

    private static async Task<TAsset> ReadPayload<TAsset, TSettings>(Stream input, int payloadLength,
        AssetId id, TSettings setting, IAssetTranscoder<TAsset, TSettings> decoder)
        where TSettings : IAssetSettings<TAsset>
    {
        var buffer = ArrayPool<byte>.Shared.Rent(payloadLength);
        try
        {
            await input.ReadExactlyAsync(buffer.AsMemory(0, payloadLength));
            var reader = SequenceReaders.Create(buffer, 0, payloadLength);
            return decoder.Decode(id, setting, ref reader);
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
