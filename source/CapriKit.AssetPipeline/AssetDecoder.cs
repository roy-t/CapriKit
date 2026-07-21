using CapriKit.IO;
using CapriKit.IO.Streams;
using System.Buffers;
using static CapriKit.AssetPipeline.AssetUtilities;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Decodes the generic asset envelope and, using a specialized IAssetTranscoder, the asset itself
/// </summary>
internal static class AssetDecoder
{
    public static async Task<Asset<TAsset>> Decode<TAsset>(AssetId id,
        IAssetTranscoder<TAsset> decoder, IVirtualFileSystem fileSystem)
    {
        var inputPath = ToEncodedFilePath(id);
        ThrowOnFileNotFound(inputPath, fileSystem);

        using var input = fileSystem.OpenRead(inputPath);
        var length = checked((int)input.Length);

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            await input.ReadExactlyAsync(buffer.AsMemory(0, length));
            var reader = SequenceReaders.Create(buffer, 0, length);

            ReadHeader(ref reader, decoder, inputPath);
            var settings = ReadSettings(ref reader, decoder);
            var asset = ReadPayload(ref reader, id, settings, decoder);
            var dependencies = ReadDependencies(ref reader);

            return new Asset<TAsset>(id, asset, dependencies);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void ReadHeader(ref SequenceReader<byte> reader, IAssetTranscoder decoder, FilePath path)
    {
        var id = reader.ReadGuid();
        var version = reader.ReadInt32();

        if (id != decoder.Id || version != decoder.Version)
        {
            throw new InvalidDataException(
                $"Cannot decode {path}, it was encoded by {id} v{version} but the decoder is {decoder.Id} v{decoder.Version}");
        }
    }

    private static IAssetSettings<TAsset> ReadSettings<TAsset>(ref SequenceReader<byte> reader,
        IAssetTranscoder<TAsset> decoder)
    {
        var settingsLength = reader.ReadInt32();
        var settingsReader = reader.SliceUnread(settingsLength);
        return decoder.ReadSettings(ref settingsReader);
    }

    private static TAsset ReadPayload<TAsset>(ref SequenceReader<byte> reader,
        AssetId id, IAssetSettings<TAsset> settings, IAssetTranscoder<TAsset> decoder)
    {
        var payloadLength = reader.ReadInt32();
        var payloadReader = reader.SliceUnread(payloadLength);
        return decoder.Decode(id, settings, ref payloadReader);
    }

    private static List<Dependency> ReadDependencies(ref SequenceReader<byte> reader)
    {
        var count = reader.ReadInt32();
        var dependencies = new List<Dependency>(count);
        for (var i = 0; i < count; i++)
        {
            var lastWriteTicks = reader.ReadInt64();
            var lastWrite = new DateTime(lastWriteTicks);
            var filePathString = reader.ReadString();
            var filePath = new FilePath(filePathString);
            dependencies.Add(new Dependency(filePath, lastWrite));
        }
        return dependencies;
    }
}
