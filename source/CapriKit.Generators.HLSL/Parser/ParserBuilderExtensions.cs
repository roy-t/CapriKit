using static CapriKit.Generators.HLSL.Parser.ParserBuilderUtilities;

namespace CapriKit.Generators.HLSL.Parser;

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
}
