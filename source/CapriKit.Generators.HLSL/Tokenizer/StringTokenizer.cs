using static CapriKit.Generators.HLSL.Tokenizer.TokenizerUtils;

namespace CapriKit.Generators.HLSL.Tokenizer;

public static class StringTokenizer
{
    private const char Delimiter = '\"';

    /// <summary>
    /// Reads string literals.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#strings"/>
    public static int ReadString(string source, int offset, List<Token> tokens)
    {
        var cursor = offset;
        if (TryPeek(source, cursor, out var start) && start == Delimiter)
        {
            cursor++;
            while (TryPeek(source, cursor, out var c) && c != Delimiter)
            {
                // skip escaped characters so that we don't stop at \"
                if (c == '\\')
                {
                    cursor++;
                }
                cursor++;
            }

            if (TryPeek(source, cursor, out var end) && end == Delimiter)
            {
                cursor++;
                var length = cursor - offset;
                tokens.Add(new Token(source, offset, length, TokenKind.String));
                return length;
            }
        }

        return 0;
    }
}
