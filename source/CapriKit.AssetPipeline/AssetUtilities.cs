using CapriKit.IO;

namespace CapriKit.AssetPipeline;

internal static class AssetUtilities
{
    public static FilePath ToEncodedFilePath(FilePath path)
    {
        return path + ".cka";
    }

    public static void ThrowOnFileNotFound(FilePath path, IVirtualFileSystem fileSystem)
    {
        if (!fileSystem.Exists(path))
        {
            throw new FileNotFoundException(null, path);
        }
    }
}
