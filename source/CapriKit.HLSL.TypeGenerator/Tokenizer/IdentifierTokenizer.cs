using CapriKit.HLSL.TypeGenerator.Parsers;
using static CapriKit.HLSL.TypeGenerator.Tokenizer.TokenizerUtils;

namespace CapriKit.HLSL.TypeGenerator.Tokenizer;

public static class IdentifierTokenizer
{
    /// <summary>
    /// Reads identifiers.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#identifiers"/>
    public static int ReadIdentifier(string source, int offset, List<Token> tokens)
    {
        var length = MatchIdentifier(source, offset);
        if (length > 0)
        {
            tokens.Add(new Token(source, offset, length, TokenKind.Identifier));
        }

        return length;
    }

    /// <summary>
    /// Returns the length of the next identifier, or zero if the next characters do not represent an identifier.
    /// </summary>
    public static int MatchIdentifier(string source, int offset)
    {
        var cursor = offset;
        if (TryPeek(source, cursor, out var head) && IsValidHead(head))
        {
            cursor++;
            while (TryPeek(source, cursor, out var body) && IsValidBody(body))
            {
                cursor++;
            }
        }

        return cursor - offset;
    }

    private static bool IsValidHead(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    private static bool IsValidBody(char c)
    {
        return char.IsLetter(c) || char.IsDigit(c) || c == '_';
    }
}
