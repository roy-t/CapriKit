using CapriKit.Generators.HLSL.Parser.Infrastructure;
using static CapriKit.Generators.HLSL.Parser.Infrastructure.ParserBuilderUtilities;

namespace CapriKit.Generators.HLSL.Parser;

// NOTE: This class is extremely similar to MemberParser but is kept separate deliberately to keep things
// simple and because we believe it might diverge later to be more critical of syntax and modifier types.
internal static class ArgumentParser
{
    internal record ArgumentAccumulator
    {
        public List<string> Modifiers { get; } = [];
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<uint> Dimensions { get; } = [];
        public string Semantic { get; set; } = string.Empty;
    }

    /// <summary>
    /// A parser that accumulates zero or more arguments, for parsing an argument list.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-function-parameters"/>
    public static ParserBuilder<List<Argument>> CreateListParser() =>
        new ParserBuilder<List<Argument>>()
            .Repeat(new ParserBuilder<List<Argument>>()
                .RequiredPattern(CreateMemberParser(), () => new ArgumentAccumulator(), (list, m) => { list.Add(ToArgument(m)); return list; }));

    // Only for easier testing
    internal static Argument Parse(ParseState state)
    {
        var accumulator = new ArgumentAccumulator();
        if (!CreateMemberParser().TryParse(state, ref accumulator))
        {
            throw new Exception("Expected an argument.");
        }

        return ToArgument(accumulator);
    }

    private static ParserBuilder<ArgumentAccumulator> CreateMemberParser()
    {
        // A single array dimension such as `[3]`
        var dimension = new ParserBuilder<ArgumentAccumulator>()
            .Required(Operator("["))
            .Required(AnyIntegerLiteral, (a, t) => { a.Dimensions.Add(uint.Parse(t.Value)); return a; })
            .Required(Operator("]"));

        // A full argument such as `inout float4 Color[4][2] : SV_COLOR,`
        return new ParserBuilder<ArgumentAccumulator>()
            .Repeat(AnyModifier, (a, t) => { a.Modifiers.Add(t.Value); return a; })
            .Required(AnyType, (a, t) => a with { Type = t.Value })
            .Required(AnyIdentifier, (a, t) => a with { Name = t.Value })
            .Repeat(dimension) // repeat for 0..n dimensions
            .OptionalSemantic((a, t) => a with { Semantic = t.Value })
            .Optional(Operator(","));
    }

    private static Argument ToArgument(ArgumentAccumulator accumulator)
    {
        return new(accumulator.Type, accumulator.Name, accumulator.Semantic, accumulator.Modifiers, accumulator.Dimensions);
    }
}
