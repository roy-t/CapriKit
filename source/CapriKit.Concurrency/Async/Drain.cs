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

    public Drain(CancellationTokenSource cancellation, LightweightChannel<JobResult<T>> channel)
    {
        Cancellation = cancellation;
        Channel = channel;
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
