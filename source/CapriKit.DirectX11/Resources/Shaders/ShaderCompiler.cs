using CapriKit.DirectX11.Debug;
using CapriKit.IO;
using Vortice.D3DCompiler;
using Vortice.Direct3D11.Shader;

namespace CapriKit.DirectX11.Resources.Shaders;

public record ShaderByteCode(byte[] Bytes, string EntryPoint, string Name);

public sealed record VertexShaderByteCode(ShaderByteCode Common)
{
    public byte[] Bytes => Common.Bytes;
    public string EntryPoint => Common.EntryPoint;
    public string Name => Common.Name;
}

public sealed record PixelShaderByteCode(ShaderByteCode Common)
{
    public byte[] Bytes => Common.Bytes;
    public string EntryPoint => Common.EntryPoint;
    public string Name => Common.Name;
}


public sealed record ComputeShaderByteCode(ShaderByteCode Common, uint NumThreadsX, uint NumThreadsY, uint NumThreadsZ)
{
    public byte[] Bytes => Common.Bytes;
    public string EntryPoint => Common.EntryPoint;
    public string Name => Common.Name;
}

public static class ShaderCompiler
{
    private const string VERTEX_SHADER_PROFILE = "vs_5_0";
    private const string PIXEL_SHADER_PROFILE = "ps_5_0";
    private const string COMPUTE_SHADER_PROFILE = "cs_5_0";

    public static IVertexShader CompileVertexShader(IReadOnlyVirtualFileSystem fileSystem, DirectoryPath includePath, Device device, string source, string entryPoint, string name)
    {
        var bytes = CompileVertexShader(fileSystem, includePath, source, entryPoint, name);
        return CreateVertexShader(bytes, device);
    }

    public static VertexShaderByteCode CompileVertexShader(IReadOnlyVirtualFileSystem fileSystem, DirectoryPath includePath, string source, string entryPoint, string name)
    {
        var byteCode = Compile(fileSystem, includePath, source, entryPoint, name, VERTEX_SHADER_PROFILE);
        return new VertexShaderByteCode(byteCode);
    }

    public static IVertexShader CreateVertexShader(VertexShaderByteCode byteCode, Device device)
    {
        var shader = device.ID3D11Device.CreateVertexShader(byteCode.Bytes);
        shader.DebugName = DebugName.For(shader, $"{byteCode.Name}:{byteCode.EntryPoint}");
        return new VertexShader(byteCode.Bytes, shader);
    }

    public static IPixelShader CompilePixelShader(IReadOnlyVirtualFileSystem fileSystem, DirectoryPath includePath, Device device, string source, string entryPoint, string name)
    {
        var byteCode = CompilePixelShader(fileSystem, includePath, source, entryPoint, name);
        return CreatePixelShader(byteCode, device);
    }

    public static PixelShaderByteCode CompilePixelShader(IReadOnlyVirtualFileSystem fileSystem, DirectoryPath includePath, string source, string entryPoint, string name)
    {
        var bytes = Compile(fileSystem, includePath, source, entryPoint, name, PIXEL_SHADER_PROFILE);
        return new PixelShaderByteCode(bytes);
    }
    public static IPixelShader CreatePixelShader(PixelShaderByteCode byteCode, Device device)
    {
        var shader = device.ID3D11Device.CreatePixelShader(byteCode.Bytes);
        shader.DebugName = DebugName.For(shader, $"{byteCode.Name}:{byteCode.EntryPoint}");
        return new PixelShader(shader);
    }

    public static IComputeShader CompileComputeShader(IReadOnlyVirtualFileSystem fileSystem, DirectoryPath includePath, Device device, string source, string entryPoint, string name)
    {
        var common = CompileComputeShader(fileSystem, includePath, source, entryPoint, name);
        return CreateComputeShader(common, device);
    }

    public static ComputeShaderByteCode CompileComputeShader(IReadOnlyVirtualFileSystem fileSystem, DirectoryPath includePath, string source, string entryPoint, string name)
    {
        var common = Compile(fileSystem, includePath, source, entryPoint, name, COMPUTE_SHADER_PROFILE);
        var (x, y, z) = QueryNumThreads(common.Bytes, name);
        return new ComputeShaderByteCode(common, x, y, z);
    }

    public static IComputeShader CreateComputeShader(ComputeShaderByteCode byteCode, Device device)
    {
        var shader = device.ID3D11Device.CreateComputeShader(byteCode.Bytes);
        shader.DebugName = DebugName.For(shader, $"{byteCode.Name}:{byteCode.EntryPoint}");
        return new ComputeShader(shader, byteCode.NumThreadsX, byteCode.NumThreadsY, byteCode.NumThreadsZ);
    }

    private static ShaderByteCode Compile(IReadOnlyVirtualFileSystem fileSystem, DirectoryPath includePath, string source, string entryPoint, string name, string profile)
    {
        using var includeResolver = new ShaderIncludeResolver(fileSystem, includePath);

        var result = Compiler.Compile(source, [], includeResolver, entryPoint, name, profile, out var blob, out var errorBlob);
        if (errorBlob != null)
        {
            ShaderCompilationAnalyzer.ThrowOnWarningOrError(errorBlob.AsSpan(), "unknown pragma ignored");
            errorBlob.Dispose();
        }

        // Check the general return value for problems, AFTER having analyzed the errors
        result.CheckError();

        var bytes = blob.AsBytes();
        blob.Dispose();
        return new ShaderByteCode(bytes, entryPoint, name);
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
            throw new Exception($"Shader reflection failed on shader: {name}");
        }

        reflection.GetThreadGroupSize(out var x, out var y, out var z);
        reflection.Dispose();

        return (x, y, z);
    }
}
