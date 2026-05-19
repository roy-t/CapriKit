using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Tokenizer;

internal class KeywordTokenizerTests
{
    [Test]
    public async Task ParseKeyword()
    {
        var trie = new Trie();
        KeywordTokenizer.AddRulesToTrie(trie);

        var input = "if;";
        var list = new List<Token>(1);
        var consumed = trie.ReadToken(input, 0, list);
        await Assert.That(consumed).IsEqualTo(2);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("if");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Keyword);
    }

    [Test]
    public async Task ParseExpandedKeyword()
    {
        var trie = new Trie();
        KeywordTokenizer.AddRulesToTrie(trie);

        var input = "float2x2 ";
        var list = new List<Token>(1);
        var consumed = trie.ReadToken(input, 0, list);
        await Assert.That(consumed).IsEqualTo(8);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo("float2x2");
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Keyword);
    }

    [Test]
    public async Task DoNotExpandNonExpandableKeyword()
    {
        var trie = new Trie();
        KeywordTokenizer.AddRulesToTrie(trie);

        // void does not support expansion, so "void2x2" is a single identifier
        // and must not be tokenized as the keyword "void".
        var input = "void2x2";
        var list = new List<Token>(1);
        var consumed = trie.ReadToken(input, 0, list);
        await Assert.That(consumed).IsEqualTo(0);
        await Assert.That(list).Count().IsEqualTo(0);
    }

    [Test]
    public async Task DoNotParseTextWithKeywordPrefix()
    {
        var trie = new Trie();
        KeywordTokenizer.AddRulesToTrie(trie);

        // for is a keyword
        var input = "fortune";
        var list = new List<Token>(1);
        var consumed = trie.ReadToken(input, 0, list);
        await Assert.That(consumed).IsEqualTo(0);
        await Assert.That(list).Count().IsEqualTo(0);
    }
}
