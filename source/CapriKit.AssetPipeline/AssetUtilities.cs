using CapriKit.IO;

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
}
