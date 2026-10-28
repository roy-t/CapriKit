using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class VariableParserTests
{
    [Test]
    public async Task ParseSamplerWithRegister()
    {
        var state = new ParseState(HLSLTokenizer.Parse("sampler TextureSampler : register(s0);"));

        var success = VariableParser.TryParse(state, out var variable);

        await Assert.That(success).IsTrue();
        await Assert.That(variable!.Type).IsEqualTo("sampler");
        await Assert.That(variable.Name).IsEqualTo("TextureSampler");
        await Assert.That(variable.Register).IsEqualTo(0u);
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task ParseMinimalDeclaration()
    {
        var state = new ParseState(HLSLTokenizer.Parse("Texture2D Diffuse;"));

        var success = VariableParser.TryParse(state, out var variable);

        await Assert.That(success).IsTrue();
        await Assert.That(variable!.Type).IsEqualTo("Texture2D");
        await Assert.That(variable.Name).IsEqualTo("Diffuse");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task ParseWithStorageClassTypeModifierAndInitializer()
    {
        var state = new ParseState(HLSLTokenizer.Parse("static const float Pi = 3.14;"));

        var success = VariableParser.TryParse(state, out var variable);

        await Assert.That(success).IsTrue();
        await Assert.That(variable!.Type).IsEqualTo("float");
        await Assert.That(variable.Name).IsEqualTo("Pi");
        await Assert.That(variable.Modifiers).Count().IsEqualTo(2);
        await Assert.That(variable.Modifiers[0]).IsEqualTo("static");
        await Assert.That(variable.Modifiers[1]).IsEqualTo("const");
        await Assert.That(state.IsAtEnd).IsTrue();

    }

    [Test]
    public async Task ParseWithArraySize()
    {
        var state = new ParseState(HLSLTokenizer.Parse("float Values[4];"));

        var success = VariableParser.TryParse(state, out var variable);

        await Assert.That(success).IsTrue();
        await Assert.That(variable!.Type).IsEqualTo("float");
        await Assert.That(variable.Name).IsEqualTo("Values");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task ParseWithTypeArgument()
    {
        var state = new ParseState(HLSLTokenizer.Parse("StructuredBuffer<Material> Materials : register(t1);"));

        var success = VariableParser.TryParse(state, out var variable);

        await Assert.That(success).IsTrue();
        await Assert.That(variable!.Type).IsEqualTo("StructuredBuffer<Material>");
        await Assert.That(variable.Name).IsEqualTo("Materials");
        await Assert.That(variable.Register).IsEqualTo(1u);
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task ParseWithMultipleTypeArguments()
    {
        var state = new ParseState(HLSLTokenizer.Parse("Texture2DMS<float4, 8> Multisampled;"));

        var success = VariableParser.TryParse(state, out var variable);

        await Assert.That(success).IsTrue();
        await Assert.That(variable!.Type).IsEqualTo("Texture2DMS<float4, 8>");
        await Assert.That(variable.Name).IsEqualTo("Multisampled");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task ParseWithNestedTypeArguments()
    {
        // The closing `>>` is a single token
        var state = new ParseState(HLSLTokenizer.Parse("A<B, C<D>> Nested;"));

        var success = VariableParser.TryParse(state, out var variable);

        await Assert.That(success).IsTrue();
        await Assert.That(variable!.Type).IsEqualTo("A<B, C<D>>");
        await Assert.That(variable.Name).IsEqualTo("Nested");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task RejectsUnbalancedTypeArgumentsAndRewinds()
    {
        var state = new ParseState(HLSLTokenizer.Parse("A<B>> Broken;"));

        var success = VariableParser.TryParse(state, out _);

        await Assert.That(success).IsFalse();
        await Assert.That(state.Peek().Value).IsEqualTo("A");
    }

    [Test]
    public async Task RejectsFunctionDeclarationAndRewinds()
    {
        var state = new ParseState(HLSLTokenizer.Parse("void Main() { return; }"));

        var success = VariableParser.TryParse(state, out _);

        await Assert.That(success).IsFalse();
        await Assert.That(state.Peek().Value).IsEqualTo("void");
    }
}
