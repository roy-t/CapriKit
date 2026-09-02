namespace CapriKit.IO.Watchers;

public enum FileSystemChangeKind
{
    Created,
    Changed,
    Deleted,
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
