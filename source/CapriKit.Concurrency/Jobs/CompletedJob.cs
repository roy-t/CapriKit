using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace CapriKit.Concurrency.Jobs;

public sealed class CompletedJob<T>
{
    private readonly string Id;
    private readonly T? Result;
    private readonly ExceptionDispatchInfo? Exception;

    private CompletedJob(string Id, T? Result, ExceptionDispatchInfo? Exception)
    {
        Debug.Assert(Result == null ^ Exception == null);

        this.Id = Id;
        this.Result = Result;
        this.Exception = Exception;
    }

    public static CompletedJob<T> Failure(string Id, ExceptionDispatchInfo exception)
    {
        return new CompletedJob<T>(Id, default, exception);
    }

    public static CompletedJob<T> Success(string Id, T result)
    {
        return new CompletedJob<T>(Id, result, default);
    }

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
}
