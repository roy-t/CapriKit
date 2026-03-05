using Vortice.D3DCompiler;
using Vortice.Direct3D11.Shader;

namespace CapriKit.DirectX11.Resources.Shaders.Reflection;

// TODO: maybe its easier to use SLANG? https://www.nuget.org/packages/Slang.Sdk#readme-body-tab
// https://github.com/Aqqorn/Slang.Sdk

internal static class ShaderReflector
{
    private enum ShaderType
    {
        VertexShader,
        PixelShader,
        ComputeShader
    }

    private record ShaderEntryPoint(string Name, ShaderType Type);

    public static void Reflect(ShaderByteCode byteCode)
    {
        // TODO: reflect using 3 different methods
        // - Constant Buffers pReflector->GetConstantBufferByIndex(i);
        // - Structured Buffers:  pReflector->GetResourceBindingDesc(i, &bindDesc);
        // - VS_INPUT etc...: pReflector->GetInputParameterDesc(i, &paramDesc);

        var result = Compiler.Reflect<ID3D11ShaderReflection>(byteCode.Bytes, out var reflection);
        result.CheckError();

        if (reflection == null)
        {
            throw new Exception($"Shader reflection failed on shader: {byteCode.Name}");
        }

        ReflectInputParameters(reflection.InputParameters);

        //foreach (var constantBuffer in reflection.ConstantBuffers)
        //{
        //    var name = constantBuffer.Description.Name;
        //    var size = constantBuffer.Description.Size;

        //    foreach(var variable in constantBuffer.Variables)
        //    {
        //        variable.VariableType
        //    }            
        //}

        reflection.Dispose();
    }

    private static void ReflectInputParameters(ShaderParameterDescription[] inputParameters)
    {
        foreach (var parameter in inputParameters)
        {
            // TODO: you can't get the name of it after compilation :(
            var semanticName = parameter.SemanticName;
            var semanticIndex = parameter.SemanticIndex;
            var mask = parameter.UsageMask; // X Y Z W
            var componentType = parameter.ComponentType;
        }
    }

    // TODO: this sounds fun but needs quite a bit of work
    //public List<ShaderEntryPoint> FindEntryPoints(string text)
    //{
    //    var start = 0;
    //    var length = 0;

    //    for (var i = 1; i < text.Length; i++)
    //    {
    //        length = i - start;

    //        if (IsValidFunctionName(text.AsSpan(start, length)))
    //        {

    //        }
    //        else
    //        {

    //        }
    //    }
    //}

    //private static bool IsValidFunctionName(ReadOnlySpan<char> name)
    //{
    //    if (name.Length == 0)
    //    {
    //        return false;
    //    }

    //    if (!IsValidHlslIdentifierStart(name[0]))
    //    {
    //        return false;
    //    }

    //    for (var i = 1; i < name.Length; i++)
    //    {
    //        if (!IsValidHlslIdentifierChar(name[i]))
    //            return false;
    //    }

    //    return true;
    //}

    //private static bool IsValidHlslIdentifierChar(char c)
    //{
    //    return char.IsLetterOrDigit(c) || c == '_';
    //}

    //private static bool IsValidHlslIdentifierStart(char c)
    //{
    //    return char.IsLetter(c) || c == '_';
    //}
}
