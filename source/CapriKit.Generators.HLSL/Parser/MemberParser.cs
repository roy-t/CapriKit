using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

public static class MemberParser
{
    private static readonly HashSet<string> InterpolationModifiers =
    [
        "linear", "centroid", "nointerpolation", "noperspective", "sample",
    ];

    /// <summary>
    /// Parses HLSL struct members
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-struct"/>
    public static List<Member> ParseList(ParseState state)
    {
        var fields = new List<Member>();
        while (!state.Peek(TokenKind.Operator, "}"))
        {
            fields.Add(Parse(state));
        }
        return fields;
    }

    public static Member Parse(ParseState state)
    {
        while (state.Peek(TokenKind.Keyword, InterpolationModifiers))
        {
            state.Advance();
        }

        var type = state.ExpectType();
        var name = state.ExpectIdentifier();
        var semantic = SemanticParser.ParseSemantic(state);

        state.ExpectOperator(";");
        return new Member(type, name, semantic);
    }
}
