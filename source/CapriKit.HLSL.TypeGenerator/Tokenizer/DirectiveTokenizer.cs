using CapriKit.HLSL.TypeGenerator.Parsers;
using static CapriKit.HLSL.TypeGenerator.Tokenizer.TokenizerUtils;

namespace CapriKit.HLSL.TypeGenerator.Tokenizer;

public static class DirectiveTokenizer
{
    /// <summary>
    /// Reads HLSL preprocessor directives. For example #pragma
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
}
