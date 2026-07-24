namespace CapriKit.IO.Watchers;

/// <summary>
/// Listens for file changes and notifies interested parties via an event
/// </summary>
public sealed class FileSystemEventListener : IVirtualFileSystemWatcher, IDisposable
{
    private readonly FileSystemWatcher Watcher;

    private event VirtualFileSystemEventHandler? onFileChanged;

    public FileSystemEventListener(DirectoryPath directory, bool includeSubDirectories = true)
    {
        Directory = directory;
        var directoryInfo = FileSystem.GetDirectoryInfo(directory);
        if (!directoryInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryInfo.FullName}");
        }

        Watcher = new FileSystemWatcher(directoryInfo.FullName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            IncludeSubdirectories = includeSubDirectories,
            EnableRaisingEvents = true,
        };

        Watcher.Created += (s, e) => onFileChanged?.Invoke(s, new VirtualFileSystemEvent(FileSystem.GetFilePath(e.FullPath), FileSystemChangeKind.Created));
        Watcher.Changed += (s, e) => onFileChanged?.Invoke(s, new VirtualFileSystemEvent(FileSystem.GetFilePath(e.FullPath), FileSystemChangeKind.Changed));
        Watcher.Deleted += (s, e) => onFileChanged?.Invoke(s, new VirtualFileSystemEvent(FileSystem.GetFilePath(e.FullPath), FileSystemChangeKind.Deleted));
    }

    public event VirtualFileSystemEventHandler? OnFileChanged
    {
        add
        {
            onFileChanged += value;
        }
        remove
        {
            onFileChanged -= value;
        }
    }

    public DirectoryPath Directory { get; }

    public void Stop()
    {
        Dispose();
    }

    public void Dispose()
    {
        Watcher.Dispose();
    }
}
