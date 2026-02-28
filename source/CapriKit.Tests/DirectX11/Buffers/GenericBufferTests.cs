using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.IO;
using TUnit.Assertions.Enums;

namespace CapriKit.Tests.DirectX11.Buffers;

internal class GenericBufferTests
{
    /// <summary>
    /// Tests StructuredBuffer, RWStructuredBuffer and StagingBuffer through a round-trip of data.
    /// </summary>    
    [Test]
    public async Task Mix_Upload_Modify_Download_Staging()
    {
        using var device = new Device();
        var context = device.ImmediateDeviceContext;

        // Create a structured buffer to upload four prime numbers to the GPU
        using var uploadBuffer = new StructuredBuffer<float>(device, "uploadBuffer");
        uploadBuffer.Write(context, [2.0f, 3.0f, 5.0f, 7.0f]);
        using var srv = uploadBuffer.CreateShaderResourceView(device);

        // Create RW structured buffer the shader can write to and CPU can read from
        using var downloadBuffer = new RWStructuredBuffer<float>(device, "downloadBuffer");
        downloadBuffer.EnsureCapacity(4);
        using var uav = downloadBuffer.CreateUnorderedAccessView(device);

        // Run the shader that reads data from the upload buffer
        // and stores a modified version of that data in the download buffer
        using var shader = Create(device);
        context.CS.SetShaderResource(0, srv);
        context.CS.SetUnorderedAccessView(0, uav);
        context.CS.SetShader(shader);

        var (dx, dy, dz) = shader.GetDispatchSize(4, 1, 1);
        context.CS.Dispatch(dx, dy, dz);

        // Read back the data to prove that both buffer types work
        var rwTarget = new float[4];
        using (var reader = downloadBuffer.OpenReader(context))
        {
            reader.Read(0, 4, rwTarget);
        }

        await Assert.That(rwTarget[0]).IsEqualTo(2.0f);
        await Assert.That(rwTarget[1]).IsEqualTo(6.0f);
        await Assert.That(rwTarget[2]).IsEqualTo(15.0f);
        await Assert.That(rwTarget[3]).IsEqualTo(28.0f);

        // Not all GPU buffers are directly readable by the CPU. In those cases
        // you can use a staging buffer to copy GPU data to GPU buffer that is CPU
        // readable. In this case simulate that to test the staging buffer.
        using var stagingBuffer = new StagingBuffer<float>(device, "stagingBuffer");
        stagingBuffer.CopyResourceToStagingBuffer(context, downloadBuffer);

        var stagingTarget = new float[4];
        using (var reader = stagingBuffer.OpenReader(context))
        {
            reader.Read(0, 4, stagingTarget);
        }

        await Assert.That(stagingTarget).IsEquivalentTo(rwTarget, CollectionOrdering.Matching);
    }


    private static IComputeShader Create(Device device)
    {
        var fileSystem = new InMemoryFileSystem();
        return ShaderCompiler.CompileComputeShader(fileSystem, device, ShaderSource, "CS", "DeviceTest.cs");
    }

    private const string ShaderSource = """        
        StructuredBuffer<float> gInput  : register(t0);        
        RWStructuredBuffer<float> gOutput : register(u0);        
        
        [numthreads(1, 1, 1)]
        void CS(uint3 dispatchThreadId : SV_DispatchThreadID)
        {
            const uint idx = dispatchThreadId.x;
            if (idx >= 4)
                return;
            
            float v = gInput[idx];
            gOutput[idx] = v * (idx + 1);
        }
        """;
}
