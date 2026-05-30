using CapriKit.Generators.HLSL.Tokenizer;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.Generators.HLSL.Parser;

internal static class IncludeParser
{
    private record IncludeAccumulator
    {
        public IncludeKind Kind { get; set; } = IncludeKind.Local;
        public string Path { get; set; } = string.Empty;
    }

    /// <summary>
    /// Parses an include directive
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-reference"/>
    public static bool TryParse(ParseState state, [NotNullWhen(true)] out Include? include)
    {
        // A full include such as `#include "types.hlsl"`
        var parser = new ParserBuilder<IncludeAccumulator>()
            .Required(IncludeDirective, (a, t) => a with { Kind = GetIncludeKind(t), Path = GetIncludePath(t) });

        var accumulator = new IncludeAccumulator();
        if (parser.TryParse(state, ref accumulator))
        {
            include = new Include(accumulator.Path, accumulator.Kind);
            return true;
        }

        include = default;
        return false;
    }

    private static bool IncludeDirective(Token directive)
    {
        var parts = SplitDirective(directive);
        return directive.Kind == TokenKind.Directive
            && parts.Length > 1
            && parts[0] == "#include";
    }

    private static IncludeKind GetIncludeKind(Token directive)
    {
        var parts = SplitDirective(directive);
        if (parts[1].StartsWith("<") && parts[1].EndsWith(">"))
        {
            return IncludeKind.System;
        }
        else if (parts[1].StartsWith("\"") && parts[1].EndsWith("\""))
        {
            return IncludeKind.Local;
        }

        throw new Exception($"Malformed #include directive: {directive.Value}");
    }

    private static string GetIncludePath(Token directive)
    {
        var parts = SplitDirective(directive);
        return parts[1].Substring(1, parts[1].Length - 2);
    }

    private static string[] SplitDirective(Token directive)
    {
        return directive.Value.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
    }
}
