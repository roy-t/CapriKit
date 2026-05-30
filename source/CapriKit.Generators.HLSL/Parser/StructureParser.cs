using System.Diagnostics.CodeAnalysis;
using static CapriKit.Generators.HLSL.Parser.ParserBuilderUtilities;

namespace CapriKit.Generators.HLSL.Parser;

internal static class StructureParser
{
    private record StructureAccumulator
    {
        public string Name { get; set; } = string.Empty;
        public List<Member> Members { get; } = [];
    }

    /// <summary>
    /// Parses a struct declaration.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-struct"/>
    public static bool TryParse(ParseState state, [NotNullWhen(true)] out Structure? structure)
    {
        // A struct such as `struct VS_INPUT { float4 position : POSITION; };`
        var parser = new ParserBuilder<StructureAccumulator>()
            .Required(Keyword("struct"))
            .Required(AnyIdentifier, (a, t) => { a.Name = t.Value; return a; })
            .Required(Operator("{"))
            .SubTree(MemberParser.CreateListParser(), () => [], (a, members) => { a.Members.AddRange(members); return a; })
            .Required(Operator("}"))
            .Required(Operator(";"));

        var accumulator = new StructureAccumulator();
        if (parser.TryParse(state, ref accumulator))
        {
            structure = new Structure(accumulator.Name, accumulator.Members);
            return true;
        }

        structure = default;
        return false;
    }
}
