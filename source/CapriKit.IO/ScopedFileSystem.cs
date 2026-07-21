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
public class ReadOnlyScopedFileSystem : IReadOnlyVirtualFileSystem
{
    private readonly IReadOnlyVirtualFileSystem Source;

    public ReadOnlyScopedFileSystem(IReadOnlyVirtualFileSystem source, DirectoryPath basePath)
    {
        Source = source;
        BasePath = basePath.ToAbsolute();
    }


    /// <summary>
    /// Gets the absolute path of a file contained in this scoped file system. To be used with IO methods that are
    /// not aware of the virtual file system. Throws if the file points to outside the scoped file system,
    /// </summary>
    public FilePath GetAbsolutePath(FilePath file) => GetFilePath(file);


    /// <summary>
    /// Gets the absolute path of a directory contained in this scoped file system. To be used with IO methods that are
    /// not aware of the virtual file system. Throws if the directory points to outside the scoped file system,
    /// </summary>
    public DirectoryPath GetAbsolutePath(DirectoryPath directory) => GetDirectoryPath(directory);

    /// <summary>
    /// The absolute path of the directory this file system is scoped to.
    /// </summary>
    public DirectoryPath BasePath { get; }


    public bool Exists(FilePath file)
    {
        return Source.Exists(GetFilePath(file));
    }

    public DateTime LastWriteTime(FilePath file)
    {
        return Source.LastWriteTime(GetFilePath(file));
    }

    public IReadOnlyList<FilePath> List(DirectoryPath directory)
    {
        return Source.List(GetDirectoryPath(directory));
    }

    public Stream OpenRead(FilePath file)
    {
        return Source.OpenRead(GetFilePath(file));
    }

    public long SizeInBytes(FilePath file)
    {
        return Source.SizeInBytes(GetFilePath(file));
    }

    protected DirectoryPath GetDirectoryPath(DirectoryPath path)
    {
        var fullPath = path.GetPathRelativeTo(BasePath);
        ThrowIfPathIsOutsideBasePath(fullPath);

        return fullPath;
    }

    protected FilePath GetFilePath(FilePath path)
    {
        var fullPath = path.GetPathRelativeTo(BasePath);
        ThrowIfPathIsOutsideBasePath(fullPath);

        return fullPath;
    }

    protected void ThrowIfPathIsOutsideBasePath(FilePath file)
    {
        Debug.Assert(file.IsAbsolute);
        if (!file.StartsWith(BasePath))
        {
            throw new ForbiddenPathException(file, BasePath);
        }
    }

    protected void ThrowIfPathIsOutsideBasePath(DirectoryPath path)
    {
        Debug.Assert(path.IsAbsolute);
        if (!path.StartsWith(BasePath))
        {
            throw new ForbiddenPathException(path, BasePath);
        }
    }
}
