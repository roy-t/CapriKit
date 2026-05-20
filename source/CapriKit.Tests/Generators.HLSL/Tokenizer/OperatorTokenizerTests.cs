using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Tokenizer;

internal class OperatorTokenizerTests
{
    [Test]
    public async Task ParseMultiCharacterOperatorGreedily()
    {
        var input = ">>=";
        var list = new List<Token>(1);
        var consumed = OperatorTokenizer.ReadOperator(input, 0, list);
        await Assert.That(consumed).IsEqualTo(3);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(input);
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Operator);
    }

    [Test]
    public async Task ParseSingleCharacterOperator()
    {
        // In HLSL any single character that is unmatched by any other rule is an operator
        // so it doesn't really matter what we use as input here.
        var input = "-1234";
        var list = new List<Token>(1);
        var consumed = OperatorTokenizer.ReadOperator(input, 0, list);
        await Assert.That(consumed).IsEqualTo(1);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("-");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Operator);
    }
}
