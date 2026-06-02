using CapriKit.Generators.HLSL.Tokenizer;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.Generators.HLSL.Parser;

internal static class EntryPointParser
{
    private static readonly Dictionary<string, EntryPointKind> EntryPointPragmas = new()
    {
        ["VertexShader"] = EntryPointKind.VertexShader,
        ["PixelShader"] = EntryPointKind.PixelShader,
        ["ComputeShader"] = EntryPointKind.ComputeShader,
    };

    private record EntryPointAccumulator
    {
        public EntryPointKind Kind { get; set; } = EntryPointKind.VertexShader;
        public string Name { get; set; } = string.Empty;
        public string Semantic { get; set; } = string.Empty;
    }

    /// <summary>
    /// Parses a HLSL entry-point, a function tagged by a #pragma directive.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-function-syntax"/>
    public static bool TryParse(ParseState state, [NotNullWhen(true)] out EntryPoint? entry)
    {
        var parser = new ParserBuilder<EntryPointAccumulator>()
            .Required(PragmaDirective, (a, t) => a with { Kind = GetKindFromPragma(t) })
            .RequiredPattern(FunctionParser.Create(), () => new FunctionParser.FunctionAccumulator(), (e, f) => e with { Name = f.Name, Semantic = f.Semantic });

        var accumulator = new EntryPointAccumulator();
        if (parser.TryParse(state, ref accumulator))
        {
            entry = new EntryPoint(accumulator.Kind, accumulator.Name, accumulator.Semantic);
            return true;
        }

        entry = default;
        return false;
    }

    private static bool PragmaDirective(Token directive)
    {
        var parts = directive.Value.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        return directive.Kind == TokenKind.Directive
            && parts.Length > 1
            && parts[0] == "#pragma"
            && EntryPointPragmas.ContainsKey(parts[1]);
    }

    private static EntryPointKind GetKindFromPragma(Token directive)
    {
        var parts = directive.Value.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        return EntryPointPragmas[parts[1]];
    }
}
