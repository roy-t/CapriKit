using static CapriKit.Generators.HLSL.Parser.Infrastructure.ParserBuilderUtilities;

namespace CapriKit.Generators.HLSL.Parser.Infrastructure;

internal static class ParserBuilderExtensions
{
    public static ParserBuilder<TAccumulator> OptionalSemantic<TAccumulator>(this ParserBuilder<TAccumulator> parser, Accumulator<TAccumulator>? accumulator = null)
    {
        var semanticParser = new ParserBuilder<TAccumulator>()
            .Required(Operator(":"))
            .Required(AnyIdentifier, accumulator);

        return parser.OptionalPattern(semanticParser);
    }

    public static ParserBuilder<TAccumulator> OptionalRegister<TAccumulator>(this ParserBuilder<TAccumulator> parser, Accumulator<TAccumulator>? accumulator = null)
    {
        var registerParser = new ParserBuilder<TAccumulator>()
            .Required(Operator(":"))
            .Required(Keyword("register"))
            .Required(Operator("("))
            .Required(AnyIdentifier, accumulator)
            .Required(Operator(")"));

        return parser.OptionalPattern(registerParser);
    }

    public static ParserBuilder<TAccumulator> RequiredRegister<TAccumulator>(this ParserBuilder<TAccumulator> parser, Func<TAccumulator, uint, TAccumulator> merge)
    {
        var registerParser = new ParserBuilder<uint>()
            .Required(Operator(":"))
            .Required(Keyword("register"))
            .Required(Operator("("))
            .Required(AnyIdentifier, (a, b) => ParseRegister(b.Value))
            .Required(Operator(")"));

        return parser.RequiredPattern(registerParser, () => 0u, merge);
    }


}
