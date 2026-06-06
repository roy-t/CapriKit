using CapriKit.Generators.HLSL.Parser.Infrastructure;
using System.Diagnostics.CodeAnalysis;
using static CapriKit.Generators.HLSL.Parser.Infrastructure.ParserBuilderUtilities;

namespace CapriKit.Generators.HLSL.Parser;

internal static class VariableParser
{
    private record VariableAccumulator
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public uint Register { get; set; } = 0u;
        public List<string> Modifiers { get; set; } = [];
        public List<uint> Dimensions { get; } = [];
    }

    /// <summary>
    /// Parses HLSL variable declarations.
    /// </summary>
    /// <remarks>Does not support packoffsets</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-variable-syntax"/>
    public static bool TryParse(ParseState state, [NotNullWhen(true)] out Variable? variable)
    {
        // A single array dimension such as `[3]`
        var dimension = new ParserBuilder<VariableAccumulator>()
            .Required(Operator("["))
            .Required(AnyIntegerLiteral, (a, t) => { a.Dimensions.Add(uint.Parse(t.Value)); return a; })
            .Required(Operator("]"));

        var assignment = new ParserBuilder<VariableAccumulator>()
            .Required(Operator("="))
            .SkipTo(Operator(";"));

        // A full variable such as `static const float Pi = 3.14;`
        var parser = new ParserBuilder<VariableAccumulator>()
            .Repeat(AnyModifier, (a, t) => { a.Modifiers.Add(t.Value); return a; })
            .Required(AnyType, (a, t) => a with { Type = t.Value })
            .Required(AnyIdentifier, (a, t) => a with { Name = t.Value })
            .Repeat(dimension) // repeat for 0..n dimensions
            .OptionalRegister((a, t) => a with { Register = ParseRegister(t.Value) })
            .OptionalPattern(assignment)
            .Required(Operator(";")); // TODO: if there is an assignment this fails

        var accumulator = new VariableAccumulator();
        if (parser.TryParse(state, ref accumulator))
        {
            variable = new Variable(accumulator.Type, accumulator.Name, accumulator.Register, accumulator.Modifiers, accumulator.Dimensions);
            return true;
        }

        variable = default;
        return false;
    }
}
