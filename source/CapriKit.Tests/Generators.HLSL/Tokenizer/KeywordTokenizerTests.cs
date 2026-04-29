using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Tokenizer;

internal class KeywordTokenizerTests
{
    [Test]
    public async Task ParseKeyword()
    {
        var trie = new Trie();
        KeywordTokenizer.AddRulesToTrie(trie);

        var input = "if";
        var list = new List<Token>(1);
        var consumed = trie.ReadToken(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(input);
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Keyword);
    }

    [Test]
    public async Task ParseExpandedKeyword()
    {
        var trie = new Trie();
        KeywordTokenizer.AddRulesToTrie(trie);

        var input = "float2x2";
        var list = new List<Token>(1);
        var consumed = trie.ReadToken(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(input);
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Keyword);
    }

    [Test]
    public async Task DoNotExpandNonExpandableKeyword()
    {
        var trie = new Trie();
        KeywordTokenizer.AddRulesToTrie(trie);

        var input = "void2x2";
        var list = new List<Token>(1);
        var consumed = trie.ReadToken(input, 0, list);
        await Assert.That(consumed).IsEqualTo("void".Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("void");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Keyword);
    }
}
