using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using static CapriKit.Generators.HLSL.SourceCodeUtils;

namespace CapriKit.Generators.HLSL;

internal static class ShaderClassBuilder
{
    public static bool TryGenerateShader(string path, SourceText? shaderText, GeneratorConfiguration config, [NotNullWhen(true)] out SourceText? classText)
    {
        classText = default;
        if (shaderText == null)
        {
            return false;
        }

        var tokens = HLSLTokenizer.Parse(shaderText.ToString());
        var metadata = HLSLParser.Parse(tokens);

        var builder = new SourceCodeBuilder();
        foreach (var include in metadata.Includes.Where(i => i.Kind == IncludeKind.Local))
        {
            // Every file leads to exactly one class. So /path/to/includes.hlsl leads to
            // the class Includes in namespace ...path.to.
            // A type defined in includes.hlsl will be defined inside the Include class
            // For every include in a file we add `using static path.to.Includes;` so
            // that references work.
            var (@namespace, @class) = IncludeToClass(path, include, config);
            builder.WriteUsingStatic(@namespace, @class);
        }

        builder.WriteNamespace(GetNamespace(path, config));
        builder.OpenClass(Modifiers.Public, GetClassName(path));
        builder.WriteField(Modifiers.Public | Modifiers.Const, "string", "Path", ToLiteral(GetRelativePath(config.AbsoluteContentRoot, path)));

        foreach (var variable in metadata.Variables)
        {
            builder.WriteField(Modifiers.Public | Modifiers.Const, "uint", CreateValidTypeIdentifier(variable.Name), ToLiteral(variable.Register));
        }

        foreach (var @struct in metadata.Structures)
        {
            StructBuilder.WriteStruct(builder, @struct);
            if (@struct.Kind == StructureKind.VertexShaderInput)
            {
                InputElementDescriptionBuilder.WriteInputElementDescription(builder, @struct);
            }
        }

        foreach (var buffer in metadata.ConstantBuffers)
        {
            builder.WriteField(Modifiers.Public | Modifiers.Const, "uint", CreateValidTypeIdentifier($"{buffer.Name}Register"), ToLiteral(buffer.Register));
            StructBuilder.WriteStruct(builder, buffer);
        }

        foreach (var function in metadata.Functions)
        {
            // Ignore regular functions
            if (function.Kind == FunctionKind.Function)
            {
                continue;
            }

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


    private static (string @namespace, string @class) IncludeToClass(string currentFilePath, Include include, GeneratorConfiguration config)
    {
        var currentDirectory = Path.GetDirectoryName(currentFilePath);
        var absoluteIncludeDirectory = Path.Combine(currentDirectory, include.Path);

        var @namespace = GetNamespace(absoluteIncludeDirectory, config);
        var @class = GetClassName(include.Path);

        return (@namespace, @class);
    }

    private static string GetNamespace(string path, GeneratorConfiguration config)
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

    private static string GetClassName(string path)
    {
        return CreateValidTypeIdentifier(Path.GetFileNameWithoutExtension(path));
    }
}
