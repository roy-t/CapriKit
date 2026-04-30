using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Tokenizer;

internal class DirectiveTokenizerTests
{
    [Test]
    public async Task ParseDirective()
    {
        var input = "#pragma once";
        var list = new List<Token>(1);
        var consumed = DirectiveTokenizer.ReadDirective(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(input);
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Directive);
    }
}
