using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CapriKit.Concurrency.Async;

// TODO: this is vibe coded code that is waaaaaaayy to verbose, but solves some problems. See what is actually the core and useful



/// <summary>
/// Central configuration hub and ergonomic lambda entry points for safe
/// "fire and forget". Configure once at start-up via <see cref="Initialize"/>.
/// </summary>
public static class FireAndForget
{
    internal static Action<Exception>? DefaultExceptionHandler;
    internal static bool IgnoreCanceledExceptions = true;

    /// <summary>Wire up global behaviour once (e.g. in Program.cs / Startup.cs).</summary>
    /// <param name="defaultExceptionHandler">
    /// Invoked whenever a forgotten task faults and the call site supplied no handler
    /// of its own. Point this at your logger so a fault is never lost.
    /// </param>
    /// <param name="ignoreCanceledExceptions">
    /// When true (default) an <see cref="OperationCanceledException"/> is treated as
    /// normal cooperative cancellation and never reported as an error.
    /// </param>
    public static void Initialize(
        Action<Exception>? defaultExceptionHandler,
        bool ignoreCanceledExceptions = true)
    {
        DefaultExceptionHandler = defaultExceptionHandler;
        IgnoreCanceledExceptions = ignoreCanceledExceptions;
    }

    /// <summary>
    /// Optional belt-and-braces: route any *genuinely* unobserved task exception (one
    /// that escaped a typed fire-and-forget) to the default handler and mark it
    /// observed, so a stray fault can never tear the process down.
    /// </summary>
    public static void WireGlobalSafetyNet()
    {
        TaskScheduler.UnobservedTaskException += static (_, e) =>
        {
            e.SetObserved();
            DefaultExceptionHandler?.Invoke(e.Exception);
        };
    }

    // ---- Ergonomic lambda entry points (Obelisk-style) ----------------------
    // The extension methods below are the primary API and cover every awaitable
    // type; these two overloads are sugar for the most common call shapes.

    /// <summary>Start async work and forget it. The synchronous prologue runs on the calling thread.</summary>
    public static void Run(
        Func<Task> work,
        Action<Exception>? onException = null,
        bool continueOnCapturedContext = false,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        => work().SafeFireAndForget(onException, continueOnCapturedContext, member, file, line);

    /// <summary>Offload synchronous work to the thread pool and forget it.</summary>
    public static void Run(
        Action work,
        Action<Exception>? onException = null,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        => Task.Run(work).SafeFireAndForget(onException, false, member, file, line);
}

/// <summary>
/// Safe fire-and-forget extension methods for Task, Task&lt;T&gt;, ValueTask and ValueTask&lt;T&gt;.
/// </summary>
public static class SafeFireAndForgetExtensions
{
    // ===================== Task / Task<T> =====================
    // Task<T> derives from Task, so these overloads also cover Task<T>
    // (the result is simply discarded).

    public static void SafeFireAndForget(
        this Task task,
        Action<Exception>? onException = null,
        bool continueOnCapturedContext = false,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        => task.SafeFireAndForget<Exception>(onException, continueOnCapturedContext, member, file, line);

    public static void SafeFireAndForget<TException>(
        this Task task,
        Action<TException>? onException = null,
        bool continueOnCapturedContext = false,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        where TException : Exception
    {
        // Ben Adams fast-path: a task that already finished successfully has nothing to
        // observe, so we avoid allocating an async state machine entirely.
        // (.NET Framework: use task.Status == TaskStatus.RanToCompletion instead.)
        if (task.IsCompletedSuccessfully)
            return;

        _ = AwaitAndCatch(task, onException, continueOnCapturedContext,
                          new CallerInfo(member, file, line));
    }

    static async Task AwaitAndCatch<TException>(
        Task task,
        Action<TException>? onException,
        bool continueOnCapturedContext,
        CallerInfo caller)
        where TException : Exception
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (OperationCanceledException) when (FireAndForget.IgnoreCanceledExceptions)
        {
            // Cooperative cancellation is normal control flow, not a fault.
        }
        catch (TException ex)
        {
            ExceptionDispatcher.Dispatch(ex, onException, caller);
        }
    }

    // ===================== ValueTask =====================
    // A ValueTask may only be consumed once, so there is no completed fast-path:
    // we always await exactly once.

    public static void SafeFireAndForget(
        this ValueTask task,
        Action<Exception>? onException = null,
        bool continueOnCapturedContext = false,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        => task.SafeFireAndForget<Exception>(onException, continueOnCapturedContext, member, file, line);

    public static void SafeFireAndForget<TException>(
        this ValueTask task,
        Action<TException>? onException = null,
        bool continueOnCapturedContext = false,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        where TException : Exception
    {
        _ = AwaitAndCatch(task, onException, continueOnCapturedContext,
                          new CallerInfo(member, file, line));
    }

    static async Task AwaitAndCatch<TException>(
        ValueTask task,
        Action<TException>? onException,
        bool continueOnCapturedContext,
        CallerInfo caller)
        where TException : Exception
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (OperationCanceledException) when (FireAndForget.IgnoreCanceledExceptions)
        {
        }
        catch (TException ex)
        {
            ExceptionDispatcher.Dispatch(ex, onException, caller);
        }
    }

    // ===================== ValueTask<T> =====================

    public static void SafeFireAndForget<TResult>(
        this ValueTask<TResult> task,
        Action<Exception>? onException = null,
        bool continueOnCapturedContext = false,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        _ = AwaitAndCatch(task, onException, continueOnCapturedContext,
                          new CallerInfo(member, file, line));
    }

    static async Task AwaitAndCatch<TResult>(
        ValueTask<TResult> task,
        Action<Exception>? onException,
        bool continueOnCapturedContext,
        CallerInfo caller)
    {
        try
        {
            _ = await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (OperationCanceledException) when (FireAndForget.IgnoreCanceledExceptions)
        {
        }
        catch (Exception ex)
        {
            ExceptionDispatcher.Dispatch(ex, onException, caller);
        }
    }
}

/// <summary>Immutable call-site breadcrumb captured for diagnostics.</summary>
readonly struct CallerInfo
{
    public readonly string Member;
    public readonly string File;
    public readonly int Line;

    public CallerInfo(string member, string file, int line)
    {
        Member = member;
        File = file;
        Line = line;
    }

    public override string ToString() => $"{Member} ({File}:{Line})";
}

/// <summary>Resolves which handler deals with a faulted fire-and-forget task.</summary>
static class ExceptionDispatcher
{
    public static void Dispatch<TException>(
        TException ex,
        Action<TException>? onException,
        in CallerInfo caller)
        where TException : Exception
    {
        // 1) A handler supplied at the call site always wins.
        if (onException is not null)
        {
            onException(ex);
            return;
        }

        // 2) Otherwise fall back to the globally-configured handler.
        if (FireAndForget.DefaultExceptionHandler is not null)
        {
            FireAndForget.DefaultExceptionHandler(ex);
            return;
        }

        // 3) Never swallow silently: surface it with the originating call site.
        Trace.TraceError(
            $"[SafeFireAndForget] Unhandled exception from fire-and-forget started at " +
            $"{caller}:{Environment.NewLine}{ex}");
    }
}
