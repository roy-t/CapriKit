using System.Diagnostics;

namespace CapriKit.Generators.HLSL.Tokenizer;

public sealed class Trie
{
    private class Node
    {
        public Dictionary<char, Node> Leafs { get; } = [];
        public TokenKind Result { get; set; } = TokenKind.Unknown;
    }

    private readonly Node Root;

    public Trie()
    {
        Root = new Node();
    }

    public int ReadToken(string source, int offset, List<Token> tokens)
    {
        var node = Root;
        var cursor = offset;
        while (cursor < source.Length && node.Leafs.TryGetValue(source[cursor], out var leaf))
        {
            node = leaf;
            cursor++;
        }

        if (node.Result != TokenKind.Unknown)
        {
            var length = cursor - offset;
            tokens.Add(new Token(source, offset, length, node.Result));
            return length;
        }

        return 0;
    }

    public void AddString(string value, TokenKind result)
    {
        var leaf = AddString(value);
        Debug.Assert(leaf.Result != result, "You should not add a path to the Trie twice");
        Debug.Assert(leaf.Result == TokenKind.Unknown, "You should not change the result of an existing path in the Trie");
        leaf.Result = result;
    }

    public void AddSubTrie(string value, Trie subTrie)
    {
        var leaf = AddString(value);
        foreach (var kv in subTrie.Root.Leafs)
        {
            leaf.Leafs.Add(kv.Key, kv.Value);
        }
    }

    private Node AddString(string value)
    {
        var node = Root;
        foreach (var c in value)
        {
            if (!node.Leafs.TryGetValue(c, out var leaf))
            {
                leaf = new Node();
                node.Leafs.Add(c, leaf);
            }

            node = leaf;
        }

        return node;
    }
}
