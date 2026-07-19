using CapriKit.IO;
using System.Buffers;
using System.IO.Hashing;

namespace CapriKit.AssetPipeline;

internal static class AssetUtilities
{
    public static FilePath ToEncodedFilePath(AssetId id)
    {
        if (string.IsNullOrEmpty(id.Key))
        {
            return $"{id.Path}.cka";
        }

        var key = IOUtilities.EscapeFileName(id.Key);
        return $"{id.Path}.{key}.cka";
    }

    public static void ThrowOnFileNotFound(FilePath path, IVirtualFileSystem fileSystem)
    {
        if (!fileSystem.Exists(path))
        {
            throw new FileNotFoundException(null, path);
        }
    }

    public static ReadOnlySpan<byte> HashSettings<TAsset, TSettings>(TSettings settings)
        where TSettings : IAssetSettings<TAsset>
    {
        var payload = new ArrayBufferWriter<byte>();
        settings.Write(payload);
        return XxHash128.Hash(payload.WrittenSpan);
    }
}
