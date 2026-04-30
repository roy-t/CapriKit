using CapriKit.Generators.HLSL.Tokenizer;
using static CapriKit.Generators.HLSL.Parser.ParserUtils;

namespace CapriKit.Generators.HLSL.Parser;

public static class EntryPointParser
{
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
