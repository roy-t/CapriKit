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

    public static bool TryParseInclude(Token token, out string include)
    {
        include = string.Empty;
        if (token.Kind != TokenKind.Directive)
        {
            return false;
        }

        var parts = token.Value.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        //#include "../Includes/Defines.hlsl"

        if (parts.Length >= 2 && parts[0] == "#include")
        {
            include = ExtractIncludePath(token.Value);
            return true;
        }

        return false;
    }

    private static string ExtractIncludePath(string include)
    {
        if (include.Contains('<') && include.Contains('>'))
        {
            return ExtractSystemIncludePath(include);
        }

        return ExtractLocalIncludePath(include);
    }

    private static string ExtractSystemIncludePath(string include)
    {
        var start = 0;
        var end = 0;
        for (var i = 0; i < include.Length; i++)
        {
            if (include[i] == '<')
            {
                start = i;
            }

            if (include[i] == '>')
            {
                end = i;
                break;
            }
        }

        return include.Substring(start, end - (start + 1));
    }

    private static string ExtractLocalIncludePath(string include)
    {
        var start = 0;
        var end = 0;
        for (var i = 0; i < include.Length; i++)
        {
            if (include[i] == '"')
            {
                start = i;
            }

            if (i > start && include[i] == '"')
            {
                end = i;
                break;
            }
        }

        if (start <= end)
        {
            throw new Exception($"Could not find local include path in string {include}");
        }

        return include.Substring(start, end - (start + 1));
    }
}
