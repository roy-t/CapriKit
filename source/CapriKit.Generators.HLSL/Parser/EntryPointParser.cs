using CapriKit.Generators.HLSL.Tokenizer;
using static CapriKit.Generators.HLSL.Parser.ParserUtils;

namespace CapriKit.Generators.HLSL.Parser;

public static class EntryPointParser
{
    /// <summary>
    /// Parses a HLSL function that could be a valid shader entrypoint
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-function-syntax"/>
    public static EntryPoint Parse(ParseState state, EntryPointKind kind)
    {
        state.Match(TokenKind.Keyword, "inline");
        state.Match(TokenKind.Keyword, "precise");

        state.ExpectType();
        var name = state.ExpectIdentifier();

        SkipArgumentList(state);
        var semantic = SemanticParser.ParseSemantic(state);
        SkipMethodBlock(state);

        return new EntryPoint(kind, name, semantic);
    }
}
