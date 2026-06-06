namespace CapriKit.Generators.HLSL.Parser.Infrastructure;

internal record RequiredPatternParseStep<TAccumulator, TChild>(
    ParserBuilder<TChild> Parser,
    Func<TChild> Seed,
    Func<TAccumulator, TChild, TAccumulator> Merge,
    string Description)
    : IParseStep<TAccumulator>
{
    public bool TryParse(ParseState state, ref TAccumulator accumulator)
    {
        var child = Seed();
        if (!Parser.TryParse(state, ref child))
        {
            return false;
        }

        accumulator = Merge(accumulator, child);
        return true;
    }

    public override string ToString() => $"Required pattern: {Description}";
}
