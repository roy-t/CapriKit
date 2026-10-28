using CapriKit.AssetLibrary.Shaders;
using CapriKit.AssetPipeline;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts;
using CapriKit.Tests.Tool.Tests.Framework;
using System.Numerics;
using static CapriKit.AssetLibrary.Shaders.InstancedModelShader;

namespace CapriKit.Tests.Tool.Tests;

internal class RenderInstancedModelTest : AModelTest
{
    public static ITestFactory CreateFactory(AssetManager assetManager)
    {
        var modelId = new AssetId(CapriKit.AssetHandles.Models.Parts.blend, "Container.1TUE");
        var vertexShaderId = new AssetId(InstancedModelShader.Path, InstancedModelShader.Vs);
        var pixelShaderId = new AssetId(InstancedModelShader.Path, InstancedModelShader.Ps);
        return CreateFactory<RenderModelTest>(assetManager, nameof(RenderModelTest), modelId, vertexShaderId, pixelShaderId);
    }

    private readonly ConstantBuffer<Constants> ConstantBuffer;
    private readonly StructuredBuffer<Instance> InstanceBuffer;

    public RenderInstancedModelTest(Device device, SwapChain swapChain, TurnTableCamera camera, GenericModelTestBundle bundle)
       : base(device, swapChain, camera, bundle, VsInputElementDescription)
    {
        ConstantBuffer = new ConstantBuffer<Constants>(device, nameof(RenderInstancedModelTest));
        InstanceBuffer = new StructuredBuffer<Instance>(device, nameof(RenderInstancedModelTest));
    }

    public override string Title => "Render Instanced Model";

    public override void RenderInternal(DeviceContext context, Matrix4x4 viewProjection, float elapsed)
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
        ConstantBuffer.Dispose();
        base.Dispose();
    }
}
