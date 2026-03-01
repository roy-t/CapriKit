namespace CapriKit.IO;

/// <summary>
/// Track which files were opened for reading or writing,
/// as well as files that were created and deleted.
/// </summary>
public sealed class VirtualFileSystemSpy : IVirtualFileSystem
{
    private readonly IVirtualFileSystem Actual;
    private readonly HashSet<FilePath> OpenedFileSet;


    public VirtualFileSystemSpy(IVirtualFileSystem actual)
    {
        Actual = actual;
        OpenedFileSet = [];
    }

    /// <summary>
    /// All files that have been read, written to, created or deleted
    /// </summary>
    public IReadOnlySet<FilePath> OpenedFiles => OpenedFileSet;

    public Stream AppendWrite(FilePath file)
    {
        OpenedFileSet.Add(file);
        return Actual.AppendWrite(file);
    }

    public Stream CreateReadWrite(FilePath file)
    {
        OpenedFileSet.Add(file);
        return Actual.CreateReadWrite(file);
    }

    public void Delete(FilePath file)
    {
        OpenedFileSet.Add(file);
        Actual.Delete(file);
    }

    public bool Exists(FilePath file)
    {
        return Actual.Exists(file);
    }

    public DateTime LastWriteTime(FilePath file)
    {
        return Actual.LastWriteTime(file);
    }

    public IReadOnlyList<FilePath> List(DirectoryPath directory)
    {
        return Actual.List(directory);
    }

    public Stream OpenRead(FilePath file)
    {
        OpenedFileSet.Add(file);
        return Actual.OpenRead(file);
    }

    public long SizeInBytes(FilePath file)
    {
        return Actual.SizeInBytes(file);
    }
}
