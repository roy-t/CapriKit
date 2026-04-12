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
        var cursor = offset;
        if (TryPeek(source, cursor, out var start) && start == '\'')
        {
            cursor++;
            while (TryPeek(source, cursor, out var c) && (c == '\\' || char.IsLetterOrDigit(c)))
            {
                cursor++;
            }

            if (TryPeek(source, cursor, out var end) && end == '\'')
            {
                cursor++;
                var length = cursor - offset;
                tokens.Add(new Token(source, offset, length, TokenKind.Character));
                return length;

            }
        }

        return 0;
    }
}
