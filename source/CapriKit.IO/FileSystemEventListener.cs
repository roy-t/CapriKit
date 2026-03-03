namespace CapriKit.IO;

public enum FileSystemChangeKind
{
    Created,
    Changed,
    Deleted,
}

public delegate void FileSystemEventHandler(object sender, (FilePath target, FileSystemChangeKind reason) e);

public sealed class FileSystemEventListener : IDisposable
{
    private readonly FileSystem FileSystem;
    private readonly FileSystemWatcher Watcher;

    private event FileSystemEventHandler? onFileChanged;

    public FileSystemEventListener(FileSystem fileSystem, DirectoryPath directory, bool includeSubDirectories = true)
    {
        FileSystem = fileSystem;
        Directory = directory;

        // Take the directory info since all file system types will point that to the true absolute path
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

        Watcher.Created += (s, e) => onFileChanged?.Invoke(s, (FileSystem.GetFilePath(e.FullPath), FileSystemChangeKind.Created));
        Watcher.Changed += (s, e) => onFileChanged?.Invoke(s, (FileSystem.GetFilePath(e.FullPath), FileSystemChangeKind.Changed));
        Watcher.Deleted += (s, e) => onFileChanged?.Invoke(s, (FileSystem.GetFilePath(e.FullPath), FileSystemChangeKind.Deleted));
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
