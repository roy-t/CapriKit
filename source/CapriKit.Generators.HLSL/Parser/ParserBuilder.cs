using CapriKit.Generators.HLSL.Tokenizer;
using System.Runtime.CompilerServices;

namespace CapriKit.Generators.HLSL.Parser;

internal delegate bool Matcher(Token token);
internal delegate T Accumulator<T>(T accumulator, Token token);

internal interface IParseStep<TAccumulator>
{
    bool TryParse(ParseState state, ref TAccumulator accumulator);
}

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

internal record SubTreeParseStep<TAccumulator>(ParserBuilder<TAccumulator> Parser, bool IsOptional, string Description)
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

    public override string ToString() => $"{(IsOptional ? "OptionalSubTree" : "SubTree")} {Description}";
}

internal record SubTreeParseStep<TAccumulator, TChild>(
    ParserBuilder<TChild> Parser,
    Func<TChild> Seed,
    Func<TAccumulator, TChild, TAccumulator> Merge,
    bool IsOptional,
    string Description)
    : IParseStep<TAccumulator>
{
    public bool TryParse(ParseState state, ref TAccumulator accumulator)
    {
        var child = Seed();
        if (!Parser.TryParse(state, ref child))
        {
            return IsOptional;
        }

        accumulator = Merge(accumulator, child);
        return true;
    }

    public override string ToString() => $"{(IsOptional ? "OptionalSubTree" : "SubTree")} {Description}";
}

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

internal sealed class ParserBuilder<TAccumulator>
{
    private readonly List<IParseStep<TAccumulator>> Steps;

    public ParserBuilder()
    {
        Steps = [];
    }

    public ParserBuilder<TAccumulator> Optional(
        Matcher matcher,
        Accumulator<TAccumulator>? accumulator = null,
        [CallerArgumentExpression(nameof(matcher))] string matcherText = "",
        [CallerArgumentExpression(nameof(accumulator))] string accumulatorText = "")
    {
        Steps.Add(new OptionalParseStep<TAccumulator>(matcher, accumulator, Describe(matcherText, accumulatorText)));
        return this;
    }

    public ParserBuilder<TAccumulator> Required(
        Matcher matcher,
        Accumulator<TAccumulator>? accumulator = null,
        [CallerArgumentExpression(nameof(matcher))] string matcherText = "",
        [CallerArgumentExpression(nameof(accumulator))] string accumulatorText = "")
    {
        Steps.Add(new RequiredParseStep<TAccumulator>(matcher, accumulator, Describe(matcherText, accumulatorText)));
        return this;
    }

    public ParserBuilder<TAccumulator> RequiredBlock(
        Matcher open,
        Matcher close,
        [CallerArgumentExpression(nameof(open))] string openText = "",
        [CallerArgumentExpression(nameof(close))] string closeText = "")
    {
        Steps.Add(new BlockParseStep<TAccumulator>(open, close, $"{openText}..{closeText}"));
        return this;
    }

    public ParserBuilder<TAccumulator> SubTree(
        ParserBuilder<TAccumulator> parser,
        bool isOptional = false,
        [CallerArgumentExpression(nameof(parser))] string parserText = "")
    {
        Steps.Add(new SubTreeParseStep<TAccumulator>(parser, isOptional, parserText));
        return this;
    }

    public ParserBuilder<TAccumulator> SubTree<TChild>(
        ParserBuilder<TChild> parser,
        Func<TChild> seed,
        Func<TAccumulator, TChild, TAccumulator> merge,
        bool isOptional = false,
        [CallerArgumentExpression(nameof(parser))] string parserText = "",
        [CallerArgumentExpression(nameof(merge))] string mergeText = "")
    {
        Steps.Add(new SubTreeParseStep<TAccumulator, TChild>(parser, seed, merge, isOptional, Describe(parserText, mergeText)));
        return this;
    }

    public ParserBuilder<TAccumulator> Repeat(
        ParserBuilder<TAccumulator> parser,
        [CallerArgumentExpression(nameof(parser))] string parserText = "")
    {
        Steps.Add(new RepeatParseStep<TAccumulator>(new SubTreeParseStep<TAccumulator>(parser, false, parserText)));
        return this;
    }

    public ParserBuilder<TAccumulator> Repeat(
        Matcher matcher,
        Accumulator<TAccumulator>? accumulator = null,
        [CallerArgumentExpression(nameof(matcher))] string matcherText = "",
        [CallerArgumentExpression(nameof(accumulator))] string accumulatorText = "")
    {
        Steps.Add(new RepeatParseStep<TAccumulator>(new RequiredParseStep<TAccumulator>(matcher, accumulator, Describe(matcherText, accumulatorText))));
        return this;
    }

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

internal static class ParserBuilderUtilities
{
    public static readonly Matcher AnyType = t => t.Kind == TokenKind.Keyword || t.Kind == TokenKind.Identifier;
    public static readonly Matcher AnyIdentifier = t => t.Kind == TokenKind.Identifier;
    public static readonly Matcher AnyIntegerLiteral = t => t.Kind == TokenKind.IntegerLiteral;
    public static readonly Matcher AnyModifier = t => ParserUtils.IsModifier(t);

    public static Matcher Keyword(string value) => t => t.Kind == TokenKind.Keyword && value.Equals(t.Value, StringComparison.Ordinal);
    public static Matcher Operator(string value) => t => t.Kind == TokenKind.Operator && value.Equals(t.Value, StringComparison.Ordinal);
}

internal static class ParserBuilderExtensions
{
    public static ParserBuilder<TAccumulator> OptionalSemantic<TAccumulator>(this ParserBuilder<TAccumulator> parser, Accumulator<TAccumulator>? accumulator = null)
    {
        var semanticParser = new ParserBuilder<TAccumulator>()
            .Optional(t => t.Kind == TokenKind.Operator && t.Value == ":")
            .Optional(t => t.Kind == TokenKind.Identifier, accumulator);

        return parser.SubTree(semanticParser);
    }
}
