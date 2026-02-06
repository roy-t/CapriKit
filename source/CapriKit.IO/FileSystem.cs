namespace CapriKit.IO;

public class FileSystem : IVirtualFileSystem
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
            var filePath = GetFilePath(file.FullName);
            filePaths.Add(filePath);
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

    protected virtual FilePath GetFilePath(string path)
    {
        return new FilePath(path);
    }

    protected virtual FileInfo GetFileInfo(FilePath file)
    {
        var absolutePath = file.IsAbsolute ? file : file.ToAbsolute();
        return new FileInfo(absolutePath.ToString());
    }

    protected virtual DirectoryInfo GetDirectoryInfo(DirectoryPath path)
    {
        var absolutePath = path.IsAbsolute ? path : path.ToAbsolute();
        return new DirectoryInfo(absolutePath.ToString());
    }
}
