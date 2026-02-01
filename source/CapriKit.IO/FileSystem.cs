namespace CapriKit.IO;

public sealed class FileSystem : IVirtualFileSystem
{
    public FileSystem(string basePath)
    {
        BasePath = basePath;
    }

    public string BasePath { get; }

    public Stream AppendReadWrite(FilePath file)
    {
        var absoluteFile = FindOrThrow(file);
        return absoluteFile.Open(FileMode.Append, FileAccess.ReadWrite, FileShare.Read);
    }

    public Stream CreateReadWrite(FilePath file)
    {
        throw new NotImplementedException();
    }

    public bool Exists(FilePath file)
    {
        throw new NotImplementedException();
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

    private FileInfo FindOrThrow(FilePath file)
    {
        var absolutePath = file.ToAbsolute(BasePath);
        var info = new FileInfo(absolutePath.ToString());
        if (info.Exists)
        {
            return info;
        }

        throw new FileNotFoundException(null, file.ToString());
    }
}
