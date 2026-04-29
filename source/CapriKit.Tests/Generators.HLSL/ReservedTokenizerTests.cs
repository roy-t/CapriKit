using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL;

internal class ReservedTokenizerTests
{
    [Test]
    public async Task ParseReserved()
    {
        var trie = new Trie();
        ReservedTokenizer.AddRulesToTrie(trie);

        var input = "goto";
        var list = new List<Token>(1);
        var consumed = trie.ReadToken(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(input);
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Reserved);
    }
}
