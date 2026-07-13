using CapriKit.IO;
using CapriKit.IO.Buffers;
using System.Buffers;
using System.IO.Pipelines;
using static CapriKit.AssetPipeline.AssetUtilities;

namespace CapriKit.AssetPipeline;

internal sealed class AssetEncoder
{
    public static async Task Encode(AssetId id, IVirtualFileSystem fileSystem, IAssetEncoder encoder)
    {
        ThrowOnFileNotFound(id.Path, fileSystem);
        var outputPath = ToEncodedFilePath(id.Path);

        using var output = fileSystem.CreateReadWrite(outputPath);
        var writer = PipeWriter.Create(output);
        var spy = fileSystem.SpyOn();

        WriteHeader(writer, encoder); // payload length is written in WritePayload
        await WritePayload(id, encoder, writer, spy);
        WriteDependencies(spy, writer);

        await writer.FlushAsync();
        await writer.CompleteAsync();
    }

    private static void WriteHeader(PipeWriter writer, IAssetEncoder encoder)
    {
        writer.Write(encoder.Id);
        writer.Write(encoder.Version);
    }

    private static async Task WritePayload(AssetId id, IAssetEncoder encoder, PipeWriter writer, VirtualFileSystemSpy spy)
    {
        var payload = new ArrayBufferWriter<byte>();
        await encoder.Encode(id, spy, payload);
        writer.Write(payload.WrittenCount);
        writer.Write(payload.WrittenSpan);
    }

    private static void WriteDependencies(VirtualFileSystemSpy spy, PipeWriter writer)
    {
        writer.Write(spy.OpenedFiles.Count);
        foreach (var dependency in spy.OpenedFiles)
        {
            writer.Write(dependency);
        }
    }


}
