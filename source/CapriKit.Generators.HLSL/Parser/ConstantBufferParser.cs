using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

public static class ConstantBufferParser
{
    public static ConstantBuffer Parse(ParseState state)
    {
        state.ExpectKeyword("cbuffer");
        var name = state.ExpectIdentifier();

        var register = string.Empty;
        if (state.Peek(TokenKind.Operator, ":"))
        {
            state.Advance();
            state.ExpectKeyword("register");
            state.ExpectOperator("(");
            register = state.ExpectIdentifier();
            state.ExpectOperator(")");
        }

        state.ExpectOperator("{");
        var fields = MemberParser.ParseList(state);
        state.ExpectOperator("}");
        state.ExpectOperator(";");
        return new ConstantBuffer(name, register, fields);
    }
}
