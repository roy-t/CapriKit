using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

public static class PragmaParser
{
    private static readonly Dictionary<string, EntryPointKind> EntryPointPragmas = new()
    {
        ["VertexShader"] = EntryPointKind.VertexShader,
        ["PixelShader"] = EntryPointKind.PixelShader,
        ["ComputeShader"] = EntryPointKind.ComputeShader,
    };

    public static bool TryParseEntryPoint(Token token, out EntryPointKind kind)
    {
        kind = default;
        if (token.Kind != TokenKind.Directive)
        {
            return false;
        }

        var parts = token.Value.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            && parts[0] == "#pragma"
            && EntryPointPragmas.TryGetValue(parts[1], out kind);
    }
}
