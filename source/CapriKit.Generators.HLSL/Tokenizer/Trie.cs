using System.Diagnostics;

namespace CapriKit.Generators.HLSL.Tokenizer;

internal sealed class Trie
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

    /// <summary>
    /// Tries to find the result for the given substring in the trie
    /// </summary>
    public bool TryParse(string source, int offset, int length, out Token token)
    {
        var node = Root;
        var read = 0;
        while (read < length && node.Leafs.TryGetValue(source[read + offset], out var leaf))
        {
            node = leaf;
            read++;
        }

        if (node.Result != TokenKind.Unknown && length == read)
        {
            token = new Token(source, offset, length, node.Result);
            return true;
        }

        token = default;
        return false;
    }

    public void AddString(string value, TokenKind result)
    {
        var leaf = AddString(value);
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
