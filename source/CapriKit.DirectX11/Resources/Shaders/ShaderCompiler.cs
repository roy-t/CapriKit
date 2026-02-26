using CapriKit.DirectX11.Debug;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11.Shader;

namespace CapriKit.DirectX11.Resources.Shaders;

public static class ShaderCompiler
{
    private const string VERTEX_SHADER_PROFILE = "vs_5_0";
    private const string PIXEL_SHADER_PROFILE = "ps_5_0";
    private const string COMPUTE_SHADER_PROFILE = "cs_5_0";

    public static IVertexShader CompileVertexShader(Device device, string source, string entryPoint, string name)
    {
        var blob = Compile(source, entryPoint, name, VERTEX_SHADER_PROFILE);
        var shader = device.ID3D11Device.CreateVertexShader(blob);
        shader.DebugName = DebugName.For(shader, $"{source}${entryPoint}");
        return new VertexShader(blob, shader);
    }

    public static IPixelShader CompilePixelShader(Device device, string source, string entryPoint, string name)
    {
        var blob = Compile(source, entryPoint, name, PIXEL_SHADER_PROFILE);
        var shader = device.ID3D11Device.CreatePixelShader(blob);
        shader.DebugName = DebugName.For(shader, $"{source}${entryPoint}");
        return new PixelShader(shader);
    }

    public static IComputeShader CompileComputeShader(Device device, string source, string entryPoint, string name)
    {
        var blob = Compile(source, entryPoint, name, COMPUTE_SHADER_PROFILE);
        var shader = device.ID3D11Device.CreateComputeShader(blob);
        shader.DebugName = DebugName.For(shader, $"{source}${entryPoint}");

        var (x, y, z) = QueryNumThreads(blob.AsSpan(), name);

        return new ComputeShader(shader, x, y, z);
    }

    private static Blob Compile(string source, string entryPoint, string name, string profile)
    {
        var result = Compiler.Compile(source, entryPoint, name, profile, out var blob, out var errorBlob);
        result.CheckError();

        if (errorBlob != null)
        {
            ShaderCompilationAnalyzer.ThrowOnWarningOrError(errorBlob.AsSpan());
        }

        return blob;
    }

    /// <summary>
    /// Uses shader reflection to get the number of threads in the x, y and z dimension
    /// as defined via [numthreads] on the compute shader's entry point. These values
    /// can be used to calculate the dispatch size.
    /// </summary>    
    private static (uint x, uint y, uint z) QueryNumThreads(ReadOnlySpan<byte> blob, string name)
    {
        var result = Compiler.Reflect<ID3D11ShaderReflection>(blob, out var reflection);
        result.CheckError();

        if (reflection == null)
        {
            throw new Exception($"Failed to reflect shader: {name}");
        }

        reflection.GetThreadGroupSize(out var x, out var y, out var z);
        reflection.Dispose();

        return (x, y, z);
    }
}
