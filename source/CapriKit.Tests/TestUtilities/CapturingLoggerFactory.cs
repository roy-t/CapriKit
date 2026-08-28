using Microsoft.Extensions.Logging;

namespace CapriKit.Tests.TestUtilities;

/// <summary>
/// Logger factory that keeps every formatted message, for tests that assert on a diagnostic instead of on
/// a return value. Messages may be written from any thread, reading them is meant for the main thread.
/// </summary>
internal sealed class CapturingLoggerFactory : ILoggerFactory
{
    private readonly List<string> messages = [];

    public IReadOnlyList<string> Messages
    {
        get { lock (messages) { return [.. messages]; } }
    }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(messages);

    public void AddProvider(ILoggerProvider provider) { }

    public void Dispose() { }

    private sealed class CapturingLogger(List<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (messages) { messages.Add(formatter(state, exception)); }
        }
    }
}
