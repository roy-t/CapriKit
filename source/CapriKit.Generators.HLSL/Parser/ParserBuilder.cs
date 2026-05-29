using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

internal delegate bool Matcher(Token token);
internal delegate T Accumulator<T>(T accumulator, Token token);

internal interface IParseStep<TAccumulator>
{
    bool TryParse(ParseState state, ref TAccumulator accumulator);
}

internal record RequiredParseStep<TAccumulator>(Matcher Matcher, Accumulator<TAccumulator>? Accumulator)
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
}

internal record OptionalParseStep<TAccumulator>(Matcher Matcher, Accumulator<TAccumulator>? Accumulator)
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
}

internal record BlockParseStep<TAccumulator>(Matcher Open, Matcher Close)
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
}

internal record SubTreeParseStep<TAccumulator>(ParserBuilder<TAccumulator> Parser)
    : IParseStep<TAccumulator>
{
    public bool TryParse(ParseState state, ref TAccumulator accumulator)
    {
        if (Parser.TryParse(state, ref accumulator))
        {
            return true;
        }

        return false;
    }
}

internal sealed class ParserBuilder<TAccumulator>
{
    private readonly List<IParseStep<TAccumulator>> Steps;

    public ParserBuilder()
    {
        Steps = [];
    }

    public ParserBuilder<TAccumulator> Optional(Matcher matcher, Accumulator<TAccumulator>? accumulator = null)
    {
        Steps.Add(new OptionalParseStep<TAccumulator>(matcher, accumulator));
        return this;
    }

    public ParserBuilder<TAccumulator> Required(Matcher matcher, Accumulator<TAccumulator>? accumulator = null)
    {
        Steps.Add(new RequiredParseStep<TAccumulator>(matcher, accumulator));
        return this;
    }

    public ParserBuilder<TAccumulator> RequiredBlock(Matcher open, Matcher close)
    {
        Steps.Add(new BlockParseStep<TAccumulator>(open, close));
        return this;
    }

    public ParserBuilder<TAccumulator> SubTree(ParserBuilder<TAccumulator> parser)
    {
        Steps.Add(new SubTreeParseStep<TAccumulator>(parser));
        return this;
    }

    public bool TryParse(ParseState state, ref TAccumulator accumulator)
    {
        var mark = state.Mark();
        foreach (var step in Steps)
        {
            if (!step.TryParse(state, ref accumulator))
            {
                state.Restore(mark);
                return false;
            }
        }

        return true;
    }
}

internal static class ParserBuilderUtilities
{
    public static readonly Matcher AnyType = t => t.Kind == TokenKind.Keyword || t.Kind == TokenKind.Identifier;
    public static readonly Matcher AnyIdentifier = t => t.Kind == TokenKind.Identifier;

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
