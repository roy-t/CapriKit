using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Tokenizer;

internal class ReservedTokenizerTests
{
    [Test]
    public async Task ParseReserved()
    {
        var input = "goto label;";
        var result = ReservedTokenizer.TryParse(input, 0, 4, out var token);
        await Assert.That(result).IsTrue();
        await Assert.That(token.Value).IsEqualTo("goto");
        await Assert.That(token.Kind).IsEqualTo(TokenKind.Reserved);
    }

    [Test]
    public async Task DoNotParseIdentifierWithReservedPrefix()
    {
        var input = "automatic";
        var result = ReservedTokenizer.TryParse(input, 0, 9, out var token);
        await Assert.That(result).IsFalse();
    }
}
