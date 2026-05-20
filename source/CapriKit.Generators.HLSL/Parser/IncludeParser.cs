using CapriKit.Generators.HLSL.Tokenizer;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.Generators.HLSL.Parser;

internal static class IncludeParser
{
    private const string IncludeDirective = "#include";

    /// <summary>
    /// Parses an include directive
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-reference"/>
    public static bool TryParse(ParseState state, [NotNullWhen(true)] out Include? include)
    {
        include = default;

        var token = state.Peek();
        if (token.Kind != TokenKind.Directive || !token.Value.StartsWith(IncludeDirective))
        {
            return false;
        }
        state.Advance();

        if (token.Value.Length <= IncludeDirective.Length)
        {
            throw new Exception("Include directive is missing an argument");
        }

        var argument = token.Value.Substring(IncludeDirective.Length).Trim();

        if (argument.StartsWith("<") && argument.EndsWith(">"))
        {
            include = new Include(argument.Substring(1, argument.Length - 2), IncludeKind.System);
            return true;
        }

        if (argument.StartsWith("\"") && argument.EndsWith("\""))
        {
            include = new Include(argument.Substring(1, argument.Length - 2), IncludeKind.Local);
            return true;
        }

        throw new Exception($"Could not parse #include argument: {argument}");
    }
}
