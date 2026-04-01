using CapriKit.HLSL.TypeGenerator.Parsers;

namespace CapriKit.HLSL.TypeGenerator.Tokenizer;

public static class ReservedTokenizer
{
    private static readonly Dictionary<string, TokenKind> Reserved = new()
    {
        ["auto"] = TokenKind.Reserved,
        ["case"] = TokenKind.Reserved,
        ["catch"] = TokenKind.Reserved,
        ["char"] = TokenKind.Reserved,
        ["class"] = TokenKind.Reserved,
        ["const_cast"] = TokenKind.Reserved,
        ["default"] = TokenKind.Reserved,
        ["delete"] = TokenKind.Reserved,
        ["dynamic_cast"] = TokenKind.Reserved,
        ["enum"] = TokenKind.Reserved,
        ["explicit"] = TokenKind.Reserved,
        ["friend"] = TokenKind.Reserved,
        ["goto"] = TokenKind.Reserved,
        ["long"] = TokenKind.Reserved,
        ["mutable"] = TokenKind.Reserved,
        ["new"] = TokenKind.Reserved,
        ["operator"] = TokenKind.Reserved,
        ["private"] = TokenKind.Reserved,
        ["protected"] = TokenKind.Reserved,
        ["public"] = TokenKind.Reserved,
        ["reinterpret_cast"] = TokenKind.Reserved,
        ["short"] = TokenKind.Reserved,
        ["signed"] = TokenKind.Reserved,
        ["sizeof"] = TokenKind.Reserved,
        ["static_cast"] = TokenKind.Reserved,
        ["template"] = TokenKind.Reserved,
        ["this"] = TokenKind.Reserved,
        ["throw"] = TokenKind.Reserved,
        ["try"] = TokenKind.Reserved,
        ["typename"] = TokenKind.Reserved,
        ["union"] = TokenKind.Reserved,
        ["unsigned"] = TokenKind.Reserved,
        ["using"] = TokenKind.Reserved,
        ["virtual"] = TokenKind.Reserved,
    };

    /// <summary>
    /// Adds rules for reserved words to the trie.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-reserved-words"/>
    public static void AddRulesToTrie(Trie trie)
    {
        foreach (var kv in Reserved)
        {
            trie.AddString(kv.Key, kv.Value);
        }
    }
}
