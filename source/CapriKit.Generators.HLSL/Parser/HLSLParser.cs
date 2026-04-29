using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

public enum EntryPointKind
{
    VertexShader,
    PixelShader,
    ComputeShader,
}

public record Field(string Type, string Name, string Semantic);
public record EntryPoint(EntryPointKind Kind, string Name, string Semantic);
public record Structure(string Name, IReadOnlyList<Field> Fields);
public record ConstantBuffer(string Name, string Register, IReadOnlyList<Field> Fields);
public record ShaderMetadata(IReadOnlyList<Structure> Structures, IReadOnlyList<ConstantBuffer> ConstantBuffers, IReadOnlyList<EntryPoint> EntryPoints);

public static class HLSLParser
{
    public static ShaderMetadata Parse(IReadOnlyList<Token> tokens)
    {
        var state = new ParseState(tokens);
        var structures = new List<Structure>();
        var constantBuffers = new List<ConstantBuffer>();
        var entryPoints = new List<EntryPoint>();

        while (!state.IsAtEnd)
        {
            if (state.Peek(TokenKind.Keyword, "struct"))
            {
                structures.Add(StructureParser.Parse(state));
            }
            else if (state.Peek(TokenKind.Keyword, "cbuffer"))
            {
                constantBuffers.Add(ConstantBufferParser.Parse(state));
            }
            else if (PragmaParser.TryParseEntryPoint(state.Peek(), out var kind))
            {
                state.Advance();
                entryPoints.Add(EntryPointParser.Parse(state, kind));
            }
            else
            {
                state.Advance();
            }
        }

        return new ShaderMetadata(structures, constantBuffers, entryPoints);
    }
}
