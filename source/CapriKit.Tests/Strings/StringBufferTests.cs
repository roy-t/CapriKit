using System.Globalization;
using CapriKit.Strings;

namespace CapriKit.Tests.Strings;

internal class StringBufferTests
{
    [Test]
    public async Task Write()
    {
        var buffer = new StringBuffer();
        var name = "frame";
        var number = 7;

        // Integers with a D format are culture independent, unlike floating point numbers
        var result = buffer.Write($"{name} {number:D3} of {42}").ToString();

        await Assert.That(result).IsEqualTo("frame 007 of 42");
    }

    [Test]
    public async Task Write_GrowsBeyondInitialCapacity()
    {
        var buffer = new StringBuffer(capacity: 4);

        // Both a literal and a formatted value overflow the buffer, forcing it to grow several times
        var result = buffer.Write($"A string that is longer than four characters {123456789}").ToString();

        await Assert.That(result).IsEqualTo("A string that is longer than four characters 123456789");
    }

    [Test]
    public async Task Write_Alignment()
    {
        var buffer = new StringBuffer();
        var name = "ab";

        var result = buffer.Write($"[{42,5}][{42,-5}][{name,4}][{name:X}][{7,4:D2}][{123456,3}]").ToString();

        await Assert.That(result).IsEqualTo("[   42][42   ][  ab][ab][  07][123456]");
    }

    [Test]
    public async Task Write_AlignmentGrowsBuffer()
    {
        var buffer = new StringBuffer(capacity: 4);

        // The padding, not the value, overflows the buffer
        var result = buffer.Write($"{1,10}").ToString();

        await Assert.That(result).IsEqualTo("         1");
    }

    [Test]
    public async Task Write_FormatProvider()
    {
        var invariant = new StringBuffer(provider: CultureInfo.InvariantCulture);
        var dutch = new StringBuffer(provider: CultureInfo.GetCultureInfo("nl-NL"));
        var value = 1234.5;

        var invariantResult = invariant.Write($"{value:N1}").ToString();
        var dutchResult = dutch.Write($"{value:N1}").ToString();

        await Assert.That(invariantResult).IsEqualTo("1,234.5");
        await Assert.That(dutchResult).IsEqualTo("1.234,5");
    }

    [Test]
    public async Task Write_OverwritesPreviousText()
    {
        var buffer = new StringBuffer();
        buffer.Write($"A long first text {1}");

        var result = buffer.Write($"Short {2}").ToString();

        await Assert.That(result).IsEqualTo("Short 2");
        await Assert.That(buffer.ToString()).IsEqualTo("Short 2");
    }
}
