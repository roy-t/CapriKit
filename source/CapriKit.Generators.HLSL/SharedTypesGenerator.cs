using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CapriKit.Generators.HLSL;

[Generator]
internal sealed class SharedTypesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context =>
        {
            context.AddEmbeddedAttributeDefinition();
            var definitions = """
            using System;
            using Microsoft.CodeAnalysis;

            namespace CapriKit.Generators.HLSL;            
            public interface IShaderMetadata
            {
                
            }
            """;


            var source = SourceText.From(definitions, Encoding.UTF8);
            context.AddSource("Definitions.g.cs", source);
        });
    }
}
