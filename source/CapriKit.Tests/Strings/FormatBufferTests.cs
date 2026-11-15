using System.Globalization;
using CapriKit.Strings;

namespace CapriKit.Tests.Strings;

internal class FormatBufferTests
{
    [Test]
    public async Task Write()
    {
        var buffer = new FormatBuffer();
        var name = "frame";
        var number = 7;

        // Integers with a D format are culture independent, unlike floating point numbers
        var result = buffer.Format($"{name} {number:D3} of {42}").ToString();

        await Assert.That(result).IsEqualTo("frame 007 of 42");
    }

    [Test]
    public async Task Write_GrowsBeyondInitialCapacity()
    {
        var buffer = new FormatBuffer(capacity: 4);

        // Both a literal and a formatted value overflow the buffer, forcing it to grow several times
        var result = buffer.Format($"A string that is longer than four characters {123456789}").ToString();

        await Assert.That(result).IsEqualTo("A string that is longer than four characters 123456789");
    }

    [Test]
    public async Task Write_Alignment()
    {
        var buffer = new FormatBuffer();
        var name = "ab";

        var result = buffer.Format($"[{42,5}][{42,-5}][{name,4}][{name:X}][{7,4:D2}][{123456,3}]").ToString();

        await Assert.That(result).IsEqualTo("[   42][42   ][  ab][ab][  07][123456]");
    }

    [Test]
    public async Task Write_AlignmentGrowsBuffer()
    {
        var buffer = new FormatBuffer(capacity: 4);

        // The padding, not the value, overflows the buffer
        var result = buffer.Format($"{1,10}").ToString();

        await Assert.That(result).IsEqualTo("         1");
    }

    [Test]
    public async Task Write_FormatProvider()
    {
        var invariant = new FormatBuffer(provider: CultureInfo.InvariantCulture);
        var dutch = new FormatBuffer(provider: CultureInfo.GetCultureInfo("nl-NL"));
        var value = 1234.5;

        var invariantResult = invariant.Format($"{value:N1}").ToString();
        var dutchResult = dutch.Format($"{value:N1}").ToString();

        await Assert.That(invariantResult).IsEqualTo("1,234.5");
        await Assert.That(dutchResult).IsEqualTo("1.234,5");
    }

    [Test]
    public async Task Write_OverwritesPreviousText()
    {
        var buffer = new FormatBuffer();
        buffer.Format($"A long first text {1}");

        var result = buffer.Format($"Short {2}").ToString();

        await Assert.That(result).IsEqualTo("Short 2");
        await Assert.That(buffer.ToString()).IsEqualTo("Short 2");
    }
}
