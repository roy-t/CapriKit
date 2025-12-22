using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace CapriKit.Tests.TestUtilities;

// Ideas from: https://github.com/StefH/FluentBuilder/blob/main/src-extensions/CSharp.SourceGenerators.Extensions/SourceGeneratorExtensions.cs
// and https://github.com/roy-t/MiniEngine3/blob/main/src/Generators/Mini.Engine.Generators.Debugger/Compiler.cs
internal static class SourceGeneratorExtensions
{
    internal record GeneratedFile(SourceText Source, string FileName);

    internal record SourceGeneratorResult(ImmutableArray<GeneratedFile> GeneratedFiles, ImmutableArray<Diagnostic> Diagnostics);
    
    public static SourceGeneratorResult Execute(this IIncrementalGenerator generator, string source, params ImmutableArray<AdditionalText> additionalTexts)
    {        
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        var assembly = GetRandomAssemblyName();
        var sourcePath = GetRandomPath();

        var syntaxTree = CSharpSyntaxTree.ParseText(source, null, sourcePath);
        var metadataReferences = MetadataReference.CreateFromFile(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create(assembly, [syntaxTree], [metadataReferences], options);

        driver = driver.AddAdditionalTexts(additionalTexts);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);        

        var generatedFiles = outputCompilation.SyntaxTrees
            .Where(st => st.FilePath != sourcePath)
            .Select(st => new GeneratedFile(st.GetText(), st.FilePath))
            .ToImmutableArray();

        return new SourceGeneratorResult(generatedFiles, diagnostics);
    }

    private static string GetRandomAssemblyName()
    {
        return $"CapriKit.Tests.GeneratedAssembly.G_{Guid.NewGuid().ToString().Replace("-", "")}";
    }

    private static string GetRandomPath()
    {
        return Path.Join("CapriKit.Tests.GeneratedFiles", Path.GetRandomFileName());
    }    
}
