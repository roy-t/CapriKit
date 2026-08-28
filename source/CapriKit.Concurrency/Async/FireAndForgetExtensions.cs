using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace CapriKit.Concurrency.Async;

public static class FireAndForgetExtensions
{
    /// <summary>
    /// Fires and forgets a task while safely observing any exceptions.
    /// <paramref name="onCompleted"/> is invoked once the task has finished,
    /// whether it completed successfully, was cancelled, or faulted.
    /// </summary>
    public static void FireAndForget(
        this Task task,
        Action<ExceptionDispatchInfo> onException,
        Action? onCompleted = null)
    {
        _ = AwaitAndCatch(task, onException, onCompleted);
    }

    private static async Task AwaitAndCatch(
        Task task,
        Action<ExceptionDispatchInfo> onException,
        Action? onCompleted)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignore cooperative cancellations
        }
        catch (Exception ex)
        {
            // Preserver the original stack trace and allow the handler to rethrow it.
            var capture = ExceptionDispatchInfo.Capture(ex);
            onException(capture);
        }
        finally
        {
            onCompleted?.Invoke();
        }
    }
}
