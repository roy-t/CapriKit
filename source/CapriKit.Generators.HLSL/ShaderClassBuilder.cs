using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using static CapriKit.Generators.HLSL.SourceCodeUtils;

namespace CapriKit.Generators.HLSL;

public static class ShaderClassBuilder
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
            var (@namespace, @class) = IncludeToClass(path, include, config);
            builder.WriteUsingStatic(@namespace, @class);
        }

        builder.WriteNamespace(GetNamespace(path, config));
        builder.OpenClass(Modifiers.Public, GetClassName(path));
        builder.WriteField(Modifiers.Public | Modifiers.Const, "string", "Path", ToLiteral(GetRelativePath(config.ContentRoot, path)));

        foreach (var variable in metadata.Variables)
        {
            builder.WriteField(Modifiers.Public | Modifiers.Const, "uint", CreateValidIdentifier(variable.Name), ToLiteral(variable.Register));
        }

        foreach (var @struct in metadata.Structures)
        {
            StructBuilder.WriteStruct(builder, @struct);
        }

        classText = SourceText.From(builder.Build(), Encoding.UTF8);
        return true;
    }


    private static (string @namespace, string @class) IncludeToClass(string currentFilePath, Include include, GeneratorConfiguration config)
    {
        var currentDirectory = Path.GetDirectoryName(currentFilePath);
        var relativeIncludeDirectory = Path.GetDirectoryName(include.Path);
        var absoluteIncludeDirectory = Path.Combine(currentDirectory, relativeIncludeDirectory);

        var @namespace = GetNamespace(absoluteIncludeDirectory, config);
        var @class = GetClassName(include.Path);

        return (@namespace, @class);
    }

    private static string GetNamespace(string path, GeneratorConfiguration config)
    {
        var directory = Path.GetDirectoryName(path);
        var directoryRelativeToContentRoot = GetRelativePath(config.ContentRoot, directory);
        var elements = directoryRelativeToContentRoot.Split(['\\', '/']);
        return $"{config.TargetNamespace}.{CreateValidNamespace(string.Join(".", elements))}";
    }

    private static string GetClassName(string path)
    {
        return CreateValidIdentifier(Path.GetFileNameWithoutExtension(path));
    }
}

/*
public sealed class SomeShader
{
    private readonly CapriKit.DirectX11.Device Device;

    public static string SourceFile => "SomePath.hlsl";

    public const uint SomeRegister = 0;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SomeType
    {
        public float Value;
    }


    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public struct SomeCBufferType
    {
        [System.Runtime.InteropServices.FieldOffset(0)]
        public float Value;
    }

    // We don't need CreateInputLayout as IVertexShader takes care of that

    public CapriKit.DirectX11.Resources.Shaders.IVertexShader EntryPointA { get; }
    public CapriKit.DirectX11.Resources.Shaders.IPixelShader EntryPointB { get; }

    public SomeShader.Binding CreateBinding() => new SomeShader.Binding(Device);

    public sealed class Binding : System.IDisposable
    {
        public Binding(CapriKit.DirectX11.Device device)
        {
             CBuffer = new CapriKit.DirectX11.Buffers.ConstantBuffer<SomeCBufferType>(device, "CBuffer");
        }

        private readonly CapriKit.DirectX11.Buffers.ConstantBuffer<SomeCBufferType> CBuffer;

        public void MapConstantBuffer(CapriKit.DirectX11.Contexts.DeviceContext context, float value)
        {
            var constants = new SomeCBufferType
            {
                Value = value,
            };

            CBuffer.Write(context, [constants]);
        }

        public void Dispose()
        {
            CBuffer.Dispose();
        }
    }
}
*/
