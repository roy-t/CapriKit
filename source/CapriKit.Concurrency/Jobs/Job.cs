namespace CapriKit.Concurrency.Jobs;

public record Job<T>(string Id, Func<CancellationToken, Task<T>> Callback);
