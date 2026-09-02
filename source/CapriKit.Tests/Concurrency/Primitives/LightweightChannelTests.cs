using CapriKit.Concurrency.Primitives;
using System.Runtime.ExceptionServices;

namespace CapriKit.Tests.Concurrency.Primitives;

internal class LightweightChannelTests
{
    [Test]
    public async Task TryRead()
    {
        var channel = new LightweightChannel<int>();
        channel.Write(1);
        channel.Write(2);

        var read1 = channel.TryRead(out var first);
        var read2 = channel.TryRead(out var second);

        await Assert.That(read1).IsTrue();
        await Assert.That(first).IsEqualTo(1);
        await Assert.That(read2).IsTrue();
        await Assert.That(second).IsEqualTo(2);
    }

    [Test]
    public async Task TryRead_ReturnsFalseWhenChannelIsEmpty()
    {
        var channel = new LightweightChannel<int>();

        var read = channel.TryRead(out var value);

        await Assert.That(read).IsFalse();
        await Assert.That(value).IsEqualTo(0);
    }

    [Test]
    public async Task TryRead_RethrowsTheWrittenExceptionBeforeReturningOtherItems()
    {
        var channel = new LightweightChannel<int>();
        channel.Write(4);
        channel.Write(ExceptionDispatchInfo.Capture(new InvalidOperationException()));

        await Assert.That(() => channel.TryRead(out _)).Throws<InvalidOperationException>();

        var read = channel.TryRead(out var value);
        await Assert.That(read).IsTrue();
        await Assert.That(value).IsEqualTo(4);
    }
}
