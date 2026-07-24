namespace CapriKit.IO.Watchers;

/// <summary>
/// Wrapper for a general IVirtualFileSystemWatcher that ensures paths are relative to the basePath
/// </summary>
internal class ScopedFileSystemEventListener : IVirtualFileSystemWatcher
{
    private readonly IVirtualFileSystemWatcher Inner;
    private readonly DirectoryPath BasePath;

    public ScopedFileSystemEventListener(IVirtualFileSystemWatcher inner, DirectoryPath basePath)
    {
        Inner = inner;
        BasePath = basePath;
        Inner.OnFileChanged += OnInner;
    }

    public event VirtualFileSystemEventHandler? OnFileChanged;

    private void OnInner(object sender, VirtualFileSystemEvent e)
    {
        OnFileChanged?.Invoke(this, e with { File = e.File.GetPathRelativeTo(BasePath) });
    }

    public void Stop() { Inner.OnFileChanged -= OnInner; Inner.Stop(); }
}
