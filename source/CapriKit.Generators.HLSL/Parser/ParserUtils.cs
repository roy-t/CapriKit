using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

internal static class ParserUtils
{
    public static void SkipArgumentList(ParseState state)
    {
        state.ExpectOperator("(");
        while (!state.IsAtEnd && !state.Peek(TokenKind.Operator, ")"))
        {
            state.Advance();
        }
        state.ExpectOperator(")");
    }

    public static void SkipMethodBlock(ParseState state)
    {
        state.ExpectOperator("{");
        while (!state.IsAtEnd && !state.Peek(TokenKind.Operator, "}"))
        {
            state.Advance();
        }
        state.ExpectOperator("}");
    }
}
