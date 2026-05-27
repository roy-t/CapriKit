using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.IO;

// TODO: the generated namespace should not include "Assets"
using CapriKit.Tests.Tool.Shaders.Assets;
using CapriKit.Tests.Tool.Tests.Framework;
using System.Numerics;
using static CapriKit.Tests.Tool.Shaders.Assets.BasicShader;

namespace CapriKit.Tests.Tool.Tests;

internal sealed class ShaderTest : ITestScreen
{
    private readonly VertexBuffer<VsInput> VertexBuffer;
    private readonly IndexBufferU16 IndexBuffer;
    private readonly ConstantBuffer<Matrix4x4> ConstantBuffer;

    public ShaderTest(Device device)
    {
        var config = CapriKit.Generators.HLSL.Configuration.ContentRoot;
        // TODO: get asset directory
        var fileSystem = new ScopedFileSystem(BasicShader.Path);
        var source = fileSystem.ReadAllText(BasicShader.Path).Result;
        var vs = ShaderCompiler.CompileVertexShader(fileSystem, source, BasicShader.Vs, "vs");

        VertexBuffer = new VertexBuffer<VsInput>(device, nameof(ShaderTest));
        IndexBuffer = new IndexBufferU16(device, nameof(ShaderTest));
        ConstantBuffer = new ConstantBuffer<Matrix4x4>(device, nameof(ShaderTest));
    }

    public string Title => "Basic Shader";

    public void Render(DeviceContext context)
    {

    }
}
