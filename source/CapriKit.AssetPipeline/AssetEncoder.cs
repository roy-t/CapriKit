using CapriKit.IO;
using CapriKit.IO.Streams;
using System.Buffers;
using System.IO.Pipelines;
using static CapriKit.AssetPipeline.AssetUtilities;

namespace CapriKit.AssetPipeline;

// File format: [encoder id][encoder version][settings length][settings][payload length][payload][dependency count][dependencies]

/// <summary>
/// Encodes the generic asset envelope and, using a specialized IAssetTranscoder, the asset itself
/// </summary>
internal static class AssetEncoder
{
    public static async Task Encode<TAsset>(AssetId id, IAssetSettings<TAsset> settings, IAssetTranscoder<TAsset> encoder, IVirtualFileSystem fileSystem)
    {
        ThrowOnFileNotFound(id.Path, fileSystem);
        var outputPath = ToEncodedFilePath(id);

        using var output = fileSystem.CreateReadWrite(outputPath);
        var writer = PipeWriter.Create(output);
        var spy = fileSystem.SpyOn();

        WriteHeader(writer, encoder);
        WriteSettings(writer, encoder, settings);
        await WritePayload(writer, id, settings, encoder, spy);
        WriteDependencies(writer, spy);

        await writer.FlushAsync();
        await writer.CompleteAsync();
    }

    private static void WriteHeader(PipeWriter writer, IAssetTranscoder encoder)
    {
        writer.Write(encoder.Id);
        writer.Write(encoder.Version);
    }

    private static void WriteSettings<TAsset>(PipeWriter writer, IAssetTranscoder<TAsset> transcoder, IAssetSettings<TAsset> settings)
    {
        var buffer = new ArrayBufferWriter<byte>();
        transcoder.WriteSettings(settings, buffer);
        writer.Write(buffer.WrittenCount);
        writer.Write(buffer.WrittenSpan);
    }

    private static async Task WritePayload<TAsset>(PipeWriter writer, AssetId id, IAssetSettings<TAsset> settings, IAssetTranscoder<TAsset> encoder, VirtualFileSystemSpy spy)
    {
        var payload = new ArrayBufferWriter<byte>();
        await encoder.Encode(id, settings, spy, payload);
        writer.Write(payload.WrittenCount);
        writer.Write(payload.WrittenSpan);
    }

    private static void WriteDependencies(PipeWriter writer, VirtualFileSystemSpy spy)
    {
        writer.Write(spy.OpenedFiles.Count);
        foreach (var dependency in spy.OpenedFiles)
        {
            var lastWrite = spy.LastWriteTime(dependency);
            writer.Write(lastWrite.Ticks);
            writer.Write(dependency);
        }
    }
}
