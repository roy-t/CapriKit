using System.Diagnostics;

namespace CapriKit.IO;


public class ForbiddenPathException(string Path, string RequiredBasePath)
    : Exception($"Attempt to access {Path} that does not have the required base path {RequiredBasePath}");


/// <summary>
/// Limits access to the base path and all its sub-directories. Relative paths
/// are assumed to be relative to the base path. Using relative or absolute paths
/// that resolve to directories outside of the scope of this file system results
/// in a ForbiddenPathException.
/// </summary>
public sealed class ScopedFileSystem(IVirtualFileSystem source, DirectoryPath basePath)
    : ReadOnlyScopedFileSystem(source, basePath), IVirtualFileSystem
{
    public Stream AppendWrite(FilePath file)
    {
        return source.AppendWrite(GetFilePath(file));
    }

    public Stream CreateReadWrite(FilePath file)
    {
        return source.CreateReadWrite(GetFilePath(file));
    }

    public void Delete(FilePath file)
    {
        source.Delete(GetFilePath(file));
    }
}

/// <summary>
/// Limits access to the base path and all its sub-directories. Relative paths
/// are assumed to be relative to the base path. Using relative or absolute paths
/// that resolve to directories outside of the scope of this file system results
/// in a ForbiddenPathException.
/// </summary>
public class ReadOnlyScopedFileSystem(IReadOnlyVirtualFileSystem source, DirectoryPath basePath) : IReadOnlyVirtualFileSystem
{
    public bool Exists(FilePath file)
    {
        return source.Exists(GetFilePath(file));
    }

    public DateTime LastWriteTime(FilePath file)
    {
        return source.LastWriteTime(GetFilePath(file));
    }

    public IReadOnlyList<FilePath> List(DirectoryPath directory)
    {
        return source.List(GetDirectoryPath(directory));
    }

    public Stream OpenRead(FilePath file)
    {
        return source.OpenRead(GetFilePath(file));
    }

    public long SizeInBytes(FilePath file)
    {
        return source.SizeInBytes(GetFilePath(file));
    }

    protected DirectoryPath GetDirectoryPath(DirectoryPath path)
    {
        var fullPath = path.GetPathRelativeTo(basePath);
        ThrowIfPathIsOutsideBasePath(fullPath);

        return fullPath;
    }

    protected FilePath GetFilePath(FilePath path)
    {
        var fullPath = path.GetPathRelativeTo(basePath);
        ThrowIfPathIsOutsideBasePath(fullPath);

        return fullPath;
    }

    protected void ThrowIfPathIsOutsideBasePath(FilePath file)
    {
        Debug.Assert(file.IsAbsolute);
        if (!file.StartsWith(basePath))
        {
            throw new ForbiddenPathException(file, basePath);
        }
    }

    protected void ThrowIfPathIsOutsideBasePath(DirectoryPath path)
    {
        Debug.Assert(path.IsAbsolute);
        if (!path.StartsWith(basePath))
        {
            throw new ForbiddenPathException(path, basePath);
        }
    }
}
