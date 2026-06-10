using CapriKit.Generators.HLSL.Parser.Infrastructure;
using System.Diagnostics.CodeAnalysis;
using static CapriKit.Generators.HLSL.Parser.Infrastructure.ParserBuilderUtilities;

namespace CapriKit.Generators.HLSL.Parser;

internal static class FunctionParser
{
    internal record FunctionAccumulator
    {
        public string Name { get; set; } = string.Empty;
        public string Semantic { get; set; } = string.Empty;
        public List<Argument> Arguments { get; set; } = [];
    }

    public static bool TryParse(ParseState state, [NotNullWhen(true)] out Function? function)
    {
        var parser = Create();

        var accumulator = new FunctionAccumulator();
        if (parser.TryParse(state, ref accumulator))
        {
            function = new Function(accumulator.Name, accumulator.Semantic, accumulator.Arguments);
            return true;
        }

        function = default;
        return false;
    }

    public static ParserBuilder<FunctionAccumulator> Create()
    {
        var argumentParser = ArgumentParser.CreateListParser();

        return new ParserBuilder<FunctionAccumulator>()
            .Optional(Keyword("inline"))
            .Optional(Keyword("precise"))
            .Required(AnyType)
            .Required(AnyIdentifier, (a, t) => a with { Name = t.Value })
            .Required(Operator("("))
            .RequiredPattern(ArgumentParser.CreateListParser(), () => [], (a, arguments) => { a.Arguments.AddRange(arguments); return a; })
            .Required(Operator(")"))
            .OptionalSemantic((a, t) => a with { Semantic = t.Value })
            .RequiredBlock(Operator("{"), Operator("}"));
    }
}
