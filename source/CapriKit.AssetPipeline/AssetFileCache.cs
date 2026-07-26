using CapriKit.IO;
using System.Buffers;

namespace CapriKit.AssetPipeline;

internal sealed class AssetFileCache(IVirtualFileSystem FileSystem, TranscoderCollection Transcoders)
{
    public async Task<AssetJob<TAsset>> Load<TAsset>(AssetId id, IAssetSettings<TAsset> settings)
        where TAsset : class
    {
        var transcoder = Transcoders.Get<TAsset>();

        var job = await AssetDecoder.Decode(id, transcoder, FileSystem);
        return job.Match(
            (_, asset) =>
            {
                if (IsUpToDate(asset) && SettingsEqual(transcoder, asset.Settings, settings))
                {
                    return job;
                }
                return AssetJob<TAsset>.Missing(id);
            },
            (_, _) => job,
            (_) => job
        );
    }

    private bool IsUpToDate<T>(Asset<T> asset)
       where T : class
    {
        foreach (var (file, version) in asset.Dependencies)
        {
            if (!FileSystem.Exists(file))
            {
                return false;
            }

            var lastWrite = FileSystem.LastWriteTime(file);
            if (version < lastWrite)
            {
                return false;
            }
        }

        return true;
    }

    private static bool SettingsEqual<T>(IAssetTranscoder<T> transcoder, IAssetSettings<T> embedded, IAssetSettings<T> requested)
    {
        var embeddedWriter = new ArrayBufferWriter<byte>();
        transcoder.WriteSettings(embedded, embeddedWriter);

        var requestedWriter = new ArrayBufferWriter<byte>();
        transcoder.WriteSettings(requested, requestedWriter);

        return embeddedWriter.WrittenSpan.SequenceEqual(requestedWriter.WrittenSpan);
    }
}
