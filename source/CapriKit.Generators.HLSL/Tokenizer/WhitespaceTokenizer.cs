using static CapriKit.Generators.HLSL.Tokenizer.TokenizerUtils;

namespace CapriKit.Generators.HLSL.Tokenizer;

public static class WhitespaceTokenizer
{
    /// <summary>
    /// Reads whitespace tokens
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#whitespace"/>
    public static int ReadWhitespace(string source, int offset, List<Token> tokens)
    {
        var cursor = offset;
        while (TryPeek(source, cursor, out var c) && char.IsWhiteSpace(c))
        {
            cursor++;
        }

        var length = cursor - offset;
        if (length > 0)
        {
            tokens.Add(new Token(source, offset, length, TokenKind.Whitespace));
        }

        return length;
    }
}
