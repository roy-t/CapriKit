namespace CapriKit.Generators.HLSL.Parser.Infrastructure;

internal record OptionalParseStep<TAccumulator>(Matcher Matcher, Accumulator<TAccumulator>? Accumulator, string Description)
    : IParseStep<TAccumulator>
{
    public bool TryParse(ParseState state, ref TAccumulator accumulator)
    {
        var token = state.Peek();
        if (Matcher(token))
        {
            state.Advance();
            if (Accumulator != null)
            {
                accumulator = Accumulator(accumulator, token);
            }
        }
        return true;
    }

    public override string ToString() => $"Optional {Description}";
}
