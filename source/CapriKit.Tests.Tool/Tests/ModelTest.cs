using CapriKit.AssetPipeline;
using CapriKit.AssetPipeline.DirectX11.Models;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Resources;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.Geometry;
using CapriKit.Tests.Tool.Shaders;
using CapriKit.Tests.Tool.Tests.Framework;
using System.Numerics;
using static CapriKit.Tests.Tool.Shaders.ModelShader;

namespace CapriKit.Tests.Tool.Tests;

internal sealed record ModelTestBundle(IVertexShader VertexShader, IPixelShader PixelShader);

internal sealed class ModelTest : ITestScreen
{
    public static ITestFactory CreateFactory(AssetManager assetManager)
    {
        var builder = new AssetBundleBuilder<ModelTestBundle>(assetManager);
        var vs = builder.Request<IVertexShader>(new AssetId(ModelShader.Path, ModelShader.Vs));
        var ps = builder.Request<IPixelShader>(new AssetId(ModelShader.Path, ModelShader.Ps));
        var bundle = builder.Build(r => new ModelTestBundle(r.Get(vs), r.Get(ps)));
        return new TestFactory<ModelTest, ModelTestBundle>("Model Test", bundle);
    }

    private readonly IVertexShader VertexShader;
    private readonly IPixelShader PixelShader;
    private readonly IInputLayout InputLayout;
    private readonly ConstantBuffer<Constants> ConstantBuffer;
    private readonly CKTModel Model;
    private readonly SwapChain SwapChain;

    private float accumulator;

    public ModelTest(Device device, SwapChain swapChain, ModelTestBundle bundle)
    {
        SwapChain = swapChain;
        VertexShader = bundle.VertexShader;
        PixelShader = bundle.PixelShader;
        InputLayout = VertexShader.CreateInputLayout(device, VsInputElementDescription);
        ConstantBuffer = new ConstantBuffer<Constants>(device, nameof(ShaderTest));
        Model = new CKTModel(device, CKTModelGenerator.CreateUnitCube());
    }

    public string Title => "Model Test";

    public void Render(DeviceContext context, float elapsed)
    {
        accumulator += elapsed;

        const int lod = 0;
        const float speed = 0.5f;
        var world = Matrix4x4.CreateRotationY(accumulator * speed) * Matrix4x4.CreateRotationX(accumulator * speed);
        var view = Matrix4x4.CreateLookAt(new Vector3(0.0f, 1.0f, -1.0f), Vector3.Zero, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2, SwapChain.AspectRatio, 0.01f, 1000.0f);

        var constants = new Constants()
        {
            World = world,
            ViewProjection = view * projection
        };
        ConstantBuffer.Write(context, [constants]);

        context.Setup(InputLayout, PrimitiveTopology.TriangleList, VertexShader, context.RasterizerStates.CullCounterClockwise, PixelShader, context.BlendStates.Opaque, context.DepthStencilStates.None);
        context.VS.SetConstantBuffer(0, ConstantBuffer);
        context.PS.SetConstantBuffer(0, ConstantBuffer);
        context.PS.SetSampler(0, context.SamplerStates.LinearWrap);

        Model.DrawIndexed(context, 0, 1, lod);
    }

    public void Dispose()
    {
        ConstantBuffer.Dispose();
        InputLayout.Dispose();
        PixelShader.Dispose();
        VertexShader.Dispose();

        Model.Dispose();
    }
}
