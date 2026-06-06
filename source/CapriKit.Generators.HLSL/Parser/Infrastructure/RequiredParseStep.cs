namespace CapriKit.Generators.HLSL.Parser.Infrastructure;

internal record RequiredParseStep<TAccumulator>(Matcher Matcher, Accumulator<TAccumulator>? Accumulator, string Description)
    : IParseStep<TAccumulator>
{
    public bool TryParse(ParseState state, ref TAccumulator accumulator)
    {
        var token = state.Peek();
        if (!Matcher(token))
        {
            return false;
        }

        if (Accumulator != null)
        {
            accumulator = Accumulator(accumulator, token);
        }
        state.Advance();
        return true;
    }

    public override string ToString() => $"Required {Description}";
}
