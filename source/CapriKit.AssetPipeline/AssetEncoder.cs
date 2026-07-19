using CapriKit.IO;
using CapriKit.IO.Buffers;
using System.Buffers;
using System.IO.Pipelines;
using static CapriKit.AssetPipeline.AssetUtilities;

namespace CapriKit.AssetPipeline;

internal sealed class AssetEncoder
{
    public static async Task Encode<TAsset, TSettings>(AssetId id, TSettings settings, IAssetTranscoder<TAsset, TSettings> encoder, IVirtualFileSystem fileSystem)
        where TSettings : IAssetSettings<TAsset>
    {
        ThrowOnFileNotFound(id.Path, fileSystem);
        var outputPath = ToEncodedFilePath(id);

        using var output = fileSystem.CreateReadWrite(outputPath);
        var writer = PipeWriter.Create(output);
        var spy = fileSystem.SpyOn();

        WriteHeader(writer, encoder, settings); // Payload length is written in WritePayload

        await WritePayload(writer, id, settings, encoder, spy);
        WriteDependencies(writer, spy);

        await writer.FlushAsync();
        await writer.CompleteAsync();
    }

    private static void WriteHeader<TAsset, TSettings>(PipeWriter writer, IAssetTranscoder<TAsset, TSettings> encoder, TSettings settings)
        where TSettings : IAssetSettings<TAsset>
    {
        writer.Write(encoder.Id);
        writer.Write(encoder.Version);

        var hash = HashSettings<TAsset, TSettings>(settings);
        writer.Write(hash);
    }

    private static async Task WritePayload<TAsset, TSettings>(PipeWriter writer, AssetId id, TSettings settings, IAssetTranscoder<TAsset, TSettings> encoder, VirtualFileSystemSpy spy)
        where TSettings : IAssetSettings<TAsset>
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
            writer.Write(dependency);
        }
    }
}
