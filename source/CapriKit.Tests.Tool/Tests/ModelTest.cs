using CapriKit.AssetPipeline;
using CapriKit.AssetPipeline.DirectX11.Models;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Resources;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.Tests.Tool.Shaders;
using CapriKit.Tests.Tool.Tests.Framework;
using static CapriKit.Tests.Tool.Shaders.BasicShader;

namespace CapriKit.Tests.Tool.Tests;

internal sealed record ModelTestBundle(IVertexShader VertexShader, IPixelShader PixelShader);

internal sealed class ModelTest : ITestScreen
{
    public static ITestFactory CreateFactory(AssetManager assetManager)
    {
        var builder = new AssetBundleBuilder<ModelTestBundle>(assetManager);
        var vs = builder.Request<IVertexShader>(new AssetId(BasicShader.Path, BasicShader.Vs));
        var ps = builder.Request<IPixelShader>(new AssetId(BasicShader.Path, BasicShader.Ps));
        var bundle = builder.Build(r => new ModelTestBundle(r.Get(vs), r.Get(ps)));
        return new TestFactory<ShaderTest, ModelTestBundle>("Model Test", bundle);
    }

    public string Title => "Model Test";

    private readonly IVertexShader VertexShader;
    private readonly IPixelShader PixelShader;
    private readonly IInputLayout InputLayout;
    private readonly ConstantBuffer<Constants> ConstantBuffer;

    private readonly CKTModel Model;

    private bool isDirty;

    public ModelTest(Device device, ModelTestBundle bundle)
    {
        VertexShader = bundle.VertexShader;
        PixelShader = bundle.PixelShader;
        InputLayout = VertexShader.CreateInputLayout(device, VsInputElementDescription);
        ConstantBuffer = new ConstantBuffer<Constants>(device, nameof(ShaderTest));

        Model = new CKTModel(device, CKTModelGenerator.CreateUnitCube());

        isDirty = true;
    }

    public void Render(DeviceContext context)
    {
        context.Setup(InputLayout, PrimitiveTopology.TriangleList, VertexShader, context.RasterizerStates.CullCounterClockwise, PixelShader, context.BlendStates.NonPreMultiplied, context.DepthStencilStates.None);
        context.VS.SetConstantBuffer(0, ConstantBuffer);
        context.PS.SetSampler(0, context.SamplerStates.LinearWrap);

        Model.Draw(context, 0);
    }

    public void Dispose()
    {
        ConstantBuffer.Dispose();
        InputLayout.Dispose();
        PixelShader.Dispose();
        VertexShader.Dispose();
    }
}
