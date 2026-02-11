using System.Runtime.ExceptionServices;
using Vortice.DXGI.Debug;
using static Vortice.DXGI.DXGI;

namespace CapriKit.DirectX11.Debug;

/// <summary>
/// Subscribes to message fom the IDXGIInfoQueue and throws Exceptions for messages that of a severity of warning or higher
/// every time the `CheckExceptions` method is called.
/// </summary>
internal sealed class InfoQueueSubscription
{
    private readonly IDXGIInfoQueue MessageQueue;

    public InfoQueueSubscription(IDXGIInfoQueue messageQueue)
    {
        MessageQueue = messageQueue;

        // Ensure we get to read all exception of the DirectX debug device before the application closes
        // due to an (unrelated) first chance exception.
        AppDomain.CurrentDomain.FirstChanceException += this.CheckExceptions;
    }

    public void CheckExceptions()
    {
        Exception? exception = null;
        var count = MessageQueue.GetNumStoredMessages(DebugAll);
        for (var i = 0ul; i < count; i++)
        {
            var message = MessageQueue.GetMessage(DebugAll, i);
            var description = $"[{message.Id}:{message.Category}] {UnterminateString(message.Description)}";

            if (exception == null)
            {
                exception = new Exception(description);
            }
            else if (exception is AggregateException aggregate)
            {
                exception = new AggregateException([.. aggregate.InnerExceptions, new Exception(description)]);
            }
        }

        MessageQueue.ClearStoredMessages(DebugAll);

        if (exception != null)
        {
            throw exception;
        }
    }

    private void CheckExceptions(object? _, FirstChanceExceptionEventArgs? e)
    {
        CheckExceptions();
    }

    private static ReadOnlySpan<char> UnterminateString(ReadOnlySpan<char> message)
    {
        if (message.Length > 0 && message[^1] == '\0')
        {
            return message[0..^1];
        }
        return message;
    }
}
