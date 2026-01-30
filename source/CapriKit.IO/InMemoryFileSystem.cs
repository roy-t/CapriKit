namespace CapriKit.IO;

public sealed class InMemoryFileSystem : IVirtualFileSystem
{
    private record InMemoryFile(MemoryStream Stream, DateTime LastWriteTime);
    private readonly Dictionary<FilePath, InMemoryFile> Disk;

    public InMemoryFileSystem()
    {
        Disk = new Dictionary<FilePath, InMemoryFile>();
    }

    public Stream AppendReadWrite(FilePath file)
    {
        var inMemoryFile = FindOrThrow(file);
        Disk[file] = inMemoryFile with { LastWriteTime = DateTime.Now };

        var stream = inMemoryFile.Stream;
        stream.Position = stream.Length;
        return stream;
    }

    public Stream CreateReadWrite(FilePath file)
    {
        if (Disk.TryGetValue(file, out var inMemoryFile))
        {
            Disk[file] = inMemoryFile with { LastWriteTime = DateTime.Now };
            inMemoryFile.Stream.SetLength(0);
            inMemoryFile.Stream.Position = 0;
            return inMemoryFile.Stream;
        }

        var newStream = new MemoryStream();
        Disk.Add(file, new InMemoryFile(newStream, DateTime.Now));
        return newStream;
    }

    public bool Exists(FilePath file)
    {
        return Disk.ContainsKey(file);
    }

    public DateTime LastWriteTime(FilePath file)
    {
        var (_, timestamp) = FindOrThrow(file);
        return timestamp;
    }

    public Stream OpenRead(FilePath file)
    {
        throw new NotImplementedException();
    }

    public int SizeInBytes(FilePath file)
    {
        throw new NotImplementedException();
    }

    private InMemoryFile FindOrThrow(FilePath file)
    {
        if (Disk.TryGetValue(file, out var value))
        {
            return value;
        }
        throw new FileNotFoundException(null, file.ToString());
    }
}
