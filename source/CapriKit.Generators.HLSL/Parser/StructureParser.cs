using CapriKit.Generators.HLSL.Tokenizer;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.Generators.HLSL.Parser;

internal static class StructureParser
{
    /// <summary>
    /// Parses a struct declaration.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-struct"/>
    public static bool TryParse(ParseState state, [NotNullWhen(true)] out Structure? structure)
    {
        structure = default;

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
