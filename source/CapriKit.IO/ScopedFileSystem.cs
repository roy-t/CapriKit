namespace CapriKit.IO;


public class ForbiddenPathException(string Path, string RequiredBasePath)
    : Exception($"Attempt to access {Path} that does not have the required base path {RequiredBasePath}");

/// <summary>
/// Limits access to the base path and all its sub-directories. Relative paths
/// are assumed to be relative to the base path. Using relative or absolute paths
/// that resolve to directories outside of the scope of this file system results
/// in a ForbiddenPathException.
/// </summary>
public sealed class ScopedFileSystem(DirectoryPath basePath) : FileSystem
{
    public string BasePath { get; } = basePath;

    protected override FilePath GetFilePath(string path)
    {
        var originalPath = base.GetFilePath(path);
        return originalPath.GetPathRelativeTo(BasePath);
    }

    protected override FileInfo GetFileInfo(FilePath file)
    {
        var absolutePath = file.IsAbsolute ? file : file.ToAbsolute(BasePath);
        ThrowIfPathIsOutsideBasePath(absolutePath);
        return new FileInfo(absolutePath.ToString());
    }

    protected override DirectoryInfo GetDirectoryInfo(DirectoryPath path)
    {
        var absolutePath = path.IsAbsolute ? path : path.ToAbsolute(BasePath);
        ThrowIfPathIsOutsideBasePath(absolutePath);
        return new DirectoryInfo(absolutePath.ToString());
    }

    private void ThrowIfPathIsOutsideBasePath(FilePath file)
    {
        if (file.IsAbsolute)
        {
            if (!file.StartsWith(BasePath))
            {
                throw new ForbiddenPathException(file, BasePath);
            }
        }
    }

    private void ThrowIfPathIsOutsideBasePath(DirectoryPath path)
    {
        if (path.IsAbsolute)
        {
            if (!path.StartsWith(BasePath))
            {
                throw new ForbiddenPathException(path, BasePath);
            }
        }
    }
}
