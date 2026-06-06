namespace CapriKit.Generators.HLSL.Parser.Infrastructure;

internal record OptionalPatternParseStep<TAccumulator>(ParserBuilder<TAccumulator> Parser, bool IsOptional, string Description)
    : IParseStep<TAccumulator>
{
    public bool TryParse(ParseState state, ref TAccumulator accumulator)
    {
        if (Parser.TryParse(state, ref accumulator))
        {
            return true;
        }

        return IsOptional;
    }

    public override string ToString() => $"{(IsOptional ? "Optional pattern" : "Pattern")} {Description}";
}
