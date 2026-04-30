using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

internal static class ParserUtils
{
    public static void SkipArgumentList(ParseState state)
    {
        SkipSegment(state, "(", ")");
    }

    public static void SkipMethodBlock(ParseState state)
    {
        SkipSegment(state, "{", "}");
    }

    private static void SkipSegment(ParseState state, string open, string close)
    {
        state.ExpectOperator(open);
        var depth = 1;
        while (!state.IsAtEnd && depth > 0)
        {
            var token = state.Advance();
            if (token.Kind == TokenKind.Operator)
            {
                if (token.Value == open)
                {
                    depth++;
                }
                else if (token.Value == close)
                {
                    depth--;
                }
            }
        }
    }
}
