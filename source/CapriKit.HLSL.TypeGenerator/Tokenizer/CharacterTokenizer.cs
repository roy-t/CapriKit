using static CapriKit.HLSL.TypeGenerator.Tokenizer.TokenizerUtils;

namespace CapriKit.HLSL.TypeGenerator.Tokenizer;

public static class CharacterTokenizer
{
    /// <summary>
    /// Reads character literals.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#characters"/>
    public static int ReadCharacter(string source, int offset, List<Token> tokens)
    {
        // TODO: this method accepts any string of characters as a character literal
        // while only escape sequences like '\n' '\101' (octal for 'A') and '\x51' (hex for 'A').
        // can lead to character literals that span more than 1 character.

        // TODO: this code is equivalent to strings right now
        var cursor = offset;
        if (TryPeek(source, cursor, out var start) && start == '\'')
        {
            
            cursor++;
            while (TryPeek(source, cursor, out var c) && c != '\"')
            {
                // skip escaped characters so that we don't stop at \"
                if (c == '\\')
                {
                    cursor++;
                }
                cursor++;
            }

            if (TryPeek(source, cursor, out var end) && end == '\'')
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
