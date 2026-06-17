using CapriKit.Generators.HLSL.Parser.Infrastructure;
using System.Diagnostics.CodeAnalysis;
using static CapriKit.Generators.HLSL.Parser.Infrastructure.ParserBuilderUtilities;

namespace CapriKit.Generators.HLSL.Parser;

internal static class StructureParser
{
    private record StructureAccumulator
    {
        public StructureKind Kind { get; set; } = StructureKind.Structure;
        public string Name { get; set; } = string.Empty;
        public List<Member> Members { get; } = [];
    }

    /// <summary>
    /// Parses a struct declaration. A struct tagged with <c>#pragma Input</c> is marked as a
    /// vertex shader input so an input element description can be generated for it.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-struct"/>
    public static bool TryParse(ParseState state, [NotNullWhen(true)] out Structure? structure)
    {
        // A struct such as `#pragma Input struct VS_INPUT { float4 position : POSITION; };`
        var parser = new ParserBuilder<StructureAccumulator>()
            .Repeat(Pragma("Input"), (a, t) => { a.Kind = StructureKind.VertexShaderInput; return a; })
            .Required(Keyword("struct"))
            .Required(AnyIdentifier, (a, t) => { a.Name = t.Value; return a; })
            .Required(Operator("{"))
            .RequiredPattern(MemberParser.CreateListParser(), () => [], (a, members) => { a.Members.AddRange(members); return a; })
            .Required(Operator("}"))
            .Required(Operator(";"));

        var accumulator = new StructureAccumulator();
        if (parser.TryParse(state, ref accumulator))
        {
            structure = new Structure(accumulator.Name, accumulator.Members, accumulator.Kind);
            return true;
        }

        structure = default;
        return false;
    }
}
