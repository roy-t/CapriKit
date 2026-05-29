using System.Diagnostics.CodeAnalysis;
using static CapriKit.Generators.HLSL.Parser.ParserBuilderUtilities;
using static CapriKit.Generators.HLSL.Parser.ParserUtils;

namespace CapriKit.Generators.HLSL.Parser;

internal static class ConstantBufferParser
{
    private record ConstantBufferAccumulator
    {
        public string Name { get; set; } = string.Empty;
        public uint Register { get; set; }
        public List<Member> Members { get; } = [];
    }

    /// <summary>
    /// Parses a cbuffer.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-constants#organizing-constant-buffers"/>
    public static bool TryParse(ParseState state, [NotNullWhen(true)] out ConstantBuffer? buffer)
    {
        // An optional `: register(b0)` binding
        var bindingClause = new ParserBuilder<ConstantBufferAccumulator>()
            .Required(Operator(":"))
            .Required(Keyword("register"))
            .Required(Operator("("))
            .Required(AnyIdentifier, (a, t) => { a.Register = ParseRegisterIndex(t.Value); return a; })
            .Required(Operator(")"));

        // A cbuffer such as `cbuffer Constants : register(b0) { float4x4 world; };`
        var parser = new ParserBuilder<ConstantBufferAccumulator>()
            .Required(Keyword("cbuffer"))
            .Required(AnyIdentifier, (a, t) => { a.Name = t.Value; return a; })
            .SubTree(bindingClause, true)
            .Required(Operator("{"))
            .SubTree(MemberParser.CreateListParser(), () => [], (a, members) => { a.Members.AddRange(members); return a; })
            .Required(Operator("}"))
            .Required(Operator(";"));

        var accumulator = new ConstantBufferAccumulator();
        if (parser.TryParse(state, ref accumulator))
        {
            buffer = new ConstantBuffer(accumulator.Name, accumulator.Register, accumulator.Members);
            return true;
        }

        buffer = default;
        return false;
    }
}
