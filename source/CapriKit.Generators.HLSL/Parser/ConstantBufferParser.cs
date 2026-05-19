using CapriKit.Generators.HLSL.Tokenizer;
using System.Diagnostics.CodeAnalysis;
using static CapriKit.Generators.HLSL.Parser.ParserUtils;

namespace CapriKit.Generators.HLSL.Parser;

public static class ConstantBufferParser
{
    /// <summary>
    /// Parses a cbuffer.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-constants#organizing-constant-buffers"/>
    public static bool TryParse(ParseState state, [NotNullWhen(true)] out ConstantBuffer? buffer)
    {
        buffer = default;

        if (!state.Peek(TokenKind.Keyword, "cbuffer"))
        {
            return false;
        }
        state.Advance();

        var name = state.ExpectIdentifier();

        var register = 0u;
        if (state.Peek(TokenKind.Operator, ":"))
        {
            state.Advance();
            state.ExpectKeyword("register");
            state.ExpectOperator("(");
            register = ParseRegisterIndex(state.ExpectIdentifier());
            state.ExpectOperator(")");
        }

        state.ExpectOperator("{");
        var fields = MemberParser.ParseList(state);
        state.ExpectOperator("}");
        state.ExpectOperator(";");

        buffer = new ConstantBuffer(name, register, fields);
        return true;
    }
}
