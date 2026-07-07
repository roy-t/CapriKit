using CapriKit.Concurrency.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.Concurrency.Async;

/// <summary>
/// Allows you to drain work as it completes
/// </summary>
public sealed class Drain<T>
{
    private readonly CancellationTokenSource Cancellation;
    private readonly LightweightChannel<JobResult<T>> Channel;

    private int outstanding;

    public Drain(CancellationTokenSource cancellation, LightweightChannel<JobResult<T>> channel)
    {
        Cancellation = cancellation;
        Channel = channel;
    }

    /// <summary>
    /// True while work that has already started has not yet been completed
    /// </summary>
    public bool HasOutstandingWork()
    {
        return Volatile.Read(ref outstanding) > 0;
    }

    internal void OnJobStarted()
    {
        Interlocked.Increment(ref outstanding);
    }

    internal void OnJobCompleted()
    {
        Interlocked.Decrement(ref outstanding);
    }

    public void Cancel()
    {
        Cancellation.Cancel();
    }

    public bool TryTake([NotNullWhen(true)] out JobResult<T>? job)
    {
        return Channel.TryRead(out job);
    }
}
