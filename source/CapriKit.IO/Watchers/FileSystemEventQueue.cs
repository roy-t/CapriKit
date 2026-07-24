using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.IO.Watchers;

/// <summary>
/// Listens for file changes and puts events in a queue so an interested party can control when to handle them.
/// </summary>
public sealed class FileSystemEventQueue
{
    private readonly ConcurrentQueue<VirtualFileSystemEvent> Queue;

    public FileSystemEventQueue(IVirtualFileSystemWatcher watcher)
    {
        Queue = new ConcurrentQueue<VirtualFileSystemEvent>();
        watcher.OnFileChanged += (s, e) => Queue.Enqueue(e);
    }

    public int Count => Queue.Count;

    public bool TryDequeue([NotNullWhen(true)] out VirtualFileSystemEvent? @event)
    {
        return Queue.TryDequeue(out @event);
    }
}
