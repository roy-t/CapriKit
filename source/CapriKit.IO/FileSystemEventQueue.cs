using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.IO;

/// <summary>
/// Listens for file changes and puts events in a queue so an interested party can control when to handle them.
/// </summary>
public sealed class FileSystemEventQueue : IDisposable
{
    private readonly ConcurrentQueue<FileSystemEvent> Queue;
    private readonly FileSystemEventListener Events;

    public FileSystemEventQueue(DirectoryPath directory, bool includeSubDirectories = true)
    {
        Queue = new ConcurrentQueue<FileSystemEvent>();
        Events = new FileSystemEventListener(directory, includeSubDirectories);
        Events.OnFileChanged += (s, e) =>
        {
            Queue.Enqueue(e);
        };
    }

    public int Count => Queue.Count;

    public bool TryDequeue([NotNullWhen(true)] out FileSystemEvent? @event)
    {
        return Queue.TryDequeue(out @event);
    }

    public void Dispose()
    {
        Events.Dispose();
    }
}
