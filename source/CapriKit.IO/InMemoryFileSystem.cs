
namespace CapriKit.IO;

public sealed class InMemoryFileSystem : IVirtualFileSystem
{
    private record InMemoryFile(InMemoryFileStream Stream, DateTime LastWriteTime);
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

        var newStream = new InMemoryFileStream();
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
        var (stream, _) = FindOrThrow(file);
        stream.Position = 0;
        return stream;
    }

    public long SizeInBytes(FilePath file)
    {
        var (stream, _) = FindOrThrow(file);
        return stream.Length;
    }

    private InMemoryFile FindOrThrow(FilePath file)
    {
        if (Disk.TryGetValue(file, out var value))
        {
            return value;
        }
        throw new FileNotFoundException(null, file.ToString());
    }

    private sealed class InMemoryFileStream : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
            Position = 0;
        }

        public override ValueTask DisposeAsync()
        {
            Position = 0;
            return ValueTask.CompletedTask;
        }

        public override void Close()
        {
            Position = 0;
        }
    }
}
