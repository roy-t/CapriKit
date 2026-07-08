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
    private readonly ConstantBuffer<Constants> ConstantBuffer;

    private readonly VsInput[] Vertices;
    private readonly ushort[] Indices;
    private bool isDirty;

    private ShaderTest(IVertexShader vertexShader, IPixelShader pixelShader, IInputLayout inputLayout, VertexBuffer<VsInput> vertexBuffer, IndexBufferU16 indexBuffer, ConstantBuffer<Constants> constantBuffer)
    {
        VertexShader = vertexShader;
        PixelShader = pixelShader;
        InputLayout = inputLayout;
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        ConstantBuffer = constantBuffer;
        Indices = [0, 1, 2];
        Vertices =
        [
            new VsInput(){ Position = new Vector2(0.5f, -0.5f), Color = new Vector4(0.0f, 1.0f, 0.0f, 1.0f)},
            new VsInput(){ Position = new Vector2(-0.5f, -0.5f), Color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f)},
            new VsInput(){ Position = new Vector2(0.0f, 0.5f), Color = new Vector4(0.0f, 0.0f, 1.0f, 1.0f)}
        ];

        isDirty = true;
    }

    public static async Task<ITestScreen> Create(Device device, IReadOnlyVirtualFileSystem fileSystem, CancellationToken token)
    {
        var source = await fileSystem.ReadAllText(BasicShader.Path, cancellationToken: token);
        var directory = new FilePath(BasicShader.Path).Directory;

        // Compiling shaders is expensive, bail out early if the result is no longer needed
        token.ThrowIfCancellationRequested();

        var vs = ShaderCompiler.CompileVertexShader(fileSystem, directory, device, source, Vs, nameof(Vs));
        var ps = ShaderCompiler.CompilePixelShader(fileSystem, directory, device, source, Ps, nameof(Ps));

        var inputLayout = vs.CreateInputLayout(device, VsInputElementDescription);
        var vertexBuffer = new VertexBuffer<VsInput>(device, nameof(ShaderTest));
        var indexBuffer = new IndexBufferU16(device, nameof(ShaderTest));
        var constantBuffer = new ConstantBuffer<Constants>(device, nameof(ShaderTest));

        return new ShaderTest(vs, ps, inputLayout, vertexBuffer, indexBuffer, constantBuffer);
    }


    public string Title => "Basic Shader";

    public void Render(DeviceContext context)
    {
        UploadData(context);
        context.Setup(InputLayout, PrimitiveTopology.TriangleList, VertexShader, context.RasterizerStates.CullCounterClockwise, PixelShader, context.BlendStates.NonPreMultiplied, context.DepthStencilStates.None);
        context.IA.SetVertexBuffer(VertexBuffer);
        context.IA.SetIndexBuffer(IndexBuffer);
        context.VS.SetConstantBuffer(0, ConstantBuffer);
        context.PS.SetSampler(0, context.SamplerStates.LinearWrap);

        context.DrawIndexed(3);
    }

    private void UploadData(DeviceContext context)
    {
        if (isDirty)
        {
            VertexBuffer.Write(context, Vertices);
            IndexBuffer.Write(context, Indices);

            var constants = new Constants()
            {
                ProjectionMatrix = System.Numerics.Matrix4x4.Identity
            };
            ConstantBuffer.Write(context, [constants]);

            isDirty = false;
        }
    }

    public void Dispose()
    {
        ConstantBuffer.Dispose();
        IndexBuffer.Dispose();
        VertexBuffer.Dispose();
        InputLayout.Dispose();
        PixelShader.Dispose();
        VertexShader.Dispose();
    }
}
