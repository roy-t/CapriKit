using CapriKit.IO;
using CapriKit.IO.Streams;
using System.Buffers;
using static CapriKit.AssetPipeline.AssetUtilities;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Decodes the generic asset envelope and, using a specialized IAssetTranscoder, the asset itself
/// Threading: thread-safe
/// </summary>
internal static class AssetDecoder
{
    public static async Task<Asset<TAsset, TSettings>> Decode<TAsset, TSettings>(AssetId id, IAssetTranscoder<TAsset, TSettings> decoder, IReadOnlyVirtualFileSystem fileSystem, Stream? inputStreamOverride = default)
        where TAsset : class
    {
        var inputPath = ToEncodedFilePath(id);
        if (!fileSystem.Exists(inputPath))
        {
            throw new FileNotFoundException($"Could not find file: {inputPath} to load asset: {id}", id.Path);
        }

        using var input = inputStreamOverride ?? fileSystem.OpenRead(inputPath);
        var length = checked((int)input.Length);
        var buffer = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            await input.ReadExactlyAsync(buffer.AsMemory(0, length));
            var reader = SequenceReaders.Create(buffer, 0, length);

            var (encoderId, encoderVersion) = ReadHeader(ref reader);
            ThrowOnDecoderMismatch(id, inputPath, encoderId, encoderVersion, decoder);

            var settings = ReadSettings(ref reader, decoder);
            var asset = ReadPayload(ref reader, id, decoder, settings);
            var dependencies = ReadDependencies(ref reader);

            var buildMetaData = new AssetBuildMetaData<TSettings>(encoderId, encoderVersion, settings, dependencies);
            return new Asset<TAsset, TSettings>(id, asset, buildMetaData);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static async Task<AssetBuildMetaData<TSettings>?> TryDecodeBuildMetaData<TAsset, TSettings>(AssetId id, IAssetTranscoder<TAsset, TSettings> decoder, IReadOnlyVirtualFileSystem fileSystem)
        where TAsset : class
    {
        try
        {
            var inputPath = ToEncodedFilePath(id);
            if (!fileSystem.Exists(inputPath))
            {
                throw new FileNotFoundException($"Could not find file: {inputPath} to load asset: {id}", id.Path);
            }

            using var input = fileSystem.OpenRead(inputPath);
            var length = checked((int)input.Length);
            var buffer = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                await input.ReadExactlyAsync(buffer.AsMemory(0, length));
                var reader = SequenceReaders.Create(buffer, 0, length);

                var (encoderId, encoderVersion) = ReadHeader(ref reader);
                var settings = ReadSettings(ref reader, decoder);
                SkipPayload(ref reader);
                var dependencies = ReadDependencies(ref reader);
                return new AssetBuildMetaData<TSettings>(encoderId, encoderVersion, settings, dependencies);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch
        {
            return null;
        }
    }

    private static (Guid id, int version) ReadHeader(ref SequenceReader<byte> reader)
    {
        var id = reader.ReadGuid();
        var version = reader.ReadInt32();
        return (id, version);
    }

    private static TSettings ReadSettings<TAsset, TSettings>(ref SequenceReader<byte> reader, IAssetTranscoder<TAsset, TSettings> decoder)
        where TAsset : class
    {
        var settingsLength = reader.ReadInt32();
        var settingsReader = reader.SliceUnread(settingsLength);
        return decoder.ReadSettings(ref settingsReader);
    }

    private static TAsset ReadPayload<TAsset, TSettings>(ref SequenceReader<byte> reader, AssetId id, IAssetTranscoder<TAsset, TSettings> decoder, TSettings settings)
        where TAsset : class
    {
        var payloadLength = reader.ReadInt32();
        var payloadReader = reader.SliceUnread(payloadLength);
        return decoder.Decode(id, settings, ref payloadReader);
    }

    private static void SkipPayload(ref SequenceReader<byte> reader)
    {
        var payloadLength = reader.ReadInt32();
        reader.Advance(payloadLength);
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

    private static void ThrowOnDecoderMismatch(AssetId id, FilePath file, Guid fileId, int fileVersion, IAssetTranscoder decoder)
    {
        if (fileId != decoder.Id || fileVersion != decoder.Version)
        {
            throw new InvalidDataException(
                $"Decoder mismatch. Asset: {id} found in file: {file}, was encoded using {fileId}:v{fileVersion} but the current decoder is {decoder.Id}:v{decoder.Version}.");
        }
    }
}
