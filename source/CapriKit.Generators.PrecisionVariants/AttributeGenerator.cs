using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CapriKit.Generators.PrecisionVariants;

[Generator]
public class AttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    { 
        context.RegisterPostInitializationOutput(static postInitializationContext =>
        {
            postInitializationContext.AddEmbeddedAttributeDefinition();
            var builder = new StringBuilder();
            builder.AppendLine("""
            using System;
            using Microsoft.CodeAnalysis;

            namespace CapriKit.PrecisionVariants
            {
            """);

            string[] types = ["SByte", "Byte", "Short", "UShort", "Int", "UInt", "Long", "ULong", "NInt", "NUInt", "Float", "Double", "Decimal"];
            foreach (var t in types)
            {
                builder.AppendLine($$"""
                    [Embedded]
                    [AttributeUsage(AttributeTargets.Method)]
                    internal sealed class Generate{{t}}Variant : Attribute { }

                """);
            }

            builder.AppendLine("""
            }
            """);

            var source = SourceText.From(builder.ToString(), Encoding.UTF8);
            postInitializationContext.AddSource("GeneratePrecisionVariantAttributes.g.cs", source);
        });
    }
}
