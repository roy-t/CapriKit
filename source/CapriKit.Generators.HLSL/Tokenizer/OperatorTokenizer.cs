namespace CapriKit.Generators.HLSL.Tokenizer;

public static class OperatorTokenizer
{
    private const int LongestOperator = 3;
    private static readonly Dictionary<string, TokenKind> Operators = new()
    {
        ["##"] = TokenKind.Operator,
        ["#@"] = TokenKind.Operator,
        ["++"] = TokenKind.Operator,
        ["--"] = TokenKind.Operator,
        ["&&"] = TokenKind.Operator,
        ["||"] = TokenKind.Operator,
        ["=="] = TokenKind.Operator,
        ["::"] = TokenKind.Operator,
        ["<<"] = TokenKind.Operator,
        ["<<="] = TokenKind.Operator,
        [">>"] = TokenKind.Operator,
        [">>="] = TokenKind.Operator,
        ["..."] = TokenKind.Operator,
        ["<="] = TokenKind.Operator,
        [">="] = TokenKind.Operator,
        ["!="] = TokenKind.Operator,
        ["*="] = TokenKind.Operator,
        ["/="] = TokenKind.Operator,
        ["+="] = TokenKind.Operator,
        ["-="] = TokenKind.Operator,
        ["%="] = TokenKind.Operator,
        ["&="] = TokenKind.Operator,
        ["|="] = TokenKind.Operator,
        ["^="] = TokenKind.Operator,
        ["->"] = TokenKind.Operator,
    };

    private static readonly Trie OperatorTrie;

    static OperatorTokenizer()
    {
        OperatorTrie = new Trie();
        foreach (var kv in Operators)
        {
            OperatorTrie.AddString(kv.Key, kv.Value);
        }
    }


    /// <summary>
    /// Parses operators like '>>='. Note: in HLSL any single character that did
    /// not match another rule is an operator. Which means this rule should always be checked last.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#operators"/>
    public static int ReadOperator(string source, int offset, List<Token> tokens)
    {
        if (offset >= source.Length)
        {
            return 0;
        }

        for (var length = LongestOperator; length > 1; length--)
        {
            if (OperatorTrie.TryParse(source, offset, length, out var token))
            {
                tokens.Add(token);
                return length;
            }
        }

        tokens.Add(new Token(source, offset, 1, TokenKind.Operator));
        return 1;
    }
}
