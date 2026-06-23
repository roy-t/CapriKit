using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Parser.Infrastructure;
using CapriKit.Generators.HLSL.Tokenizer;
using static CapriKit.Generators.HLSL.Parser.Infrastructure.ParserBuilderUtilities;

namespace CapriKit.Tests.Generators.HLSL.Parser.Infrastructure;

internal class ParserBuilderTests
{
    // Collects the value of every matched token so we can assert what was consumed.
    private static Accumulator<List<string>> Collect => (a, t) => { a.Add(t.Value); return a; };

    private static (bool Success, List<string> Values, ParseState State) Run(string source, ParserBuilder<List<string>> parser)
    {
        var state = new ParseState(HLSLTokenizer.Parse(source));
        var values = new List<string>();
        var success = parser.TryParse(state, ref values);
        return (success, values, state);
    }

    [Test]
    public async Task RequiredMatchesAccumulatesAndAdvances()
    {
        var parser = new ParserBuilder<List<string>>()
            .Required(AnyType, Collect);

        var (success, values, state) = Run("float", parser);

        await Assert.That(success).IsTrue();
        await Assert.That(values).Count().IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo("float");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task OptionalConsumesWhenPresent()
    {
        var parser = new ParserBuilder<List<string>>()
            .Optional(Keyword("const"), Collect);

        var (success, values, state) = Run("const", parser);

        await Assert.That(success).IsTrue();
        await Assert.That(values[0]).IsEqualTo("const");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task OptionalSucceedsAndConsumesNothingWhenAbsent()
    {
        var parser = new ParserBuilder<List<string>>()
            .Optional(Keyword("const"), Collect);

        var (success, values, state) = Run("Foo", parser);

        await Assert.That(success).IsTrue();
        await Assert.That(values).Count().IsEqualTo(0);
        await Assert.That(state.Peek().Value).IsEqualTo("Foo");
    }

    [Test]
    public async Task SequenceOfStepsParsesWholePattern()
    {
        var parser = new ParserBuilder<List<string>>()
            .Required(AnyType, Collect)
            .Required(AnyIdentifier, Collect)
            .Required(Operator(";"));

        var (success, values, state) = Run("float Foo;", parser);

        await Assert.That(success).IsTrue();
        await Assert.That(values[0]).IsEqualTo("float");
        await Assert.That(values[1]).IsEqualTo("Foo");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task FailingStepRewindsCursorToStart()
    {
        var parser = new ParserBuilder<List<string>>()
            .Required(AnyType)
            .Required(Operator(";")); // fails: an identifier follows, not ';'

        var (success, _, state) = Run("float Foo", parser);

        await Assert.That(success).IsFalse();
        await Assert.That(state.Peek().Value).IsEqualTo("float");
    }

    [Test]
    public async Task SkipToStopsBeforeTargetToken()
    {
        var parser = new ParserBuilder<List<string>>()
            .Required(Operator("="))
            .SkipToBefore(Operator(";"));

        var (success, _, state) = Run("= 1 + 2;", parser);

        await Assert.That(success).IsTrue();
        await Assert.That(state.Peek().Value).IsEqualTo(";");
    }

    [Test]
    public async Task RequiredBlockConsumesBalancedNestedBlock()
    {
        var parser = new ParserBuilder<List<string>>()
            .RequiredBlock(Operator("{"), Operator("}"));

        var (success, _, state) = Run("{ a { b } c } tail", parser);

        await Assert.That(success).IsTrue();
        await Assert.That(state.Peek().Value).IsEqualTo("tail");
    }

    [Test]
    public async Task OptionalPatternConsumesSubPatternWhenPresent()
    {
        var semantic = new ParserBuilder<List<string>>()
            .Required(Operator(":"))
            .Required(AnyIdentifier, Collect);

        var parser = new ParserBuilder<List<string>>()
            .OptionalPattern(semantic);

        var (success, values, state) = Run(": SV_Position rest", parser);

        await Assert.That(success).IsTrue();
        await Assert.That(values[0]).IsEqualTo("SV_Position");
        await Assert.That(state.Peek().Value).IsEqualTo("rest");
    }

    [Test]
    public async Task OptionalPatternSucceedsAndConsumesNothingWhenAbsent()
    {
        var semantic = new ParserBuilder<List<string>>()
            .Required(Operator(":"))
            .Required(AnyIdentifier, Collect);

        var parser = new ParserBuilder<List<string>>()
            .OptionalPattern(semantic);

        var (success, values, state) = Run("Foo", parser);

        await Assert.That(success).IsTrue();
        await Assert.That(values).Count().IsEqualTo(0);
        await Assert.That(state.Peek().Value).IsEqualTo("Foo");
    }

    [Test]
    public async Task RequiredPatternMergesChildAccumulatorIntoParent()
    {
        // Child parses to a string, which is then merged into the parent's list.
        var child = new ParserBuilder<string>()
            .Required(Operator("("))
            .Required(AnyIdentifier, (_, t) => t.Value)
            .Required(Operator(")"));

        var parser = new ParserBuilder<List<string>>()
            .RequiredPattern(child, () => string.Empty, (parent, c) => { parent.Add(c); return parent; });

        var (success, values, state) = Run("( Inner )", parser);

        await Assert.That(success).IsTrue();
        await Assert.That(values[0]).IsEqualTo("Inner");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task RepeatConsumesMatchingTokensUntilMismatch()
    {
        var parser = new ParserBuilder<List<string>>()
            .Repeat(AnyModifier, Collect);

        var (success, values, state) = Run("static const precise float", parser);

        await Assert.That(success).IsTrue();
        await Assert.That(values).Count().IsEqualTo(3);
        await Assert.That(values[0]).IsEqualTo("static");
        await Assert.That(values[2]).IsEqualTo("precise");
        await Assert.That(state.Peek().Value).IsEqualTo("float");
    }

    [Test]
    public async Task RepeatSucceedsWithZeroMatches()
    {
        var parser = new ParserBuilder<List<string>>()
            .Repeat(AnyModifier, Collect);

        var (success, values, state) = Run("float", parser);

        await Assert.That(success).IsTrue();
        await Assert.That(values).Count().IsEqualTo(0);
        await Assert.That(state.Peek().Value).IsEqualTo("float");
    }
}
