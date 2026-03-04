using CapriKit.DirectX11.Debug;
using CapriKit.IO;
using Vortice.D3DCompiler;
using Vortice.Direct3D11.Shader;

namespace CapriKit.DirectX11.Resources.Shaders;

public record VertexShaderByteCode(byte[] Bytes, string EntryPoint, string Name);

public record PixelShaderByteCode(byte[] Bytes, string EntryPoint, string Name);

public record ComputeShaderByteCode(byte[] Bytes, uint NumThreadsX, uint NumThreadsY, uint NumThreadsZ, string EntryPoint, string Name);

public static class ShaderCompiler
{
    private const string VERTEX_SHADER_PROFILE = "vs_5_0";
    private const string PIXEL_SHADER_PROFILE = "ps_5_0";
    private const string COMPUTE_SHADER_PROFILE = "cs_5_0";

    public static IVertexShader CompileVertexShader(IReadOnlyVirtualFileSystem includes, Device device, string source, string entryPoint, string name)
    {
        var byteCode = CompileVertexShader(includes, source, entryPoint, name);
        return CreateVertexShader(byteCode, device);
    }

    public static VertexShaderByteCode CompileVertexShader(IReadOnlyVirtualFileSystem includes, string source, string entryPoint, string name)
    {
        var blob = Compile(includes, source, entryPoint, name, VERTEX_SHADER_PROFILE);
        return new VertexShaderByteCode(blob.ToArray(), entryPoint, name);
    }

    public static IVertexShader CreateVertexShader(VertexShaderByteCode byteCode, Device device)
    {
        var shader = device.ID3D11Device.CreateVertexShader(byteCode.Bytes);
        shader.DebugName = DebugName.For(shader, $"{byteCode.Name}:{byteCode.EntryPoint}");
        return new VertexShader(byteCode.Bytes, shader);
    }

    public static IPixelShader CompilePixelShader(IReadOnlyVirtualFileSystem includes, Device device, string source, string entryPoint, string name)
    {
        var byteCode = CompilePixelShader(includes, source, entryPoint, name);
        return CreatePixelShader(byteCode, device);
    }

    public static PixelShaderByteCode CompilePixelShader(IReadOnlyVirtualFileSystem includes, string source, string entryPoint, string name)
    {
        var blob = Compile(includes, source, entryPoint, name, PIXEL_SHADER_PROFILE);
        return new PixelShaderByteCode(blob.ToArray(), entryPoint, name);
    }
    public static IPixelShader CreatePixelShader(PixelShaderByteCode byteCode, Device device)
    {
        var shader = device.ID3D11Device.CreatePixelShader(byteCode.Bytes);
        shader.DebugName = DebugName.For(shader, $"{byteCode.Name}:{byteCode.EntryPoint}");
        return new PixelShader(shader);
    }

    public static IComputeShader CompileComputeShader(IReadOnlyVirtualFileSystem includes, Device device, string source, string entryPoint, string name)
    {
        var byteCode = CompileComputeShader(includes, source, entryPoint, name);
        return CreateComputeShader(byteCode, device);
    }

    public static ComputeShaderByteCode CompileComputeShader(IReadOnlyVirtualFileSystem includes, string source, string entryPoint, string name)
    {
        var blob = Compile(includes, source, entryPoint, name, COMPUTE_SHADER_PROFILE);
        var (x, y, z) = QueryNumThreads(blob, name);
        return new ComputeShaderByteCode(blob.ToArray(), x, y, z, entryPoint, name);
    }

    public static IComputeShader CreateComputeShader(ComputeShaderByteCode byteCode, Device device)
    {
        var shader = device.ID3D11Device.CreateComputeShader(byteCode.Bytes);
        shader.DebugName = DebugName.For(shader, $"{byteCode.Name}:{byteCode.EntryPoint}");
        return new ComputeShader(shader, byteCode.NumThreadsX, byteCode.NumThreadsY, byteCode.NumThreadsZ);
    }

    private static ReadOnlySpan<byte> Compile(IReadOnlyVirtualFileSystem includes, string source, string entryPoint, string name, string profile)
    {
        using var includeResolver = new ShaderIncludeResolver(includes);

        var result = Compiler.Compile(source, [], includeResolver, entryPoint, name, profile, out var blob, out var errorBlob);
        if (errorBlob != null)
        {
            ShaderCompilationAnalyzer.ThrowOnWarningOrError(errorBlob.AsSpan());
        }

        // Check the general return value for problems, AFTER having analyzed the errors        
        result.CheckError();

        return blob.AsSpan();
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
