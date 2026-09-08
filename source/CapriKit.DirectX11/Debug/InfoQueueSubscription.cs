using Microsoft.Extensions.Logging;
using System.Runtime.ExceptionServices;
using Vortice.DXGI.Debug;
using static Vortice.DXGI.DXGI;

namespace CapriKit.DirectX11.Debug;

/// <summary>
/// Drains the IDXGIInfoQueue and reports every message it finds to the logger, and throws Exceptions for
/// messages of severity of warning or higher, every time <seealso cref="LogMessages()"/> is called.
/// </summary>
internal sealed partial class InfoQueueSubscription
{
    private readonly ILogger<InfoQueueSubscription> Logger;
    private readonly IDXGIInfoQueue MessageQueue;

    public InfoQueueSubscription(ILoggerFactory loggerFactory, IDXGIInfoQueue messageQueue)
    {
        Logger = loggerFactory.CreateLogger<InfoQueueSubscription>();
        MessageQueue = messageQueue;

        // Ensure we get to read all exception of the DirectX debug device before the application closes
        // due to an (unrelated) first chance exception.
        AppDomain.CurrentDomain.FirstChanceException += LogMessages;
    }

    public void LogMessages()
    {
        // Prevents errors if a first chance exception happens after the message queue was disposed
        if (MessageQueue.NativePointer == nint.Zero) { return; }

        List<Exception>? exceptions = null;
        var count = MessageQueue.GetNumStoredMessages(DebugAll);
        for (var i = 0ul; i < count; i++)
        {
            var message = MessageQueue.GetMessage(DebugAll, i);
            var severity = ToLogLevel(message.Severity);
            if (Logger.IsEnabled(severity))
            {
                var description = NullTerminatedStringToDotNetString(message.Description);
                LogInfoQueueMessage(Logger, severity, message.Category, message.Id, description);
            }

            if (IsExceptional(message.Severity))
            {
                var description = NullTerminatedStringToDotNetString(message.Description);
                var text = $"[{message.Id}:{message.Category}] {description}";
                (exceptions = exceptions ?? []).Add(new Exception(text));
            }
        }

        MessageQueue.ClearStoredMessages(DebugAll);
        if (exceptions != null && exceptions.Count == 1) { throw exceptions[0]; }
        if (exceptions != null && exceptions.Count >= 2) { throw new AggregateException(exceptions); }
    }

    private void LogMessages(object? _, FirstChanceExceptionEventArgs? e)
    {
        LogMessages();
    }

    private static bool IsExceptional(InfoQueueMessageSeverity severity)
    {
        return severity == InfoQueueMessageSeverity.Corruption || severity == InfoQueueMessageSeverity.Error || severity == InfoQueueMessageSeverity.Warning;
    }

    private static LogLevel ToLogLevel(InfoQueueMessageSeverity severity) => severity switch
    {
        InfoQueueMessageSeverity.Corruption => LogLevel.Critical,
        InfoQueueMessageSeverity.Error => LogLevel.Error,
        InfoQueueMessageSeverity.Warning => LogLevel.Warning,
        InfoQueueMessageSeverity.Info => LogLevel.Information,
        _ => LogLevel.Trace,
    };

    private static string NullTerminatedStringToDotNetString(ReadOnlySpan<char> message)
    {
        if (message.Length == 0) { return string.Empty; }
        if (message[^1] == '\0') { return new string(message[0..^1]); }
        return new string(message);
    }

    [LoggerMessage(Message = "[{category}:{id}] {description}")]
    private static partial void LogInfoQueueMessage(ILogger logger, LogLevel level, InfoQueueMessageCategory category, int id, string description);
}
