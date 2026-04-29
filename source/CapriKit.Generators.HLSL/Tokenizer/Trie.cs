namespace CapriKit.Generators.HLSL.Tokenizer;

public sealed class Trie
{
    private class Node
    {
        public Node()
        {
            Leafs = [];
            Result = TokenKind.Unknown;
        }

        public Dictionary<char, Node> Leafs { get; }
        public TokenKind Result { get; set; }
    }

    private readonly Node Root;

    public Trie()
    {
        Root = new Node();
    }

    public int ReadToken(string source, int offset, List<Token> tokens)
    {
        if (Read(source, offset, out var token))
        {
            tokens.Add(token);
            return token.Length;
        }

        return 0;
    }

    public bool Read(string source, int offset, out Token token)
    {
        var node = Root;
        for (var i = offset; i < source.Length; i++)
        {
            var c = source[i];
            if (node.Leafs.TryGetValue(c, out var leaf))
            {
                node = leaf;
            }
            else
            {
                var length = i - offset;
                token = new Token(source, offset, length, node.Result);
                return node.Result != TokenKind.Unknown;
            }
        }

        token = default;
        return false;
    }

    public void AddString(string value, TokenKind result)
    {
        var leaf = AddString(value);
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
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
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
