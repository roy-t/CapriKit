namespace CapriKit.IO;

/// <summary>
/// Track which files were opened for reading.
/// </summary>
public sealed class ReadOnlyVirtualFileSystemSpy : IReadOnlyVirtualFileSystem
{
    private readonly IReadOnlyVirtualFileSystem Actual;
    private readonly HashSet<FilePath> OpenedFileSet;

    public ReadOnlyVirtualFileSystemSpy(IReadOnlyVirtualFileSystem actual)
    {
        Actual = actual;
        OpenedFileSet = [];
    }

    /// <summary>
    /// All files that have been read from
    /// </summary>
    public IReadOnlySet<FilePath> OpenedFiles => OpenedFileSet;

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
