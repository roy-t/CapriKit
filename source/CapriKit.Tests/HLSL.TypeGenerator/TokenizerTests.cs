using CapriKit.HLSL.TypeGenerator.Parsers;
using TUnit.Assertions.Enums;

namespace CapriKit.Tests.HLSL.TypeGenerator;

internal class TokenizerTests
{
    [Test]
    public async Task ParseWhiteSpace()
    {
        var tokenizer = new Tokenizer();
        var input = """
                
                    
                    
            """;

        var tokens = tokenizer.Parse(input);
        await Assert.That(tokens.Count).IsZero();
    }

    [Test]
    public async Task ParseSimpleToken()
    {
        var tokenizer = new Tokenizer();
        var input = """
            struct {
                
            };
            """;

        var tokens = tokenizer.Parse(input);
        await Assert.That(tokens.Select(x => x.Kind))
            .IsEquivalentTo([
                TokenKind.Struct,
                TokenKind.LeftCurlyBracket,
                TokenKind.RightCurlyBracket,
                TokenKind.SemiColon
                ], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ParseLineComment()
    {
        var tokenizer = new Tokenizer();
        var input = """
            // Hello World
            ;
            """;

        var tokens = tokenizer.Parse(input);
        await Assert.That(tokens.Select(x => x.Kind))
            .IsEquivalentTo([
                TokenKind.LineComment,
                TokenKind.SemiColon,
                ], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ParseBlockComment()
    {
        var tokenizer = new Tokenizer();
        var input = """
            /*
             * Hello
             * World
             */
            ;
            """;

        var tokens = tokenizer.Parse(input);
        await Assert.That(tokens.Select(x => x.Kind))
            .IsEquivalentTo([
                TokenKind.BlockComment,
                TokenKind.SemiColon,
                ], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ParseNumbers()
    {
        var tokenizer = new Tokenizer();
        var input = """
            .123
            .123e4
            .123e-4
            .123e+4f
            0.123e+4f
            1
            123
            123e4
            123e-4
            123e-4f
            ;
            """;

        var tokens = tokenizer.Parse(input);
        await Assert.That(tokens.Select(x => x.Kind))
            .IsEquivalentTo([
                TokenKind.Number,
                TokenKind.Number,
                TokenKind.Number,
                TokenKind.Number, TokenKind.NumberSuffix,
                TokenKind.Number,
                TokenKind.Number,
                TokenKind.Number,
                TokenKind.Number,
                TokenKind.SemiColon,
        ], CollectionOrdering.Matching);
    }

    private static string LineShader = """
        struct VS_INPUT
        {
            float3 position : POSITION;
        };

        struct PS_INPUT
        {
            float4 position : SV_POSITION;    
        };

        struct OUTPUT
        {
            float4 color : SV_Target0;
        };

        cbuffer Constants : register(b0)
        {    
            float4x4 WorldViewProjection; 
            float4 Color;
        };

        #pragma VertexShader
        PS_INPUT VS(VS_INPUT input, uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
        {
            PS_INPUT output;

            float4 position = float4(input.position, 1.0f);
            output.position = mul(WorldViewProjection, position);

            return output;
        }

        #pragma PixelShader
        OUTPUT PS(PS_INPUT input)
        {
            OUTPUT output;

            output.color = ToLinear(Color);
            return output;
        }
        """;
}
