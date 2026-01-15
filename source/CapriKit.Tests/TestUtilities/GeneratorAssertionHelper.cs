using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Mono.Cecil;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Core;

namespace CapriKit.Tests.TestUtilities;

internal record SourceGenerator<T>() where T : IIncrementalGenerator, new();

internal sealed record SourceGeneratorWithInputs<T>(SourceGeneratorInput Input) : SourceGenerator<T> where T : IIncrementalGenerator, new();

internal static class SourceGenerator
{
    public static SourceGenerator<T> OfType<T>()
        where T : IIncrementalGenerator, new()
    {
        return new SourceGenerator<T>();
    }
}

internal sealed record class SourceGeneratorInput(IEnumerable<(string fileName, SourceText content)> Sources, IEnumerable<(string fileName, SourceText content)> AdditionalFiles);

internal sealed record class SourceGeneratorOutput(IEnumerable<(string fileName, SourceText content)> GeneratedSources, IEnumerable<DiagnosticResult> ExpectedDiagnostics);

internal record class InputFiles(
    IEnumerable<(string fileName, SourceText content)> SourceFiles,
    IEnumerable<(string fileName, SourceText content)> AdditionalFiles);

internal sealed class SourceGeneratorGeneratesFilesAssertion<T> : Assertion<SourceGenerator<T>>
    where T : IIncrementalGenerator, new()
{
    private readonly SourceGeneratorOutput Expected;

    public SourceGeneratorGeneratesFilesAssertion(
        AssertionContext<SourceGenerator<T>> context,
        SourceGeneratorOutput expected)
        : base(context)
    {
        Expected = expected;
    }

    protected override string GetExpectation() => "to generate expected files";

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<SourceGenerator<T>> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return AssertionResult.Failed($"threw {exception.GetType().Name}");

        if (value == null)
            return AssertionResult.Failed("value was null");


        await GeneratorAssertionHelper.Run<T>(Expected);
        return AssertionResult.Passed;
    }
}

internal static class SourceGeneratorAssertionExtensions
{
    public static IAssertionSource<SourceGenerator<T>> WithAdditionalFiles<T>(
        this IAssertionSource<SourceGenerator<T>> source)
        where T : IIncrementalGenerator, new()
    {
        // TODO: somehow access the generator in the IAssertionSource and add context
        var value = source.Context.Map(AddFiles);
        
    }

    private static SourceGenerator<T>? AddFiles<T>(SourceGenerator<T>? original)
        where T : IIncrementalGenerator, new()
    {
        return original;
    }

    public static SourceGeneratorGeneratesFilesAssertion<T> GeneratesFiles<T>(
        this IAssertionSource<SourceGenerator<T>> source,
        SourceGeneratorOutput expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null
        )
        where T : IIncrementalGenerator, new()
    {
        source.Context.ExpressionBuilder.Append($".GeneratesFiles({expression})");
        return new SourceGeneratorGeneratesFilesAssertion<T>(source.Context, expected);
    }
}


internal sealed class GeneratorAssertion<T>(
    AssertionContext<IEnumerable<(string fileName, SourceText content)>> context,
    IEnumerable<(string fileName, SourceText content)> expected)
    : Assertion<IEnumerable<(string fileName, SourceText content)>>(context)
    where T : IIncrementalGenerator, new()
{
    protected override string GetExpectation() => "to generate expected files";

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<IEnumerable<(string fileName, SourceText content)>> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return AssertionResult.Failed($"threw {exception.GetType().Name}");

        if (value == null)
            return AssertionResult.Failed("value was null");


        await GeneratorAssertionHelper.Run<T>(value, expected);
        return AssertionResult.Passed;
    }
}

internal static class GeneratorAssertionExtension
{
    public static GeneratorAssertion<T> GeneratesFiles<T>(
        this IAssertionSource<IEnumerable<(string fileName, SourceText content)>> source,
        IEnumerable<(string fileName, SourceText content)> expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null
        )
        where T : IIncrementalGenerator, new()
    {
        source.Context.ExpressionBuilder.Append($".GeneratesFiles({expression})");
        return new GeneratorAssertion<T>(source.Context, expected);
    }
}


internal static class GeneratorAssertionHelper
{
    public static async Task Run<T>(IEnumerable<(string fileName, SourceText content)> inputFiles, IEnumerable<(string fileName, SourceText content)> generatedFiles)
        where T : IIncrementalGenerator, new()
    {
        var harness = new CSharpSourceGeneratorTest<T, DefaultVerifier>();
        harness.TestState.Sources.AddRange(inputFiles);
        harness.TestState.GeneratedSources.AddRange(generatedFiles);
        await harness.RunAsync();
    }


    public static async Task Run<T>(SourceGeneratorOutput output)
        where T : IIncrementalGenerator, new()
    {
        var harness = new CSharpSourceGeneratorTest<T, DefaultVerifier>();
        harness.TestState.GeneratedSources.AddRange(output.GeneratedSources);
        harness.TestState.ExpectedDiagnostics.AddRange(output.ExpectedDiagnostics);
        await harness.RunAsync();
    }

    public static async Task Run<T>(SourceGeneratorInput input, SourceGeneratorOutput output)
       where T : IIncrementalGenerator, new()
    {
        var harness = new CSharpSourceGeneratorTest<T, DefaultVerifier>();
        harness.TestState.Sources.AddRange(input.Sources);
        harness.TestState.AdditionalFiles.AddRange(input.AdditionalFiles);
        harness.TestState.GeneratedSources.AddRange(output.GeneratedSources);
        harness.TestState.ExpectedDiagnostics.AddRange(output.ExpectedDiagnostics);
        await harness.RunAsync();
    }
}
