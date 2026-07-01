using System.Runtime.CompilerServices;

namespace CapriKit.Concurrency.Async;

public static class FireAndForgetExtensions
{
    /// <summary>
    /// Fires and forgets a task while safely observing any exceptions
    /// </summary>
    public static void FireAndForget(
        this Task task,
        Action<Exception> onException,
        Action? onCompleted = null,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        _ = AwaitAndCatch(task, onException, onCompleted, member, file, line);
    }

    private static async Task AwaitAndCatch(
        Task task,
        Action<Exception> onException,
        Action? onCompleted,
        string member,
        string file,
        int line)
    {
        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        try
        {
            await task.ConfigureAwait(false);
            onCompleted?.Invoke();

        }
        catch (OperationCanceledException)
        {
            // Ignore cooperative cancellations
        }
        catch (Exception ex)
        {
            // TODO: does this make sense, does this break the stacktrace
            onException(new FaFTaskException(ex, member, file, line));
        }
    }
}

public sealed class FaFTaskException : Exception
{
    public FaFTaskException(Exception innerException, string member, string file, int line)
        : base($"A fire-and-forget task started from '{member}' at {file}:{line} failed.", innerException)
    {
        Member = member;
        File = file;
        Line = line;
    }

    public string Member { get; }
    public string File { get; }
    public int Line { get; }
}
