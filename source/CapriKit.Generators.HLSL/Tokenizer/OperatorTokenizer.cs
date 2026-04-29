using static CapriKit.Generators.HLSL.Tokenizer.TokenizerUtils;

namespace CapriKit.Generators.HLSL.Tokenizer;

public static class OperatorTokenizer
{
    private static readonly Dictionary<string, TokenKind> Operators = new()
    {
        ["##"] = TokenKind.Operator,
        ["#@"] = TokenKind.Operator,
        ["++"] = TokenKind.Operator,
        ["--"] = TokenKind.Operator,
        ["&"] = TokenKind.Operator,
        ["&"] = TokenKind.Operator,
        ["&"] = TokenKind.Operator,
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

    /// <summary>
    /// Adds rules for operators to the trie.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#operators"/>
    public static void AddRulesToTrie(Trie trie)
    {
        foreach (var kv in Operators)
        {
            trie.AddString(kv.Key, kv.Value);
        }
    }

    /// <summary>
    /// In HLSL any other single character that did not match another rule is an operator.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#operators"/>
    public static int ReadSingleCharacterOperator(string source, int offset, List<Token> tokens)
    {
        var cursor = offset;
        if (TryPeek(source, cursor, out var c))
        {
            tokens.Add(new Token(source, offset, 1, TokenKind.Operator));
            return 1;
        }

        return 0;
    }
}
