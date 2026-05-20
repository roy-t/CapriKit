using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Tokenizer;

internal class KeywordTokenizerTests
{
    [Test]
    public async Task ParseKeyword()
    {
        var input = "if;";
        var result = KeywordTokenizer.TryParse(input, 0, 2, out var token);
        await Assert.That(result).IsTrue();
        await Assert.That(token.Value).IsEqualTo("if");
        await Assert.That(token.Kind).IsEqualTo(TokenKind.Keyword);
    }

    [Test]
    public async Task ParseExpandedKeyword()
    {
        var input = "float2x2";
        var result = KeywordTokenizer.TryParse(input, 0, 8, out var token);
        await Assert.That(result).IsTrue();
        await Assert.That(token.Value).IsEqualTo("float2x2");
        await Assert.That(token.Kind).IsEqualTo(TokenKind.Keyword);
    }

    [Test]
    public async Task ParseKeywordAtOffset()
    {
        var input = """
            struct Data // sequential layout
            {
                float3 A : SV_SEMANTIC; 
                nointerpolation float B;
                float3 C;               
                float2 D;               
                float E[3][1]; // flattened to [3]
            };
            """;
        var result = KeywordTokenizer.TryParse(input, 71, 15, out var token);
        await Assert.That(result).IsTrue();
        await Assert.That(token.Value).IsEqualTo("nointerpolation");
        await Assert.That(token.Kind).IsEqualTo(TokenKind.Keyword);
    }

    [Test]
    public async Task DoNotExpandNonExpandableKeyword()
    {
        var input = "void2x2";
        var result = KeywordTokenizer.TryParse(input, 0, 7, out var token);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task DoNotParseIdentifierWithKeywordPrefix()
    {
        // for is a keyword but should not match since we expect length 7
        var input = "fortune";
        var result = KeywordTokenizer.TryParse(input, 0, 7, out var token);
        await Assert.That(result).IsFalse();
    }
}
