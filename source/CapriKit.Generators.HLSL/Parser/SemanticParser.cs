using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

internal static class SemanticParser
{
    public static string ParseSemantic(ParseState state)
    {
        if (state.Peek(TokenKind.Operator, ":"))
        {
            state.Advance();
            return state.ExpectIdentifier();
        }

        return string.Empty;
    }
}
