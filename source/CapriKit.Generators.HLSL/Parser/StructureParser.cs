using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

public static class StructureParser
{
    /// <summary>
    /// Parses a struct declaration.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-struct"/>
    public static bool TryParse(ParseState state, out Structure structure)
    {
        structure = default!;

        if (!state.Peek(TokenKind.Keyword, "struct"))
        {
            return false;
        }

        state.ExpectKeyword("struct");
        var name = state.ExpectIdentifier();
        state.ExpectOperator("{");
        var fields = MemberParser.ParseList(state);
        state.ExpectOperator("}");
        state.ExpectOperator(";");

        structure = new Structure(name, fields);
        return true;
    }
}
