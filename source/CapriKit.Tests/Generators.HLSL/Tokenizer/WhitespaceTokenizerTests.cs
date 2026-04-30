using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Tokenizer;

internal class WhitespaceTokenizerTests
{
    [Test]
    public async Task ParseWhitespaceUntilNonWhitespace()
    {
        var input = " \t\n a";
        var list = new List<Token>(1);
        var consumed = WhitespaceTokenizer.ReadWhitespace(input, 0, list);
        await Assert.That(consumed).IsEqualTo(4);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(" \t\n ");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Whitespace);
    }
}
