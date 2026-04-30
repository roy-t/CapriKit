using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

public static class IncludeParser
{
    public const string IncludeDirective = "#include";

    public static Include Parse(ParseState state)
    {
        var token = state.Advance();
        if (token.Kind != TokenKind.Directive || !token.Value.StartsWith(IncludeDirective))
        {
            throw new Exception($"Expected include directive but got {token.Kind} '{token.Value}'.");
        }

        if (token.Value.Length <= IncludeDirective.Length)
        {
            throw new Exception($"Include directive is missing an argument");
        }

        var argument = token.Value.Substring(IncludeDirective.Length).Trim();

        if (argument.StartsWith("<") && argument.EndsWith(">"))
        {
            argument = argument.Substring(1, argument.Length - 2);
            return new Include(argument, IncludeKind.System);
        }

        if (argument.StartsWith("\"") && argument.EndsWith("\""))
        {
            argument = argument.Substring(1, argument.Length - 2);
            return new Include(argument, IncludeKind.Local);
        }

        throw new Exception($"Could not parse #include argument: {argument}");
    }
}
