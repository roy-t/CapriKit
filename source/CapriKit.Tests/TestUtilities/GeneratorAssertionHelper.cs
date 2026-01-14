using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Core;

namespace CapriKit.Tests.TestUtilities;

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
}
