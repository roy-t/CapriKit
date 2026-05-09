namespace CapriKit.Generators.HLSL.Parser;

public static class StructureParser
{
    public static Structure Parse(ParseState state)
    {
        state.ExpectKeyword("struct");
        var name = state.ExpectIdentifier();
        state.ExpectOperator("{");
        var fields = MemberParser.ParseList(state);
        state.ExpectOperator("}");
        state.ExpectOperator(";");
        return new Structure(name, fields);
    }
}
