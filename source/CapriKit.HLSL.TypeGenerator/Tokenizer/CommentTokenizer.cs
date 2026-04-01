using CapriKit.HLSL.TypeGenerator.Parsers;
using static CapriKit.HLSL.TypeGenerator.Tokenizer.TokenizerUtils;

namespace CapriKit.HLSL.TypeGenerator.Tokenizer;

public static class CommentTokenizer
{
    /// <summary>
    /// Reads HLSL line (//) and block (/* */)comments
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#whitespace"/>
    public static int ReadComment(string source, int offset, List<Token> tokens)
    {
        var read = ReadBlockComment(source, offset, tokens);
        if (read == 0)
        {
            read = ReadLineComment(source, offset, tokens);
        }
        return read;
    }

    private static int ReadBlockComment(string source, int offset, List<Token> tokens)
    {
        if ((offset + 1) < source.Length && source[offset] == '/' && source[offset + 1] == '*')
        {
            var advanced = 2;
            var cursor = offset + advanced;

            while (cursor < source.Length && !(source[cursor - 1] == '*' && source[cursor] == '/'))
            {
                advanced++;
                cursor = offset + advanced;
            }

            if (cursor + 1 < source.Length)
            {
                advanced++;
            }

            tokens.Add(new Token(source, offset, advanced, TokenKind.Comment));
            return advanced;
        }

        return 0;
    }


    private static int ReadLineComment(string source, int offset, List<Token> tokens)
    {
        if ((offset + 1) < source.Length && source[offset] == '/' && source[offset + 1] == '/')
        {
            var advanced = AdvancePastEndOfLine(source, offset);
            tokens.Add(new Token(source, offset, advanced, TokenKind.Comment));
            return advanced;
        }

        return 0;
    }
}
