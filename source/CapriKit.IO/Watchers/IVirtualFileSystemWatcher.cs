namespace CapriKit.IO.Watchers;

public enum FileSystemChangeKind
{
    Created,
    Changed,
    Deleted,

    /// <summary>
    /// A file is renamed, note that a lot of text/code editors first store changes in a temporary
    /// file before renaming and overwriting an existing file.
    /// </summary>
    Renamed,
}

/// <param name="File">The absolute path to the file affected</param>
/// <param name="Kind">The kind of change the file underwent</param>
public record VirtualFileSystemEvent(FilePath File, FileSystemChangeKind Kind);

public delegate void VirtualFileSystemEventHandler(object sender, VirtualFileSystemEvent e);

public interface IVirtualFileSystemWatcher
{
    public event VirtualFileSystemEventHandler? OnFileChanged;

    public void Stop();
}
