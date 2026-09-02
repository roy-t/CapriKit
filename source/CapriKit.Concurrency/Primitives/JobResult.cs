using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace CapriKit.Concurrency.Primitives;

public sealed class JobResult<T>
{
    private readonly string Id;
    private readonly T? Result;
    private readonly ExceptionDispatchInfo? Exception;

    private JobResult(string Id, T? Result, ExceptionDispatchInfo? Exception)
    {
        Debug.Assert(Result == null ^ Exception == null);

        this.Id = Id;
        this.Result = Result;
        this.Exception = Exception;
    }

    public static JobResult<T> Failure(string Id, ExceptionDispatchInfo exception)
    {
        return new JobResult<T>(Id, default, exception);
    }

    public static JobResult<T> Success(string Id, T result)
    {
        return new JobResult<T>(Id, result, default);
    }

    /// <summary>
    /// True when the job produced a result, false when it failed. Use <see cref="Match"/> to get at either
    /// payload, this only answers which one is there.
    /// </summary>
    public bool IsSuccess => Exception is null;

    public void Match(Action<string, T> onSuccess, Action<string, ExceptionDispatchInfo> onFailure)
    {
        if (Result != null)
        {
            onSuccess(Id, Result);
        }

        if (Exception != null)
        {
            onFailure(Id, Exception);
        }
    }

    public T GetOrThrow()
    {
        Exception?.Throw();
        return Result!;
    }
}
