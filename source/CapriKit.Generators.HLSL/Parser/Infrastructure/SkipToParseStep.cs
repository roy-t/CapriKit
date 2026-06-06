namespace CapriKit.Generators.HLSL.Parser.Infrastructure;

internal record SkipToParseStep<TAccumulator>(Matcher Matcher, string Description)
    : IParseStep<TAccumulator>
{
    public bool TryParse(ParseState state, ref TAccumulator accumulator)
    {
        while (!state.IsAtEnd)
        {
            var token = state.Peek();
            if (Matcher(token))
            {
                return true;
            }
            state.Advance();
        }

        return false;
    }

    public override string ToString() => $"Optional {Description}";
}
