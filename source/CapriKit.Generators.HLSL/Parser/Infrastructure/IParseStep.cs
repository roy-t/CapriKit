namespace CapriKit.Generators.HLSL.Parser.Infrastructure;

internal interface IParseStep<TAccumulator>
{
    bool TryParse(ParseState state, ref TAccumulator accumulator);
}
