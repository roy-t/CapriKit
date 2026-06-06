using CapriKit.Generators.HLSL.Tokenizer;
using System.Runtime.CompilerServices;

namespace CapriKit.Generators.HLSL.Parser.Infrastructure;

internal delegate bool Matcher(Token token);
internal delegate T Accumulator<T>(T accumulator, Token token);

internal sealed class ParserBuilder<TAccumulator>
{
    private readonly List<IParseStep<TAccumulator>> Steps;

    public ParserBuilder()
    {
        Steps = [];
    }

    /// <summary>
    /// Parse token, parsing does not fail if there is no match
    /// </summary>
    public ParserBuilder<TAccumulator> Optional(
        Matcher matcher,
        Accumulator<TAccumulator>? accumulator = null,
        [CallerArgumentExpression(nameof(matcher))] string matcherText = "",
        [CallerArgumentExpression(nameof(accumulator))] string accumulatorText = "")
    {
        Steps.Add(new OptionalParseStep<TAccumulator>(matcher, accumulator, Describe(matcherText, accumulatorText)));
        return this;
    }

    /// <summary>
    /// Expect and parse token
    /// </summary>
    public ParserBuilder<TAccumulator> Required(
        Matcher matcher,
        Accumulator<TAccumulator>? accumulator = null,
        [CallerArgumentExpression(nameof(matcher))] string matcherText = "",
        [CallerArgumentExpression(nameof(accumulator))] string accumulatorText = "")
    {
        Steps.Add(new RequiredParseStep<TAccumulator>(matcher, accumulator, Describe(matcherText, accumulatorText)));
        return this;
    }

    /// <summary>
    /// Consumes everything, up-to-EXCLUDING the requested token
    /// </summary>
    public ParserBuilder<TAccumulator> SkipTo(
    Matcher matcher,
    [CallerArgumentExpression(nameof(matcher))] string matcherText = "")
    {
        Steps.Add(new SkipToParseStep<TAccumulator>(matcher, $"Until: {matcherText}"));
        return this;
    }

    /// <summary>
    /// Consumes everything (including the delimiters) delimited by the two requested tokens
    /// </summary>
    public ParserBuilder<TAccumulator> RequiredBlock(
        Matcher open,
        Matcher close,
        [CallerArgumentExpression(nameof(open))] string openText = "",
        [CallerArgumentExpression(nameof(close))] string closeText = "")
    {
        Steps.Add(new BlockParseStep<TAccumulator>(open, close, $"{openText}..{closeText}"));
        return this;
    }

    /// <summary>
    /// Tries to consumes the given sub pattern, but does not fail if it doesn't match
    /// </summary>
    public ParserBuilder<TAccumulator> OptionalPattern(
       ParserBuilder<TAccumulator> parser,
       [CallerArgumentExpression(nameof(parser))] string parserText = "")
    {
        Steps.Add(new OptionalPatternParseStep<TAccumulator>(parser, true, parserText));
        return this;
    }

    /// <summary>
    /// Consumes the given sub pattern and merges the results of those matches back into the main pattern
    /// </summary>
    public ParserBuilder<TAccumulator> RequiredPattern<TChild>(
        ParserBuilder<TChild> parser,
        Func<TChild> seed,
        Func<TAccumulator, TChild, TAccumulator> merge,
        [CallerArgumentExpression(nameof(parser))] string parserText = "",
        [CallerArgumentExpression(nameof(merge))] string mergeText = "")
    {
        Steps.Add(new RequiredPatternParseStep<TAccumulator, TChild>(parser, seed, merge, Describe(parserText, mergeText)));
        return this;
    }

    /// <summary>
    /// Consumes the given pattern 0 or more times
    /// </summary>
    public ParserBuilder<TAccumulator> Repeat(
        ParserBuilder<TAccumulator> parser,
        [CallerArgumentExpression(nameof(parser))] string parserText = "")
    {
        Steps.Add(new RepeatParseStep<TAccumulator>(new OptionalPatternParseStep<TAccumulator>(parser, false, parserText)));
        return this;
    }

    /// <summary>
    /// Consumes the given pattern 0 or more times
    /// </summary>
    public ParserBuilder<TAccumulator> Repeat(
        Matcher matcher,
        Accumulator<TAccumulator>? accumulator = null,
        [CallerArgumentExpression(nameof(matcher))] string matcherText = "",
        [CallerArgumentExpression(nameof(accumulator))] string accumulatorText = "")
    {
        Steps.Add(new RepeatParseStep<TAccumulator>(new RequiredParseStep<TAccumulator>(matcher, accumulator, Describe(matcherText, accumulatorText))));
        return this;
    }

    /// <summary>
    /// Tries to parse the whole pattern constructed in this parser builder, returns false and resets
    /// parse state if unable.
    /// </summary>
    public bool TryParse(ParseState state, ref TAccumulator accumulator)
    {
        var mark = state.Mark();
        foreach (var step in Steps)
        {
            var current = state.IsAtEnd ? (Token?)null : state.Peek();
            if (!step.TryParse(state, ref accumulator))
            {
                state.Restore(mark);
                return false;
            }
        }

        return true;
    }

    // Combines the captured matcher/parser text with an optional accumulator lambda for tracing.
    private static string Describe(string matcherText, string accumulatorText) =>
        string.IsNullOrEmpty(accumulatorText) ? matcherText : $"{matcherText} => {accumulatorText}";
}
