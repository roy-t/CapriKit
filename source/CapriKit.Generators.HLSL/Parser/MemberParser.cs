using CapriKit.Generators.HLSL.Parser.Infrastructure;
using static CapriKit.Generators.HLSL.Parser.Infrastructure.ParserBuilderUtilities;

namespace CapriKit.Generators.HLSL.Parser;

internal static class MemberParser
{
    private record MemberAccumulator
    {
        public List<string> Modifiers { get; } = [];
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<uint> Dimensions { get; } = [];
        public string Semantic { get; set; } = string.Empty;
    }

    /// <summary>
    /// A parser that accumulates zero or more members, for embedding in a struct or cbuffer parser.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-struct"/>
    public static ParserBuilder<List<Member>> CreateListParser() =>
        new ParserBuilder<List<Member>>()
            .Repeat(new ParserBuilder<List<Member>>()
                .RequiredPattern(CreateMemberParser(), () => new MemberAccumulator(), (list, m) => { list.Add(ToMember(m)); return list; }));

    public static Member Parse(ParseState state)
    {
        var accumulator = new MemberAccumulator();
        if (!CreateMemberParser().TryParse(state, ref accumulator))
        {
            throw new Exception("Expected a struct member.");
        }

        return ToMember(accumulator);
    }

    private static ParserBuilder<MemberAccumulator> CreateMemberParser()
    {
        // A single array dimension such as `[3]`
        var dimension = new ParserBuilder<MemberAccumulator>()
            .Required(Operator("["))
            .Required(AnyIntegerLiteral, (a, t) => { a.Dimensions.Add(uint.Parse(t.Value)); return a; })
            .Required(Operator("]"));

        // A full member such as `precise float4 Color[4][2] : SV_COLOR;`
        return new ParserBuilder<MemberAccumulator>()
            .Repeat(AnyModifier, (a, t) => { a.Modifiers.Add(t.Value); return a; })
            .Required(AnyType, (a, t) => a with { Type = t.Value })
            .Required(AnyIdentifier, (a, t) => a with { Name = t.Value })
            .Repeat(dimension) // repeat for 0..n dimensions
            .OptionalSemantic((a, t) => a with { Semantic = t.Value })
            .Required(Operator(";"));
    }

    private static Member ToMember(MemberAccumulator accumulator)
    {
        return new(accumulator.Type, accumulator.Name, accumulator.Semantic, accumulator.Modifiers, accumulator.Dimensions);
    }
}
