using System.IO;

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
        foreach (var file in files)
        {
            var relativePath = MakeRelative(file.FullName);
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

    private FilePath MakeRelative(string path)
    {
        var comparisonType = IOUtilities.GetOSPathComparisonType();
        var relativePath = path.Replace(BasePath, string.Empty, comparisonType);

        return new FilePath(relativePath);
    }

    private void ThrowIfPathIsOutsideBasePath(FilePath file)
    {
        if (file.IsAbsolute)
        {
            var comparisonType = IOUtilities.GetOSPathComparisonType();
            var path = file.ToString();
            var basePath = BasePath.ToString();

            if (!path.StartsWith(basePath, comparisonType) && !path.Equals(basePath, comparisonType))
            {
                throw new ForbiddenPathException(path, basePath);
            }
        }
    }

    private void ThrowIfPathIsOutsideBasePath(DirectoryPath directory)
    {
        if (directory.IsAbsolute)
        {
            var comparisonType = IOUtilities.GetOSPathComparisonType();
            var path = directory.ToString();
            var basePath = BasePath.ToString();

            if (!path.StartsWith(basePath, comparisonType) && !path.Equals(basePath, comparisonType))
            {
                throw new ForbiddenPathException(path, basePath);
            }
        }
    }
}
