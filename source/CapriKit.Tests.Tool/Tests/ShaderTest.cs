using CapriKit.AssetLibrary.Shaders;
using CapriKit.AssetPipeline;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Resources;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.Tests.Tool.Tests.Framework;
using Microsoft.Extensions.DependencyInjection;
using System.Numerics;
using static CapriKit.AssetLibrary.Shaders.BasicShader;

namespace CapriKit.Tests.Tool.Tests;

internal sealed record ShaderTestBundle(IVertexShader VertexShader, IPixelShader PixelShader);

internal sealed class ShaderTest : ITestScreen
{
    public static Func<AssetManager, AssetBundle<ShaderTestBundle>> CreateBundleFactory()
    {
        return assetManager =>
        {
            var builder = new AssetBundleBuilder<ShaderTestBundle>(assetManager);
            var vs = builder.Request<IVertexShader>(new AssetId(BasicShader.Path, BasicShader.Vs));
            var ps = builder.Request<IPixelShader>(new AssetId(BasicShader.Path, BasicShader.Ps));
            return builder.Build(r => new ShaderTestBundle(r.Get(vs), r.Get(ps)));
        };
    }

    private readonly IVertexShader VertexShader;
    private readonly IPixelShader PixelShader;
    private readonly IInputLayout InputLayout;
    private readonly VertexBuffer<VsInput> VertexBuffer;
    private readonly IndexBuffer<ushort> IndexBuffer;
    private readonly ConstantBuffer<Constants> ConstantBuffer;

    private readonly VsInput[] Vertices;
    private readonly ushort[] Indices;
    private bool isDirty;

    public ShaderTest(Device device, [FromKeyedServices(nameof(ShaderTest))] ShaderTestBundle bundle)
    {
        VertexShader = bundle.VertexShader;
        PixelShader = bundle.PixelShader;
        InputLayout = VertexShader.CreateInputLayout(device, VsInputElementDescription);
        VertexBuffer = new VertexBuffer<VsInput>(device);
        IndexBuffer = IndexBuffers.CreateU16(device);
        ConstantBuffer = new ConstantBuffer<Constants>(device);
        Indices = [0, 1, 2];
        Vertices =
        [
            new VsInput(){ Position = new Vector2(0.5f, -0.5f), Color = new Vector4(0.0f, 1.0f, 0.0f, 1.0f)},
            new VsInput(){ Position = new Vector2(-0.5f, -0.5f), Color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f)},
            new VsInput(){ Position = new Vector2(0.0f, 0.5f), Color = new Vector4(0.0f, 0.0f, 1.0f, 1.0f)}
        ];

        isDirty = true;
    }

    public string Title => "Basic Shader";

    public void Render(DeviceContext context, float _)
    {
        UploadData(context);
        context.Setup(InputLayout, PrimitiveTopology.TriangleList, VertexShader, context.RasterizerStates.CullCounterClockwise, PixelShader, context.BlendStates.NonPreMultiplied, context.DepthStencilStates.None);
        context.IA.SetVertexBuffer(VertexBuffer);
        context.IA.SetIndexBuffer(IndexBuffer);
        context.VS.SetConstantBuffer(BasicShader.ConstantsRegister, ConstantBuffer);

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
                ProjectionMatrix = Matrix4x4.Identity
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
    }
}
