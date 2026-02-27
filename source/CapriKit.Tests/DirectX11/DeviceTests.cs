using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.IO;

namespace CapriKit.Tests.DirectX11;

internal class DeviceTests
{
    [Test]
    public async Task CanCreate()
    {
        using var device = new Device();
        await Assert.That(device).IsNotNull();
    }


    [Test]
    public async Task BufferTest()
    {
        using var device = new Device();
        var context = device.ImmediateDeviceContext;

        using var uploadBuffer = new StructuredBuffer<float>(device, "uploadBuffer");
        uploadBuffer.Write(context, [0.5f, 0.5f, 0.5f, 0.5f]);
        using var srv = uploadBuffer.CreateShaderResourceView(device);

        using var downloadBuffer = new RWStructuredBuffer<float>(device, "downloadBuffer");
        downloadBuffer.EnsureCapacity(4);
        using var uav = downloadBuffer.CreateUnorderedAccessView(device);

        var shader = Create(device);
        context.CS.SetShaderResource(0, srv);
        context.CS.SetUnorderedAccessView(0, uav);
        context.CS.SetShader(shader);

        var (dx, dy, dz) = shader.GetDispatchSize(2, 2, 1);
        context.CS.Dispatch(dx, dy, dz);

        var target = new float[4];
        using (var reader = downloadBuffer.OpenReader(context))
        {
            reader.Read(0, 4, target);
        }

        await Assert.That(target[0]).IsEqualTo(0.5f);
        await Assert.That(target[1]).IsEqualTo(1.0f);
        await Assert.That(target[2]).IsEqualTo(1.5f);
        await Assert.That(target[3]).IsEqualTo(2.0f);
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
            gOutput[idx] = v * idx;
        }
        """;
}
