using static CapriKit.Generators.HLSL.Tokenizer.TokenizerUtils;

namespace CapriKit.Generators.HLSL.Tokenizer;

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
        var cursor = offset;
        if (TryPeek(source, cursor, out var first) && first == '/' &&
            TryPeek(source, cursor + 1, out var second) && second == '*')
        {
            cursor += 2;
            while (TryPeek(source, cursor, out var c))
            {
                if (c == '*' && TryPeek(source, cursor + 1, out var next) && next == '/')
                {
                    cursor += 2;
                    break;
                }
                cursor++;
            }

            var advanced = cursor - offset;
            tokens.Add(new Token(source, offset, advanced, TokenKind.Comment));
            return advanced;
        }

        return 0;
    }
    private static int ReadLineComment(string source, int offset, List<Token> tokens)
    {        
        if (TryPeek(source, offset, out var first) && first == '/' &&
            TryPeek(source, offset + 1, out var second) && second == '/')
        {            
            var advanced = AdvancePastEndOfLine(source, offset);
            tokens.Add(new Token(source, offset, advanced, TokenKind.Comment));
            return advanced;
        }

        return 0;
    }
}
