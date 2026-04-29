using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL;

internal class IdentifierTokenizerTests
{
    [Test]
    public async Task ParseIdentifier()
    {
        var input = "a3_b4";
        var list = new List<Token>(1);
        var consumed = IdentifierTokenizer.ReadIdentifier(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(input);
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Identifier);
    }

    [Test]
    public async Task DoNotParseIdentifierStartingWithDigit()
    {
        var input = "3-4";
        var list = new List<Token>(1);
        var consumed = IdentifierTokenizer.ReadIdentifier(input, 0, list);
        await Assert.That(consumed).IsEqualTo(0);
        await Assert.That(list).Count().IsEqualTo(0);
    }
}
