using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace CapriKit.Tests.TestUtilities;

internal sealed record GeneratorSubject<T>(SourceGeneratorInput Input)
    where T : IIncrementalGenerator, new()
{
    public GeneratorSubject<T> MergeInputs(SourceGeneratorInput additionalInputs)
    {
        var merged = Input.Merge(additionalInputs);
        return new GeneratorSubject<T>(merged);
    }
}

internal static class GeneratorSubject
{
    public static GeneratorSubject<T> OfType<T>()
        where T : IIncrementalGenerator, new()
    {
        return new GeneratorSubject<T>(SourceGeneratorInput.Empty);
    }
}

internal sealed record class SourceGeneratorInput(IEnumerable<(string fileName, SourceText content)> Sources, IEnumerable<(string fileName, SourceText content)> AdditionalFiles)
{
    public static readonly SourceGeneratorInput Empty = new([], []);

    public SourceGeneratorInput Merge(SourceGeneratorInput other)
    {
        List<(string fileName, SourceText content)> sources = [.. Sources, .. other.Sources];
        List<(string fileName, SourceText content)> additionalFiles = [.. AdditionalFiles, .. other.AdditionalFiles];

        return new SourceGeneratorInput(sources, additionalFiles);
    }
}

internal sealed record class SourceGeneratorOutput(IEnumerable<(string hintName, SourceText content)> GeneratedSources, IEnumerable<DiagnosticResult> ExpectedDiagnostics)
{
    public static readonly SourceGeneratorOutput Empty = new([], []);

    public SourceGeneratorOutput Merge(SourceGeneratorOutput other)
    {
        IEnumerable<(string hintName, SourceText content)> generatedSources = [.. GeneratedSources, .. other.GeneratedSources];
        IEnumerable<DiagnosticResult> expectedDiagnostics = [.. ExpectedDiagnostics, .. other.ExpectedDiagnostics];

        return new SourceGeneratorOutput(generatedSources, expectedDiagnostics);
    }
}


internal sealed class SourceGeneratorAssertion<T> : Assertion<GeneratorSubject<T>>
    where T : IIncrementalGenerator, new()
{
    private readonly SourceGeneratorOutput Expected;

    public SourceGeneratorAssertion(
        AssertionContext<GeneratorSubject<T>> context,
        SourceGeneratorOutput expected)
        : base(context)
    {
        Expected = expected;
    }

    protected override string GetExpectation() => $"to generate {Expected.GeneratedSources.Count()} file(s) and {Expected.ExpectedDiagnostics.Count()} diagnostic(s)";

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<GeneratorSubject<T>> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return AssertionResult.Failed($"threw {exception.GetType().Name}");

        if (value == null)
            return AssertionResult.Failed("value was null");

        await Run(value.Input, Expected);
        return AssertionResult.Passed;
    }

    private static async Task Run(SourceGeneratorInput input, SourceGeneratorOutput output)
    {
        var harness = new CSharpSourceGeneratorTest<T, DefaultVerifier>();
        harness.TestState.Sources.AddRange(input.Sources);
        harness.TestState.AdditionalFiles.AddRange(input.AdditionalFiles);
        harness.TestState.ExpectedDiagnostics.AddRange(output.ExpectedDiagnostics);

        // Ensure users only have to provide the hint name. The harness generates
        // a fake output path based on the generator type and hint name.
        foreach (var (hintName, content) in output.GeneratedSources)
        {
            harness.TestState.GeneratedSources.Add((typeof(T), hintName, content));
        }

        await harness.RunAsync();
    }
}

internal static class SourceGeneratorAssertionExtensions
{
    public static IAssertionSource<GeneratorSubject<T>> WithInput<T>(
    this IAssertionSource<GeneratorSubject<T>> source,
    SourceGeneratorInput input,
    bool addEmbeddedAttributeDefinition = false)
    where T : IIncrementalGenerator, new()
    {
        if (addEmbeddedAttributeDefinition)
        {
            input = new SourceGeneratorInput([EmbeddedAttributeDefinition.SourceFile], []).Merge(input);
        }
        var value = source.Context.Map(generator => generator?.MergeInputs(input));
        return new AssertionSourceAdapter<GeneratorSubject<T>>(value);
    }

    public static IAssertionSource<GeneratorSubject<T>> WithSources<T>(
        this IAssertionSource<GeneratorSubject<T>> source,
        params IEnumerable<(string fileName, SourceText content)> sources)
        where T : IIncrementalGenerator, new()
    {
        var input = new SourceGeneratorInput(sources, []);
        return WithInput(source, input);
    }

    public static IAssertionSource<GeneratorSubject<T>> WithSources<T>(
    this IAssertionSource<GeneratorSubject<T>> source,
    IEnumerable<(string fileName, SourceText content)> sources,
    bool addEmbeddedAttributeDefinition = false)
    where T : IIncrementalGenerator, new()
    {
        var input = new SourceGeneratorInput(sources, []);
        return WithInput(source, input, addEmbeddedAttributeDefinition);
    }

    public static IAssertionSource<GeneratorSubject<T>> WithAdditionalFiles<T>(
        this IAssertionSource<GeneratorSubject<T>> source,
        params IEnumerable<(string fileName, SourceText content)> additionalFiles)
        where T : IIncrementalGenerator, new()
    {
        var input = new SourceGeneratorInput([], additionalFiles);
        return WithInput(source, input);
    }

    public static IAssertionSource<GeneratorSubject<T>> WithAdditionalFiles<T>(
       this IAssertionSource<GeneratorSubject<T>> source,
       IEnumerable<(string fileName, SourceText content)> additionalFiles,
       bool addEmbeddedAttributeDefinition = false)
       where T : IIncrementalGenerator, new()
    {
        var input = new SourceGeneratorInput([], additionalFiles);
        return WithInput(source, input, addEmbeddedAttributeDefinition);
    }

    public static SourceGeneratorAssertion<T> Generates<T>(
        this IAssertionSource<GeneratorSubject<T>> source,
        SourceGeneratorOutput expected,
        bool expectEmbeddedAttributeDefinition = false)
        where T : IIncrementalGenerator, new()
    {
        source.Context.ExpressionBuilder.Append(".GeneratesFiles(...)");
        if (expectEmbeddedAttributeDefinition)
        {
            expected = new SourceGeneratorOutput([EmbeddedAttributeDefinition.SourceFile], []).Merge(expected);
        }
        return new SourceGeneratorAssertion<T>(source.Context, expected);
    }

    public static SourceGeneratorAssertion<T> Generates<T>(
        this IAssertionSource<GeneratorSubject<T>> source,
        IEnumerable<(string hintName, SourceText content)> generatedSources, IEnumerable<DiagnosticResult>? expectedDiagnostics = null, bool addEmbeddedAttributeDefinition = false)
        where T : IIncrementalGenerator, new()
    {
        var expected = new SourceGeneratorOutput(generatedSources, expectedDiagnostics ?? []);
        return Generates(source, expected, addEmbeddedAttributeDefinition);
    }
}

internal static class EmbeddedAttributeDefinition
{
    private static readonly string Path = @"Microsoft.CodeAnalysis.EmbeddedAttribute.cs";
    private static readonly SourceText Text = SourceText.From("""
            // <auto-generated/>
            namespace Microsoft.CodeAnalysis
            {
                internal sealed partial class EmbeddedAttribute : global::System.Attribute
                {
                }
            }
            """, Encoding.UTF8);

    public static readonly (string fileName, SourceText content) SourceFile = new(Path, Text);
}
