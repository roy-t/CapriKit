using CapriKit.Generators.HLSL.Parser.Infrastructure;
using CapriKit.Generators.HLSL.Tokenizer;
using System.Diagnostics.CodeAnalysis;
using static CapriKit.Generators.HLSL.Parser.Infrastructure.ParserBuilderUtilities;

namespace CapriKit.Generators.HLSL.Parser;

internal static class FunctionParser
{
    private static FunctionKind StagePragmas(string name) => name switch
    {
        "VertexShader" => FunctionKind.VertexShader,
        "PixelShader" => FunctionKind.PixelShader,
        "ComputeShader" => FunctionKind.ComputeShader,
        _ => FunctionKind.Function
    };


    internal record FunctionAccumulator
    {
        public FunctionKind Kind { get; set; } = FunctionKind.Function;
        public string Name { get; set; } = string.Empty;
        public string Semantic { get; set; } = string.Empty;
        public List<Argument> Arguments { get; set; } = [];
    }

    /// <summary>
    /// Parses a HLSL function. Functions tagged with a shader-stage <c>#pragma</c> are entry points
    /// and carry the matching <see cref="FunctionKind"/>, all other functions have no kind.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-function-syntax"/>
    public static bool TryParse(ParseState state, [NotNullWhen(true)] out Function? function)
    {
        var parser = Create();

        var accumulator = new FunctionAccumulator();
        if (parser.TryParse(state, ref accumulator))
        {
            function = new Function(accumulator.Kind, accumulator.Name, accumulator.Semantic, accumulator.Arguments);
            return true;
        }

        function = default;
        return false;
    }

    private static ParserBuilder<FunctionAccumulator> Create()
    {
        return new ParserBuilder<FunctionAccumulator>()
            .Repeat(AnyPragma, (a, t) => a with { Kind = GetStage(t) })
            .Optional(Keyword("inline"))
            .Optional(Keyword("precise"))
            .Required(AnyType)
            .Required(AnyIdentifier, (a, t) => a with { Name = t.Value })
            .Required(Operator("("))
            .RequiredPattern(ArgumentParser.CreateListParser(), () => [], (a, arguments) => { a.Arguments.AddRange(arguments); return a; })
            .Required(Operator(")"))
            .OptionalSemantic((a, t) => a with { Semantic = t.Value })
            .RequiredBlock(Operator("{"), Operator("}"));
    }

    private static FunctionKind GetStage(Token pragma)
    {
        TryGetPragmaName(pragma, out var name);
        return StagePragmas(name);
    }
}
