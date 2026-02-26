using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources.Shaders;

public interface IComputeShader : IDisposable
{
    internal ID3D11ComputeShader ID3D11ComputeShader { get; }

    /// <summary>
    /// The shader kernel defines how big the groups for each dimension are.
    /// Use this method to compute the minimum number of groups to dispatch to
    /// cover your entire volume.
    /// </summary>    
    (uint X, uint Y, uint Z) GetDispatchSize(uint dimX, uint dimY, uint dimZ);
}

public sealed class ComputeShader : IComputeShader
{
    private readonly uint NumThreadsX;
    private readonly uint NumThreadsY;
    private readonly uint NumThreadsZ;
    private readonly ID3D11ComputeShader Shader;

    internal ComputeShader(ID3D11ComputeShader shader, uint numThreadsX, uint numThreadsY, uint numThreadsZ)
    {
        Shader = shader;
        NumThreadsX = numThreadsX;
        NumThreadsY = numThreadsY;
        NumThreadsZ = numThreadsZ;
    }

    ID3D11ComputeShader IComputeShader.ID3D11ComputeShader => Shader;

    /// <inheritdoc/>
    public (uint X, uint Y, uint Z) GetDispatchSize(uint dimX, uint dimY, uint dimZ)
    {
        var x = GetDispatchSize(NumThreadsX, dimX);
        var y = GetDispatchSize(NumThreadsY, dimY);
        var z = GetDispatchSize(NumThreadsZ, dimZ);

        return (x, y, z);
    }

    private static uint GetDispatchSize(uint numThreads, uint dim)
    {
        return (dim + numThreads - 1) / numThreads;
    }

    public void Dispose()
    {
        Shader.Dispose();
    }
}
