using static CapriKit.Generators.HLSL.Parser.ParserBuilderUtilities;

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

        public Member ToMember() => new(Type, Name, Semantic, Modifiers, Dimensions);
    }

    /// <summary>
    /// Parses zero or more HLSL struct members
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-struct"/>
    public static List<Member> ParseList(ParseState state)
    {
        var members = new List<Member>();
        var parser = new ParserBuilder<List<Member>>()
            .Repeat(new ParserBuilder<List<Member>>()
                .SubTree(CreateMemberParser(), () => new MemberAccumulator(), (list, m) => { list.Add(m.ToMember()); return list; }));

        parser.TryParse(state, ref members);
        return members;
    }

    public static Member Parse(ParseState state)
    {
        var accumulator = new MemberAccumulator();
        if (!CreateMemberParser().TryParse(state, ref accumulator))
        {
            throw new Exception("Expected a struct member.");
        }

        return accumulator.ToMember();
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
            .Required(AnyType, (a, t) => { a.Type = t.Value; return a; })
            .Required(AnyIdentifier, (a, t) => { a.Name = t.Value; return a; })
            .Repeat(dimension) // repeat for 0..n dimensions
            .OptionalSemantic((a, t) => { a.Semantic = t.Value; return a; })
            .Required(Operator(";"));
    }
}
