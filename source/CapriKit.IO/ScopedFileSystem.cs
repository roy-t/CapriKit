namespace CapriKit.IO;

public sealed class ScopedFileSystem(DirectoryPath BasePath) : IVirtualFileSystem
{
    public Stream AppendReadWrite(FilePath file)
    {
        var absoluteFile = FindOrThrow(file);
        return absoluteFile.Open(FileMode.Append, FileAccess.ReadWrite, FileShare.Read);
    }

    public Stream CreateReadWrite(FilePath file)
    {
        var absoluteFile = GetFileInfo(file);
        if (!absoluteFile.Exists)
        {
            Directory.CreateDirectory(absoluteFile.FullName);
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

    private FileInfo GetFileInfo(FilePath file)
    {
        // TODO: add a better check to make sure that this file is really
        // in the basePath and doesn't use .. etc.. to get somewhere else
        var absolutePath = file.ToAbsolute(BasePath);
        return new FileInfo(absolutePath.ToString());
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
}
