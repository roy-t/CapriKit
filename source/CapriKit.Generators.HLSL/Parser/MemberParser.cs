using CapriKit.Generators.HLSL.Tokenizer;
using static CapriKit.Generators.HLSL.Parser.ParserUtils;

namespace CapriKit.Generators.HLSL.Parser;

internal static class MemberParser
{
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
        var modifiers = ConsumeModifiers(state);

        var type = state.ExpectType();
        var name = state.ExpectIdentifier();
        var dimensions = ParseArrayDimensions(state);
        var semantic = SemanticParser.ParseSemantic(state);

        state.ExpectOperator(";");
        return new Member(type, name, semantic, modifiers, dimensions);
    }

    /// <summary>
    /// Parses zero or more <c>[size]</c> array dimensions that follow a member name, e.g. <c>[3][2]</c>.
    /// </summary>
    private static IReadOnlyList<uint> ParseArrayDimensions(ParseState state)
    {
        var dimensions = new List<uint>();
        while (state.Match(TokenKind.Operator, "["))
        {
            var size = state.Advance();
            if (size.Kind != TokenKind.IntegerLiteral)
            {
                throw new Exception($"Expected array size but got {size.Kind} '{size.Value}'.");
            }
            dimensions.Add(uint.Parse(size.Value));
            state.ExpectOperator("]");
        }
        return dimensions;
    }
}
