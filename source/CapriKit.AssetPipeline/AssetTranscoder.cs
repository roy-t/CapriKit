using CapriKit.IO;
using CapriKit.IO.Buffers;
using System.Buffers;
using System.IO.Pipelines;

namespace CapriKit.AssetPipeline;

internal sealed record Envelope<T>(T Asset, IReadOnlySet<FilePath> Dependencies);

// TODO: split in encoder and decoder and make the encoder as nice as the decoder
internal static class AssetTranscoder
{
    // Encoder id (16 bytes) + encoder version (4 bytes) + payload length (4 bytes)
    private const int HeaderSizeInBytes = 24;

    public static async Task Encode(AssetId id, IVirtualFileSystem fileSystem, IAssetEncoder encoder)
    {
        ThrowOnFileNotFound(id.Path, fileSystem);

        var outputPath = ToEncodedFilePath(id.Path);
        using var output = fileSystem.CreateReadWrite(outputPath);
        var writer = PipeWriter.Create(output);

        // Header
        writer.Write(encoder.Id);
        writer.Write(encoder.Version);

        // Payload
        var payload = new ArrayBufferWriter<byte>();
        var spy = fileSystem.SpyOn();
        await encoder.Encode(id, spy, payload);
        writer.Write(payload.WrittenCount);
        writer.Write(payload.WrittenSpan);

        // Asset dependencies, including the source file itself
        writer.Write(spy.OpenedFiles.Count);
        foreach (var dependency in spy.OpenedFiles)
        {
            writer.Write(dependency);
        }

        await writer.FlushAsync();
        await writer.CompleteAsync();
    }

    public static async Task<Envelope<T>> Decode<T>(AssetId id, IVirtualFileSystem fileSystem, IAssetDecoder<T> decoder)
    {
        var inputPath = ToEncodedFilePath(id.Path);
        ThrowOnFileNotFound(inputPath, fileSystem);

        using var input = fileSystem.OpenRead(inputPath);

        var payloadLength = await ReadHeader(input, decoder, inputPath);
        var asset = await ReadPayload(input, payloadLength, decoder, id);
        var dependencies = await ReadDependencies(input);

        return new Envelope<T>(asset, dependencies);
    }

    private static async Task<int> ReadHeader<T>(Stream input, IAssetDecoder<T> decoder, FilePath path)
    {
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

    private static FilePath ToEncodedFilePath(FilePath path)
    {
        return path + ".cka";
    }

    private static void ThrowOnFileNotFound(FilePath path, IVirtualFileSystem fileSystem)
    {
        if (!fileSystem.Exists(path))
        {
            throw new FileNotFoundException(null, path);
        }
    }
}
