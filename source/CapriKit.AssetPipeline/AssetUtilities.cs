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

    public static bool IsUpToDate<T>(Asset<T> asset, IReadOnlyVirtualFileSystem fileSystem)
    {
        foreach (var (file, version) in asset.Dependencies)
        {
            if (!fileSystem.Exists(file))
            {
                return false;
            }

            var lastWrite = fileSystem.LastWriteTime(file);
            if (version < lastWrite)
            {
                return false;
            }
        }

        return true;
    }
}
