using CapriKit.IO.Watchers;

namespace CapriKit.IO;

public sealed class InMemoryFileSystem : IVirtualFileSystem
{
    private record InMemoryFile(InMemoryFileStream Stream, DateTime LastWriteTime);
    private readonly Dictionary<FilePath, InMemoryFile> Disk;
    private readonly List<InMemoryFileSystemWatcher> Watchers;

    public InMemoryFileSystem()
    {
        Disk = [];
        Watchers = [];
    }

    public Stream AppendWrite(FilePath file)
    {
        var inMemoryFile = FindOrThrow(file);
        Disk[file] = inMemoryFile with { LastWriteTime = DateTime.Now };

        var stream = inMemoryFile.Stream;
        stream.Position = stream.Length;
        RaiseChange(file, FileSystemChangeKind.Changed);
        return stream;
    }

    public Stream CreateReadWrite(FilePath file)
    {
        if (Disk.TryGetValue(file, out var inMemoryFile))
        {
            Disk[file] = inMemoryFile with { LastWriteTime = DateTime.Now };
            inMemoryFile.Stream.SetLength(0);
            inMemoryFile.Stream.Position = 0;
            RaiseChange(file, FileSystemChangeKind.Changed);
            return inMemoryFile.Stream;
        }

        var newStream = new InMemoryFileStream();
        Disk.Add(file, new InMemoryFile(newStream, DateTime.Now));
        RaiseChange(file, FileSystemChangeKind.Created);
        return newStream;
    }

    public void Delete(FilePath file)
    {
        if (Disk.Remove(file))
        {
            RaiseChange(file, FileSystemChangeKind.Deleted);
        }
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

    public IReadOnlyList<FilePath> List(DirectoryPath directory)
    {
        var files = new List<FilePath>();
        foreach (var key in Disk.Keys)
        {
            if (key.Directory.Equals(directory))
            {
                files.Add(key);
            }
        }

        return files;
    }

    public IVirtualFileSystemWatcher Watch(DirectoryPath directory, bool includeSubDirectories = true)
    {
        var watcher = new InMemoryFileSystemWatcher(this, directory, includeSubDirectories);
        Watchers.Add(watcher);
        return watcher;
    }

    private InMemoryFile FindOrThrow(FilePath file)
    {
        if (Disk.TryGetValue(file, out var value))
        {
            return value;
        }
        throw new FileNotFoundException(null, file.ToString());
    }

    private void RaiseChange(FilePath file, FileSystemChangeKind kind)
    {
        var @event = new VirtualFileSystemEvent(file, kind);
        foreach (var watcher in Watchers.ToArray())
        {
            watcher.Notify(this, @event);
        }
    }

    private void RemoveWatcher(InMemoryFileSystemWatcher watcher)
    {
        Watchers.Remove(watcher);
    }

    private sealed class InMemoryFileStream : MemoryStream
    {
        // Prevent disposing of the actual stream, just rewind it
        protected override void Dispose(bool disposing)
        {
            Position = 0;
        }
    }

    private sealed class InMemoryFileSystemWatcher(
        InMemoryFileSystem owner, DirectoryPath directory, bool includeSubDirectories)
        : IVirtualFileSystemWatcher
    {
        private event VirtualFileSystemEventHandler? onFileChanged;

        public event VirtualFileSystemEventHandler? OnFileChanged
        {
            add => onFileChanged += value;
            remove => onFileChanged -= value;
        }

        internal void Notify(object sender, VirtualFileSystemEvent @event)
        {
            if (Matches(@event.File))
            {
                onFileChanged?.Invoke(sender, @event);
            }
        }

        private bool Matches(FilePath file)
        {
            var fileDirectory = file.Directory;
            return includeSubDirectories
                ? fileDirectory.StartsWith(directory)
                : fileDirectory.Equals(directory);
        }

        public void Stop()
        {
            owner.RemoveWatcher(this);
        }
    }
}
