using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.IO;
using Microsoft.Extensions.Logging.Abstractions;
using TUnit.Assertions.Enums;

namespace CapriKit.Tests.DirectX11.Buffers;

[NotInParallel("DirectX")]
internal class GenericBufferTests
{
    /// <summary>
    /// Tests StructuredBuffer, RWStructuredBuffer and StagingBuffer through a round-trip of data.
    /// </summary>
    [Test]
    public async Task Mix_Upload_Modify_Download_Staging()
    {
        using var device = new Device(NullLoggerFactory.Instance);
        var context = device.ImmediateDeviceContext;

        // Create a structured buffer to upload four prime numbers to the GPU
        using var structuredBuffer = new StructuredBuffer<float>(device, "uploadBuffer");
        structuredBuffer.Write(context, [2.0f, 3.0f, 5.0f, 7.0f]);
        using var srv = structuredBuffer.CreateShaderResourceView(device);

        // Create RW structured buffer the shader can write to
        using var rwBuffer = new RWStructuredBuffer<float>(device, "downloadBuffer");
        rwBuffer.EnsureCapacity(4);
        using var uav = rwBuffer.CreateUnorderedAccessView(device);

        // Run the shader that reads data from the structured buffer
        // and stores a modified version of that data in the rw structured buffer
        using var shader = Create(device);
        context.CS.SetShaderResource(0, srv);
        context.CS.SetUnorderedAccessView(0, uav);
        context.CS.SetShader(shader);

        var (dx, dy, dz) = shader.GetDispatchSize(4, 1, 1);
        context.CS.Dispatch(dx, dy, dz);

        // Use a staging buffer to read back the data
        using var stagingBuffer = new StagingBuffer<float>(device, "stagingBuffer");
        stagingBuffer.CopyResourceToStagingBuffer(context, rwBuffer);

        var stagingTarget = new float[4];
        using (var reader = stagingBuffer.OpenReader(context))
        {
            reader.Read(0, 4, stagingTarget);
        }

        await Assert.That(stagingTarget).IsEquivalentTo([2.0f, 6.0f, 15.0f, 28.0f], CollectionOrdering.Matching);
    }


    private static IComputeShader Create(Device device)
    {
        var fileSystem = new InMemoryFileSystem();
        var includePath = new DirectoryPath(Path.GetTempPath());
        return ShaderCompiler.CompileComputeShader(fileSystem, includePath, device, ShaderSource, "CS", "DeviceTest.cs");
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
