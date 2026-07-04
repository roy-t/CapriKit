using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace CapriKit.Concurrency.Primitives;

/// <summary>
/// Lightweight variant of <see cref="System.Threading.Channels.Channel"/> that allows ONE reader
/// to receive items from MULTIPLE writers. Has the following semantics:
/// - void Write(T value) always succeeds and supports multiple writers working in parallel
/// - void Write(Exception exception) always succeeds but only keeps the first exception
/// - bool TryRead(out T? value) is non blocking, it returns one item from the internal queue if available
/// if there are no items but an exception was written, it rethrows the exception.
///
/// Note that there being no items doesn't mean the work is done. Users must keep track of the number of items
/// they expect to see if the work is done, there is no Completed method or completion tracking to avoid
/// problems like 'write after complete' that require extensive locking.
/// </summary>
public sealed class LightweightChannel<T> where T : notnull
{
    private readonly ConcurrentQueue<T> Queue;
    private volatile ExceptionDispatchInfo? Error;

    public LightweightChannel()
    {
        Queue = [];
        Error = null;
    }

    public void Write(T value)
    {
        Queue.Enqueue(value);
    }

    public void Write(Exception exception)
    {
        var error = ExceptionDispatchInfo.Capture(exception);
        Interlocked.CompareExchange(ref Error, error, null);
    }

    public bool TryRead([NotNullWhen(true)] out T? value)
    {
        if (Queue.TryDequeue(out value))
        {
            return true;
        }

        Error?.Throw();
        return false;
    }
}
