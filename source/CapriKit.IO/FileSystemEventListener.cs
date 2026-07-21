namespace CapriKit.IO;

public enum FileSystemChangeKind
{
    Created,
    Changed,
    Deleted,
}

/// <param name="File">The absolute path to the file affected</param>
/// <param name="Kind">The kind of change the file underwent</param>
public record FileSystemEvent(FilePath File, FileSystemChangeKind Kind);

public delegate void FileSystemEventHandler(object sender, FileSystemEvent e);

/// <summary>
/// Listens for file changes and notifies interested parties via an event
/// </summary>
public sealed class FileSystemEventListener : IDisposable
{
    private readonly FileSystemWatcher Watcher;

    private event FileSystemEventHandler? onFileChanged;

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

        Watcher.Created += (s, e) => onFileChanged?.Invoke(s, new FileSystemEvent(FileSystem.GetFilePath(e.FullPath), FileSystemChangeKind.Created));
        Watcher.Changed += (s, e) => onFileChanged?.Invoke(s, new FileSystemEvent(FileSystem.GetFilePath(e.FullPath), FileSystemChangeKind.Changed));
        Watcher.Deleted += (s, e) => onFileChanged?.Invoke(s, new FileSystemEvent(FileSystem.GetFilePath(e.FullPath), FileSystemChangeKind.Deleted));
    }

    public event FileSystemEventHandler? OnFileChanged
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

    public void Dispose()
    {
        Watcher.Dispose();
    }
}
