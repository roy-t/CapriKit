using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

internal enum FunctionKind
{
    Function,
    VertexShader,
    PixelShader,
    ComputeShader,
}

internal enum StructureKind
{
    Structure,
    VertexShaderInput,
}

internal enum IncludeKind
{
    Local,
    System
}

internal sealed record Include(string Path, IncludeKind Kind);
internal sealed record Variable(string Type, string Name, uint Register, IReadOnlyList<string> Modifiers, IReadOnlyList<uint> Dimensions);
internal sealed record Member(string Type, string Name, string Semantic, IReadOnlyList<string> Modifiers, IReadOnlyList<uint> Dimensions);
internal sealed record Argument(string Type, string Name, string Semantic, IReadOnlyList<string> Modifiers, IReadOnlyList<uint> Dimensions);
internal sealed record Structure(string Name, IReadOnlyList<Member> Members, StructureKind Kind);
internal sealed record ConstantBuffer(string Name, uint Register, IReadOnlyList<Member> Members);
internal sealed record Function(FunctionKind Kind, string Name, string Semantic, IReadOnlyList<Argument> Arguments);
internal sealed record ShaderMetadata(IReadOnlyList<Include> Includes, IReadOnlyList<Variable> Variables, IReadOnlyList<Structure> Structures, IReadOnlyList<ConstantBuffer> ConstantBuffers, IReadOnlyList<Function> Functions);

internal static class HLSLParser
{
    public static ShaderMetadata Parse(IReadOnlyList<Token> tokens)
    {
        var state = new ParseState(tokens);
        var variables = new List<Variable>();
        var structures = new List<Structure>();
        var constantBuffers = new List<ConstantBuffer>();
        var functions = new List<Function>();
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
            else if (FunctionParser.TryParse(state, out var function))
            {
                functions.Add(function);
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

        return new ShaderMetadata(includes, variables, structures, constantBuffers, functions);
    }
}
