using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources.Shaders;

public interface IComputeShader : IDisposable
{
    internal uint NumThreadsX { get; set; }
    internal uint NumThreadsY { get; set; }
    internal uint NumThreadsZ { get; set; }
    internal ID3D11ComputeShader ID3D11ComputeShader { get; set; }


    public void HotSwap(IComputeShader replacement)
    {
        NumThreadsX = replacement.NumThreadsX;
        NumThreadsY = replacement.NumThreadsY;
        NumThreadsZ = replacement.NumThreadsZ;

        var oldShader = ID3D11ComputeShader;
        ID3D11ComputeShader = replacement.ID3D11ComputeShader;
        oldShader.Dispose();
    }

    /// <summary>
    /// The shader kernel defines how big the groups for each dimension are.
    /// Use this method to compute the minimum number of groups to dispatch to
    /// cover your entire volume.
    /// </summary>    
    (uint X, uint Y, uint Z) GetDispatchSize(uint dimX, uint dimY, uint dimZ);
}

internal sealed class ComputeShader : IComputeShader
{
    private uint numThreadsX;
    private uint numThreadsY;
    private uint numThreadsZ;
    private ID3D11ComputeShader shader;

    internal ComputeShader(ID3D11ComputeShader shader, uint numThreadsX, uint numThreadsY, uint numThreadsZ)
    {
        this.shader = shader;
        this.numThreadsX = numThreadsX;
        this.numThreadsY = numThreadsY;
        this.numThreadsZ = numThreadsZ;
    }

    ID3D11ComputeShader IComputeShader.ID3D11ComputeShader
    {
        get { return shader; }
        set { shader = value; }
    }

    uint IComputeShader.NumThreadsX
    {
        get { return numThreadsX; }
        set { numThreadsX = value; }
    }

    uint IComputeShader.NumThreadsY
    {
        get { return numThreadsY; }
        set { numThreadsY = value; }
    }

    uint IComputeShader.NumThreadsZ
    {
        get { return numThreadsZ; }
        set { numThreadsZ = value; }
    }

    /// <inheritdoc/>
    public (uint X, uint Y, uint Z) GetDispatchSize(uint dimX, uint dimY, uint dimZ)
    {
        var x = GetDispatchSize(numThreadsX, dimX);
        var y = GetDispatchSize(numThreadsY, dimY);
        var z = GetDispatchSize(numThreadsZ, dimZ);

        return (x, y, z);
    }

    private static uint GetDispatchSize(uint numThreads, uint dim)
    {
        return (dim + numThreads - 1) / numThreads;
    }

    public void Dispose()
    {
        shader.Dispose();
    }
}
