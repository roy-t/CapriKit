namespace CapriKit.Generators.HLSL.Parser.Infrastructure;

internal record RepeatParseStep<TAccumulator>(IParseStep<TAccumulator> Step)
    : IParseStep<TAccumulator>
{
    public bool TryParse(ParseState state, ref TAccumulator accumulator)
    {
        while (!state.IsAtEnd)
        {
            var mark = state.Mark();
            if (!Step.TryParse(state, ref accumulator) || state.Mark() == mark)
            {
                break;
            }
        }

        return true;
    }

    public override string ToString() => $"Repeat({Step})";
}
