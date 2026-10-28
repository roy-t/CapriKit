using CapriKit.Generators.HLSL.Tokenizer;
using static CapriKit.Generators.HLSL.Parser.Infrastructure.ParserBuilderUtilities;

namespace CapriKit.Generators.HLSL.Parser.Infrastructure;

/// <summary>
/// Parses a type including its (nested) type arguments, such as `A&lt;B, C&lt;D&gt;&gt;`, and passes a single
/// token that spans the entire type to the accumulator.
/// </summary>
internal record TypeParseStep<TAccumulator>(Accumulator<TAccumulator>? Accumulator, string Description)
    : IParseStep<TAccumulator>
{
    public bool TryParse(ParseState state, ref TAccumulator accumulator)
    {
        if (state.IsAtEnd || !AnyType(state.Peek()))
        {
            return false;
        }

        var first = state.Advance();
        var last = first;

        if (state.Peek(TokenKind.Operator, "<"))
        {
            var depth = 0;
            do
            {
                if (state.IsAtEnd)
                {
                    return false;
                }

                last = state.Advance();
                if (last.Kind == TokenKind.Operator)
                {
                    // The tokenizer reads `>>` as a single operator, in a type it closes two levels
                    depth += last.Value switch { "<" => 1, ">" => -1, ">>" => -2, _ => 0 };
                }
            } while (depth > 0);

            // Closed more levels than were opened, for example `A<B>>`
            if (depth < 0)
            {
                return false;
            }
        }

        if (Accumulator != null)
        {
            var type = new Token(first.Source, first.Offset, last.Offset + last.Length - first.Offset, first.Kind);
            accumulator = Accumulator(accumulator, type);
        }

        return true;
    }

    public override string ToString() => $"Type {Description}";
}
