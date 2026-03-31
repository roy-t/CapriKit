using CapriKit.HLSL.TypeGenerator.Parsers;

namespace CapriKit.Tests.HLSL.TypeGenerator;

internal class NumberTokenizerTests
{

    [Test]
    public async Task ParseDigit()
    {
        var input = "1";
        var list = new List<Token>(1);
        var consumed = NumberTokenizer.ReadNumber(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("1");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Number);
    }

    [Test]
    public async Task ParseInteger()
    {
        var input = "123";
        var list = new List<Token>(1);
        var consumed = NumberTokenizer.ReadNumber(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("123");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Number);
    }

    [Test]
    public async Task ParseIntegerWithExponent()
    {
        var input = "123e4";
        var list = new List<Token>(1);
        var consumed = NumberTokenizer.ReadNumber(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("123e4");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Number);
    }

    [Test]
    public async Task ParseIntegerWithSignedExponent()
    {
        var input = "123e-4";
        var list = new List<Token>(1);
        var consumed = NumberTokenizer.ReadNumber(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("123e-4");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Number);
    }


    [Test]
    public async Task ParseIntegerWithSignedExponentAndSuffix()
    {
        var input = "123e-4L";
        var list = new List<Token>(2);
        var consumed = NumberTokenizer.ReadNumber(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(2);
        await Assert.That(list[0].Value).IsEqualTo("123e-4");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Number);

        await Assert.That(list[1].Value).IsEqualTo("L");
        await Assert.That(list[1].Kind).IsEqualTo(TokenKind.NumberSuffix);
    }

    [Test]
    public async Task ParseFractional()
    {
        var input = ".123";
        var list = new List<Token>(1);
        var consumed = NumberTokenizer.ReadNumber(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(".123");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Number);
    }

    [Test]
    public async Task ParseFractionalWithExponent()
    {
        var input = ".123e4";
        var list = new List<Token>(1);
        var consumed = NumberTokenizer.ReadNumber(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(".123e4");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Number);
    }

    [Test]
    public async Task ParseFractionalWithSignedExponent()
    {
        var input = ".123e+4";
        var list = new List<Token>(1);
        var consumed = NumberTokenizer.ReadNumber(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(".123e+4");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Number);
    }

    [Test]
    public async Task ParseFractionalWithSignedExponentAndSuffix()
    {
        var input = ".123e+4f";
        var list = new List<Token>(1);
        var consumed = NumberTokenizer.ReadNumber(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(2);
        await Assert.That(list[0].Value).IsEqualTo(".123e+4");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Number);

        await Assert.That(list[1].Value).IsEqualTo("f");
        await Assert.That(list[1].Kind).IsEqualTo(TokenKind.NumberSuffix);
    }

    [Test]
    public async Task ParseFractionalWithPreceedingDigitsSignedExponentAndSuffix()
    {
        var input = "0.123e+4f";
        var list = new List<Token>(2);
        var consumed = NumberTokenizer.ReadNumber(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(2);
        await Assert.That(list[0].Value).IsEqualTo("0.123e+4");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Number);

        await Assert.That(list[1].Value).IsEqualTo("f");
        await Assert.That(list[1].Kind).IsEqualTo(TokenKind.NumberSuffix);
    }

    [Test]
    public async Task DoNotParseNumberWithNoDigitsBeforeExponent()
    {
        var input = "E4f";
        var list = new List<Token>(0);
        var consumed = NumberTokenizer.ReadNumber(input, 0, list);
        await Assert.That(consumed).IsEqualTo(0);
        await Assert.That(list).Count().IsEqualTo(0);
    }

    [Test]
    public async Task DoNotConsumeMultipleDecimalPoints()
    {
        var input = "1.0.0f";
        var list = new List<Token>(1);
        var consumed = NumberTokenizer.ReadNumber(input, 0, list);
        await Assert.That(consumed).IsEqualTo(3);
        await Assert.That(list).Count().IsEqualTo(1);

        await Assert.That(list[0].Value).IsEqualTo("1.0");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Number);
    }
}
