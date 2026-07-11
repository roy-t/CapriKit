using CapriKit.IO;
using CapriKit.IO.Buffers;
using System.Buffers;
using System.IO.Pipelines;

namespace CapriKit.AssetPipeline;

internal sealed record Envelope<T>(T Asset, IReadOnlySet<FilePath> Dependencies);

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

        // Header
        var header = new byte[HeaderSizeInBytes];
        await input.ReadExactlyAsync(header);
        var payloadLength = ReadHeader(header, decoder, inputPath);

        // Note: rented buffers are AT LEAST the requested length
        var remainingLength = (int)(input.Length - input.Position);
        var buffer = ArrayPool<byte>.Shared.Rent(remainingLength);
        try
        {
            await input.ReadExactlyAsync(buffer.AsMemory(0, remainingLength));

            // Payload
            var payloadBuffer = buffer.AsMemory(0, payloadLength);
            var payloadReader = new SequenceReader<byte>(new ReadOnlySequence<byte>(payloadBuffer));
            var asset = decoder.Decode(id, ref payloadReader);

            // Dependencies
            var dependencyBuffer = buffer.AsMemory(payloadLength, remainingLength - payloadLength);
            var dependencyReader = new SequenceReader<byte>(new ReadOnlySequence<byte>(dependencyBuffer));
            var dependencies = ReadDependencies(ref dependencyReader);

            return new Envelope<T>(asset, dependencies);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static int ReadHeader<T>(byte[] header, IAssetDecoder<T> decoder, FilePath path)
    {
        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(header));
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

    private static HashSet<FilePath> ReadDependencies(ref SequenceReader<byte> reader)
    {
        var count = reader.ReadInt32();
        var dependencies = new HashSet<FilePath>(count);
        for (var i = 0; i < count; i++)
        {
            dependencies.Add(reader.ReadString());
        }

        return dependencies;
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
