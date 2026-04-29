using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

public static class FieldParser
{
    private static readonly HashSet<string> InterpolationModifiers =
    [
        "linear", "centroid", "nointerpolation", "noperspective", "sample",
    ];

    public static List<Field> ParseList(ParseState state)
    {
        var fields = new List<Field>();
        while (!state.IsAtEnd && !state.Peek(TokenKind.Operator, "}"))
        {
            fields.Add(Parse(state));
        }
        return fields;
    }

    public static Field Parse(ParseState state)
    {
        while (state.Peek(TokenKind.Keyword, InterpolationModifiers))
        {
            state.Advance();
        }

        var type = state.ExpectType();
        var name = state.ExpectIdentifier();
        var semantic = SemanticParser.ParseSemantic(state);
        
        state.ExpectOperator(";");
        return new Field(type, name, semantic);
    }
}
