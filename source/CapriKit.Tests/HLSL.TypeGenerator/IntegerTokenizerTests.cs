
using CapriKit.HLSL.TypeGenerator.Tokenizer;

namespace CapriKit.Tests.HLSL.TypeGenerator;

internal class IntegerTokenizerTests
{

    [Test]
    public async Task ParseDigit()
    {
        var input = "1";
        var list = new List<Token>(1);
        var consumed = IntegerTokenizer.ReadInteger(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("1");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.IntegerLiteral);
    }

    [Test]
    public async Task ParseInteger()
    {
        var input = "123";
        var list = new List<Token>(1);
        var consumed = IntegerTokenizer.ReadInteger(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("123");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.IntegerLiteral);
    }

    [Test]
    public async Task ParseSignedInteger()
    {
        var input = "-123";
        var list = new List<Token>(1);
        var consumed = IntegerTokenizer.ReadInteger(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("-123");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.IntegerLiteral);
    }

    [Test]
    public async Task ParseOctal()
    {
        var input = "0733";
        var list = new List<Token>(1);
        var consumed = IntegerTokenizer.ReadInteger(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("0733");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.IntegerLiteral);
    }

    [Test]
    public async Task ParseHex()
    {
        var input = "0xAF";
        var list = new List<Token>(1);
        var consumed = IntegerTokenizer.ReadInteger(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("0xAF");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.IntegerLiteral);
    }


    [Test]
    public async Task ParseIntegerWithSuffix()
    {
        var input = "123L";
        var list = new List<Token>(2);
        var consumed = IntegerTokenizer.ReadInteger(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("123L");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.IntegerLiteral);
    }

    [Test]
    public async Task DoNotParseWithNoDigits()
    {
        var input = "E4f";
        var list = new List<Token>(0);
        var consumed = IntegerTokenizer.ReadInteger(input, 0, list);
        await Assert.That(consumed).IsEqualTo(0);
        await Assert.That(list).Count().IsEqualTo(0);
    }
}
