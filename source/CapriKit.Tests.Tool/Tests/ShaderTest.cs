using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Resources;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.IO;
using CapriKit.Tests.Tool.Shaders;
using CapriKit.Tests.Tool.Tests.Framework;
using System.Numerics;
using static CapriKit.Tests.Tool.Shaders.BasicShader;

namespace CapriKit.Tests.Tool.Tests;

internal sealed class ShaderTest : ITestScreen
{
    private readonly IVertexShader VertexShader;
    private readonly IPixelShader PixelShader;
    private readonly IInputLayout InputLayout;
    private readonly VertexBuffer<VsInput> VertexBuffer;
    private readonly IndexBufferU16 IndexBuffer;
    private readonly ConstantBuffer<Matrix4x4> ConstantBuffer;

    public ShaderTest(IVertexShader vertexShader, IPixelShader pixelShader, IInputLayout inputLayout, VertexBuffer<VsInput> vertexBuffer, IndexBufferU16 indexBuffer, ConstantBuffer<Matrix4x4> constantBuffer)
    {
        VertexShader = vertexShader;
        PixelShader = pixelShader;
        InputLayout = inputLayout;
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        ConstantBuffer = constantBuffer;
    }

    public static async Task<ShaderTest> Create(Device device, IVirtualFileSystem fileSystem)
    {
        var source = await fileSystem.ReadAllText(BasicShader.Path);
        var directory = new FilePath(BasicShader.Path).Directory;

        var vs = ShaderCompiler.CompileVertexShader(fileSystem, directory, device, source, Vs, nameof(Vs));
        var ps = ShaderCompiler.CompilePixelShader(fileSystem, directory, device, source, Ps, nameof(Ps));
        var inputLayout = vs.CreateInputLayout(device, []);
        var vertexBuffer = new VertexBuffer<VsInput>(device, nameof(ShaderTest));
        var indexBuffer = new IndexBufferU16(device, nameof(ShaderTest));
        var constantBuffer = new ConstantBuffer<Matrix4x4>(device, nameof(ShaderTest));

        return new ShaderTest(vs, ps, inputLayout, vertexBuffer, indexBuffer, constantBuffer);
    }


    public string Title => "Basic Shader";

    public void Render(DeviceContext context)
    {

    }
}
