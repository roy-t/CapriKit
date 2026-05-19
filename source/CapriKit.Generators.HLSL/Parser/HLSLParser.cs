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
public record Variable(string Type, string Name, uint Register, IReadOnlyList<string> Modifiers);
public record Member(string Type, string Name, string Semantic, IReadOnlyList<string> Modifiers, IReadOnlyList<uint> Dimensions);
public record Structure(string Name, IReadOnlyList<Member> Members);
public record ConstantBuffer(string Name, uint Register, IReadOnlyList<Member> Members);
public record EntryPoint(EntryPointKind Kind, string Name, string Semantic);
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
            if (IncludeParser.TryParse(state, out var include))
            {
                includes.Add(include);
            }
            else if (StructureParser.TryParse(state, out var structure))
            {
                structures.Add(structure);
            }
            else if (ConstantBufferParser.TryParse(state, out var buffer))
            {
                constantBuffers.Add(buffer);
            }
            else if (EntryPointParser.TryParse(state, out var entry))
            {
                entryPoints.Add(entry);
            }
            else if (VariableParser.TryParse(state, out var variable))
            {
                variables.Add(variable);
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
