using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace CapriKit.Concurrency.Primitives;

/// <summary>
/// Lightweight variant of <see cref="System.Threading.Channels.Channel"/> that allows ONE reader
/// to receive items from MULTIPLE writers. Has the following semantics:
/// - void Write(T value) always succeeds and supports multiple writers working in parallel
/// - void Write(ExceptionDispatchInfo exception) always succeeds and supports multiple writers working in parallel
/// - bool TryRead(out T? value) is non blocking, it first drains the exceptions in the queue (one per call) then the items.
/// Note that there being no items doesn't mean the work is done. Users must keep track of the number of items
/// they expect to see if the work is done, there is no Completed method or completion tracking to avoid
/// problems like 'write after complete' that require extensive locking.
/// </summary>
public sealed class LightweightChannel<T> where T : notnull
{
    private readonly ConcurrentQueue<T> Queue;
    private readonly ConcurrentQueue<ExceptionDispatchInfo> Errors;
    public LightweightChannel()
    {
        Queue = [];
        Errors = [];
    }

    public void Write(T value)
    {
        Queue.Enqueue(value);
    }

    public void Write(ExceptionDispatchInfo exception)
    {
        Errors.Enqueue(exception);
    }

    public bool TryRead([NotNullWhen(true)] out T? value)
    {
        if (Errors.TryDequeue(out var error))
        {
            error.Throw();
        }

        if (Queue.TryDequeue(out value))
        {
            return true;
        }

        return false;
    }
}
