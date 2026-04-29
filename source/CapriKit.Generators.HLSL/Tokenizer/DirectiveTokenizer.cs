using static CapriKit.Generators.HLSL.Tokenizer.TokenizerUtils;

namespace CapriKit.Generators.HLSL.Tokenizer;

public static class DirectiveTokenizer
{
    /// <summary>
    /// Reads HLSL preprocessor directives. For example #pragma
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-preprocessor"/>
    public static int ReadDirective(string source, int offset, List<Token> tokens)
    {
        if (TryPeek(source, offset, out var c) && c == '#')
        {
            var advanced = AdvancePastEndOfLine(source, offset);
            var token = new Token(source, offset, advanced, TokenKind.Directive);
            tokens.Add(token);

            return advanced;
        }

        return 0;
    }
}
