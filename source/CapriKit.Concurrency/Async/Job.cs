namespace CapriKit.Concurrency.Async;

public record Job<T>(string Id, Func<CancellationToken, Task<T>> Callback);
