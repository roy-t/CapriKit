using CapriKit.AssetLibrary.Shaders;
using CapriKit.AssetPipeline;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Resources;
using Microsoft.Extensions.DependencyInjection;
using System.Numerics;
using static CapriKit.AssetLibrary.Shaders.ModelShader;

namespace CapriKit.Tests.Tool.Tests;

internal sealed class RenderModelTest : AModelTest
{
    public static Func<AssetManager, AssetBundle<GenericModelTestBundle>> CreateBundleFactory()
    {
        var modelId = new AssetId(CapriKit.AssetHandles.Models.Parts.blend, "Container.1TUE");
        var vertexShaderId = new AssetId(ModelShader.Path, ModelShader.Vs);
        var pixelShaderId = new AssetId(ModelShader.Path, ModelShader.Ps);
        return GenericModelTestBundle.CreateFactory(modelId, vertexShaderId, pixelShaderId);
    }

    private readonly ConstantBuffer<Constants> ConstantBuffer;

    public RenderModelTest(Device device, SwapChain swapChain, TurnTableCamera camera, [FromKeyedServices(nameof(RenderModelTest))] GenericModelTestBundle bundle)
        : base(device, swapChain, camera, bundle, VsInputElementDescription)
    {
        ConstantBuffer = new ConstantBuffer<Constants>(device);
    }

    public override string Title => "Render Model";

    public override void RenderInternal(DeviceContext context, Matrix4x4 viewProjection, float _)
    {
        var constants = new Constants()
        {
            World = Matrix4x4.Identity,
            ViewProjection = viewProjection,
        };
        ConstantBuffer.Write(context, [constants]);

        context.Setup(InputLayout, PrimitiveTopology.TriangleList, VertexShader, context.RasterizerStates.CullCounterClockwise, PixelShader, context.BlendStates.Opaque, context.DepthStencilStates.ReverseZ);
        context.VS.SetConstantBuffer(ModelShader.ConstantsRegister, ConstantBuffer);
        context.PS.SetConstantBuffer(ModelShader.ConstantsRegister, ConstantBuffer);

        const int lod = 0;
        Model.DrawIndexed(context, ModelShader.Materials, ModelShader.Triangles, lod);
    }

    public override void Dispose()
    {
        ConstantBuffer.Dispose();
        base.Dispose();
    }
}
