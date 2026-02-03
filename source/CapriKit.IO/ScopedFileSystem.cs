namespace CapriKit.IO;


public class ForbiddenPathException(string Path, string RequiredBasePath)
    : Exception($"Attempt to access {Path} that does not have the required base path {RequiredBasePath}");

public sealed class ScopedFileSystem(DirectoryPath BasePath) : IVirtualFileSystem
{
    public Stream AppendWrite(FilePath file)
    {
        var absoluteFile = FindOrThrow(file);
        return absoluteFile.Open(FileMode.Append, FileAccess.Write, FileShare.Read);
    }

    public Stream CreateReadWrite(FilePath file)
    {
        var absoluteFile = GetFileInfo(file);
        if (!absoluteFile.Exists)
        {
            var directory = absoluteFile.DirectoryName ?? string.Empty;
            Directory.CreateDirectory(directory);
        }

        return absoluteFile.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
    }

    public void Delete(FilePath file)
    {
        var absoluteFile = GetFileInfo(file);
        absoluteFile.Delete();
    }

    public bool Exists(FilePath file)
    {
        var absoluteFile = GetFileInfo(file);
        return absoluteFile.Exists;
    }

    public DateTime LastWriteTime(FilePath file)
    {
        var absoluteFile = FindOrThrow(file);
        return absoluteFile.LastWriteTime;
    }

    public Stream OpenRead(FilePath file)
    {
        var absoluteFile = FindOrThrow(file);
        return absoluteFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public long SizeInBytes(FilePath file)
    {
        var absoluteFile = FindOrThrow(file);
        return absoluteFile.Length;
    }

    public IReadOnlyList<FilePath> List(DirectoryPath directory)
    {
        var absoluteDirectory = GetDirectoryInfo(directory);
        var files = absoluteDirectory.GetFiles();

        var filePaths = new List<FilePath>();
        foreach (var file in files.Select(f => new FilePath(f.FullName)))
        {
            var relativePath = file.GetPathRelativeTo(BasePath);
            filePaths.Add(relativePath);
        }

        return filePaths;
    }

    private FileInfo FindOrThrow(FilePath file)
    {
        var info = GetFileInfo(file);
        if (info.Exists)
        {
            return info;
        }

        throw new FileNotFoundException(null, file.ToString());
    }

    private FileInfo GetFileInfo(FilePath file)
    {
        var absolutePath = file.IsAbsolute ? file : file.ToAbsolute(BasePath);
        ThrowIfPathIsOutsideBasePath(absolutePath);
        return new FileInfo(absolutePath.ToString());
    }

    private DirectoryInfo GetDirectoryInfo(DirectoryPath path)
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
