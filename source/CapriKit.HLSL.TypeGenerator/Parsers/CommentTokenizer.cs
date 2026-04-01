using static System.Net.WebRequestMethods;

namespace CapriKit.HLSL.TypeGenerator.Parsers;

public static class CommentTokenizer
{
    public static int ReadBlockComment(string source, int offset, List<Token> tokens)
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

            tokens.Add(new Token(source, offset, advanced, TokenKind.BlockComment));
            return advanced;
        }

        return 0;
    }


    public static int ReadLineComment(string source, int offset, List<Token> tokens)
    {
        if ((offset + 1) < source.Length && source[offset] == '/' && source[offset + 1] == '/')
        {
            var advanced = AdvancePastEndOfLine(source, offset);
            tokens.Add(new Token(source, offset, advanced, TokenKind.LineComment));
            return advanced;
        }

        return 0;
    }

    /// <summary>
    /// Parses HLSL Preprocessor directive. For example #pragma
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-preprocessor"/>
    public static int ReadDirective(string source, int offset, List<Token> tokens)
    {
        if (source.Length < offset && source[offset] == '#')
        {
            var advanced = AdvancePastEndOfLine(source, offset);
            var token = new Token(source, offset, advanced, TokenKind.Directive);
            tokens.Add(token);

            return advanced;
        }

        return 0;
    }

    /// <summary>
    /// Consumes all characters remaining on this line. If this is not the last line in the file
    /// it will also consume the \n character itself.
    /// </summary>
    private static int AdvancePastEndOfLine(string source, int offset)
    {
        var advanced = 0;
        var cursor = offset + advanced;
        while (cursor < source.Length && source[cursor] != '\n')
        {
            advanced++;
            cursor = offset + advanced;
        }

        if (cursor + 1 < source.Length)
        {
            advanced++;
        }

        return advanced;
    }
}
