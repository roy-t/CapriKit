using System.Diagnostics.CodeAnalysis;
using static CapriKit.Generators.HLSL.Parser.ParserBuilderUtilities;

namespace CapriKit.Generators.HLSL.Parser;

internal static class FunctionParser
{
    private record FunctionAccumulator
    {
        public string Name { get; set; } = string.Empty;
        public string Semantic { get; set; } = string.Empty;
    }

    public static bool TryParse(ParseState state, [NotNullWhen(true)] out Function? function)
    {
        var parser = new ParserBuilder<FunctionAccumulator>()
            .Optional(Keyword("inline"))
            .Optional(Keyword("precise"))
            .Required(AnyType)
            .Required(AnyIdentifier, (a, t) => a with { Name = t.Value })
            .RequiredBlock(Operator("("), Operator(")"))
            .OptionalSemantic((a, t) => a with { Semantic = t.Value })
            .RequiredBlock(Operator("{"), Operator("}"));

        var accumulator = new FunctionAccumulator();
        if (parser.TryParse(state, ref accumulator))
        {
            function = new Function(accumulator.Name, accumulator.Semantic);
            return true;
        }

        function = default;
        return false;
    }
}
