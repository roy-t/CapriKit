using CapriKit.Generators.HLSL.Tokenizer;
using System.Diagnostics.CodeAnalysis;
using static CapriKit.Generators.HLSL.Parser.ParserUtils;

namespace CapriKit.Generators.HLSL.Parser;

internal static class EntryPointParser
{
    private static readonly Dictionary<string, EntryPointKind> EntryPointPragmas = new()
    {
        ["VertexShader"] = EntryPointKind.VertexShader,
        ["PixelShader"] = EntryPointKind.PixelShader,
        ["ComputeShader"] = EntryPointKind.ComputeShader,
    };

    /// <summary>
    /// Parses a HLSL entry-point, a function tagged by a #pragma directive.    
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-function-syntax"/>
    public static bool TryParse(ParseState state, [NotNullWhen(true)] out EntryPoint? entry)
    {
        entry = default;

        if (!state.Peek(TokenKind.Directive))
        {
            return false;
        }

        var directive = state.Peek();
        var parts = directive.Value.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || parts[0] != "#pragma" || !EntryPointPragmas.TryGetValue(parts[1], out var kind))
        {
            return false;
        }

        state.Advance();

        state.Match(TokenKind.Keyword, "inline");
        state.Match(TokenKind.Keyword, "precise");

        state.ExpectType();
        var name = state.ExpectIdentifier();

        SkipArgumentList(state);
        var semantic = SemanticParser.ParseSemantic(state);
        SkipMethodBlock(state);

        entry = new EntryPoint(kind, name, semantic);
        return true;
    }
}
