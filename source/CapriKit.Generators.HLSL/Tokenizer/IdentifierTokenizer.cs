using static CapriKit.Generators.HLSL.Tokenizer.TokenizerUtils;

namespace CapriKit.Generators.HLSL.Tokenizer;

internal static class IdentifierTokenizer
{
    /// <summary>
    /// Reads identifiers and tokenizes them as identifiers, reserved words or keywords
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#identifiers"/>
    public static int ReadIdentifier(string source, int offset, List<Token> tokens)
    {
        var length = MatchIdentifier(source, offset);
        if (length > 0)
        {
            // An identifier might be a keyword or reserved word.
            if (KeywordTokenizer.TryParse(source, offset, length, out var keyword))
            {
                tokens.Add(keyword);
            }
            else if (ReservedTokenizer.TryParse(source, offset, length, out var reserved))
            {
                tokens.Add(reserved);
            }
            else
            {
                tokens.Add(new Token(source, offset, length, TokenKind.Identifier));
            }
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
