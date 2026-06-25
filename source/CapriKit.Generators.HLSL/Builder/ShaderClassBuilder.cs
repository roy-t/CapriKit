using CapriKit.Generators.HLSL.Parser;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using static CapriKit.Generators.HLSL.Builder.SourceCodeUtils;

namespace CapriKit.Generators.HLSL.Builder;

internal static class ShaderClassBuilder
{
    public static bool TryGenerateShader(string path, ShaderMetadata? metadata, IncludeResolver includeResolver, GeneratorConfiguration config, [NotNullWhen(true)] out SourceText? classText)
    {
        classText = default;
        if (metadata == null)
        {
            return false;
        }

        var includes = includeResolver.GetIncludedFiles(path, metadata);

        var builder = new SourceCodeBuilder();
        foreach (var (includePath, include) in includes)
        {
            // Every file leads to exactly one class. So /path/to/includes.hlsl leads to
            // the class Includes in namespace ...path.to.
            // A type defined in includes.hlsl will be defined inside the Include class
            // For every include in a file we add `using static path.to.Includes;` so
            // that references work.
            var (@namespace, @class) = IncludeToClass(includePath, include, config);
            builder.WriteUsingStatic(@namespace, @class);
        }

        builder.WriteNamespace(GetNamespace(path, config));
        builder.OpenClass(Modifiers.Public, GetClassName(path));
        builder.WriteField(Modifiers.Public | Modifiers.Const, "string", "Path", ToLiteral(GetRelativePath(config.AbsoluteContentRoot, path)));

        // Write any variable (uniform)
        foreach (var variable in metadata.Variables)
        {
            builder.WriteField(Modifiers.Public | Modifiers.Const, "uint", CreateValidTypeIdentifier(variable.Name), ToLiteral(variable.Register));
        }

        // Write the structs and input element descriptions
        var structBuilder = new StructBuilder();
        structBuilder.RegisterStructs(includes, config);
        foreach (var @struct in metadata.Structures)
        {
            structBuilder.WriteStruct(builder, path, metadata, @struct, config);
        }

        // Write the cbuffer structs and registers
        foreach (var buffer in metadata.ConstantBuffers)
        {
            builder.WriteField(Modifiers.Public | Modifiers.Const, "uint", CreateValidTypeIdentifier($"{buffer.Name}Register"), ToLiteral(buffer.Register));
            structBuilder.WriteCBuffer(builder, path, metadata, buffer, config);
        }

        // Write entry points
        foreach (var function in metadata.Functions.Where(f => f.Kind != FunctionKind.Function))
        {
            var comment = new StringBuilder();
            comment.AppendLine($"Kind: {function.Kind}");
            if (!string.IsNullOrEmpty(function.Semantic))
            {
                comment.AppendLine($"Semantic: {function.Semantic}");
            }
            builder.WriteSummaryComment(comment.ToString());
            builder.WriteField(Modifiers.Public | Modifiers.Const, "string", CreateValidTypeIdentifier(function.Name), ToLiteral(function.Name));
        }

        classText = SourceText.From(builder.Build(), Encoding.UTF8);
        return true;
    }


    private static (string @namespace, string @class) IncludeToClass(string path, ShaderMetadata include, GeneratorConfiguration config)
    {
        var @namespace = GetNamespace(path, config);
        var @class = GetClassName(path);
        return (@namespace, @class);
    }

    public static string GetNamespace(string path, GeneratorConfiguration config)
    {
        // The trailing separator makes Uri treat this as a directory, matching the content root
        var directory = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
        var directoryRelativeToContentRoot = GetRelativePath(config.AbsoluteContentRoot, directory);
        var subNamespace = CreateValidNamespace(directoryRelativeToContentRoot);

        // A shader directly in the content root has no sub-namespace; avoid a trailing dot.
        return string.IsNullOrEmpty(subNamespace)
            ? config.TargetNamespace
            : $"{config.TargetNamespace}.{subNamespace}";
    }

    public static string GetClassName(string path)
    {
        return CreateValidTypeIdentifier(Path.GetFileNameWithoutExtension(path));
    }
}
