namespace CapriKit.Generators.HLSL.Parser.Infrastructure;

internal record BlockParseStep<TAccumulator>(Matcher Open, Matcher Close, string Description)
    : IParseStep<TAccumulator>
{
    public bool TryParse(ParseState state, ref TAccumulator accumulator)
    {
        var token = state.Peek();
        if (!Open(token))
        {
            return false;
        }
        state.Advance();

        var depth = 1;
        while (!state.IsAtEnd && depth > 0)
        {
            token = state.Advance();
            if (Close(token))
            {
                depth--;
            }
            else if (Open(token))
            {
                depth++;
            }
        }

        return depth == 0;
    }

    public override string ToString() => $"Block {Description}";
}
