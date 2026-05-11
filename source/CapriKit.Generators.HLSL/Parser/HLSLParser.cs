using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

public enum EntryPointKind
{
    VertexShader,
    PixelShader,
    ComputeShader,
}
public enum IncludeKind
{
    Local,
    System
}

public record Include(string Path, IncludeKind Kind);

public record Variable(string Type, string Name, int Register);
public record Member(string Type, string Name, string Semantic);
public record EntryPoint(EntryPointKind Kind, string Name, string Semantic);
public record Structure(string Name, IReadOnlyList<Member> Members);
public record ConstantBuffer(string Name, string Register, IReadOnlyList<Member> Members);
public record ShaderMetadata(IReadOnlyList<Include> Includes, IReadOnlyList<Variable> Variables, IReadOnlyList<Structure> Structures, IReadOnlyList<ConstantBuffer> ConstantBuffers, IReadOnlyList<EntryPoint> EntryPoints);

public static class HLSLParser
{
    public static ShaderMetadata Parse(IReadOnlyList<Token> tokens)
    {
        var state = new ParseState(tokens);
        var variables = new List<Variable>();
        var structures = new List<Structure>();
        var constantBuffers = new List<ConstantBuffer>();
        var entryPoints = new List<EntryPoint>();
        var includes = new List<Include>();

        while (!state.IsAtEnd)
        {
            if (DirectiveParser.TryParseInclude(state.Peek()))
            {
                includes.Add(IncludeParser.Parse(state));
            }
            else if (state.Peek(TokenKind.Keyword, "struct"))
            {
                structures.Add(StructureParser.Parse(state));
            }
            else if (state.Peek(TokenKind.Keyword, "cbuffer"))
            {
                constantBuffers.Add(ConstantBufferParser.Parse(state));
            }
            else if (DirectiveParser.TryParseEntryPoint(state.Peek(), out var kind))
            {
                state.Advance();
                entryPoints.Add(EntryPointParser.Parse(state, kind));
            }
            else
            {
                // Since this is an incomplete parser silently advance
                // past tokens we cannot parse.
                state.Advance();
            }
        }

        return new ShaderMetadata(includes, variables, structures, constantBuffers, entryPoints);
    }
}
