using CapriKit.AssetLibrary.Shaders;
using CapriKit.AssetPipeline;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Resources;
using CapriKit.DirectX11.Resources.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Numerics;
using static CapriKit.AssetLibrary.Shaders.InstancedModelShader;

namespace CapriKit.Tests.Tool.Tests;

internal class RenderInstancedModelTest : AModelTest
{
    public static Func<AssetManager, AssetBundle<GenericModelTestBundle>> CreateBundleFactory()
    {
        var modelId = new AssetId(CapriKit.AssetHandles.Models.Parts.blend, "Container.1TUE");
        var vertexShaderId = new AssetId(InstancedModelShader.Path, InstancedModelShader.Vs);
        var pixelShaderId = new AssetId(InstancedModelShader.Path, InstancedModelShader.Ps);
        return GenericModelTestBundle.CreateFactory(modelId, vertexShaderId, pixelShaderId);
    }

    private readonly ConstantBuffer<Constants> ConstantBuffer;
    private readonly StructuredBuffer<Instance> InstanceBuffer;
    private readonly IShaderResourceView InstanceView;

    private bool isDirty = true;
    private uint instanceCount = 0;

    public RenderInstancedModelTest(Device device, SwapChain swapChain, TurnTableCamera camera, [FromKeyedServices(nameof(RenderInstancedModelTest))] GenericModelTestBundle bundle)
       : base(device, swapChain, camera, bundle, VsInputElementDescription)
    {
        ConstantBuffer = new ConstantBuffer<Constants>(device);
        InstanceBuffer = new StructuredBuffer<Instance>(device);
        InstanceBuffer.EnsureCapacity(4);
        InstanceView = InstanceBuffer.CreateShaderResourceView(device);
    }

    public override string Title => "Render Instanced Model";

    public override void RenderInternal(DeviceContext context, Matrix4x4 viewProjection, float elapsed)
    {
        if (isDirty)
        {
            var instances = new Instance[4];
            for (var i = 0; i < instances.Length; i++)
            {
                var rad = (MathF.PI / instances.Length) * 2.0f;
                var x = MathF.Sin(rad * i) * 10.0f;
                var z = MathF.Cos(rad * i) * 10.0f;
                instances[i] = new() { World = Matrix4x4.CreateTranslation(new Vector3(x, 0, z)) };
            }

            InstanceBuffer.Write(context, instances);
            isDirty = false;
            instanceCount = (uint)instances.Length;
        }

        var constants = new Constants()
        {
            ViewProjection = viewProjection
        };
        ConstantBuffer.Write(context, [constants]);

        context.Setup(InputLayout, PrimitiveTopology.TriangleList, VertexShader, context.RasterizerStates.CullCounterClockwise, PixelShader, context.BlendStates.Opaque, context.DepthStencilStates.ReverseZ);
        context.VS.SetConstantBuffer(InstancedModelShader.ConstantsRegister, ConstantBuffer);
        context.VS.SetShaderResource(InstancedModelShader.Instances, InstanceView);
        context.PS.SetConstantBuffer(InstancedModelShader.ConstantsRegister, ConstantBuffer);

        const int lod = 0;
        Model.DrawIndexedInstanced(context, instanceCount, InstancedModelShader.Materials, InstancedModelShader.Triangles, lod);
    }

    public override void Dispose()
    {
        InstanceView.Dispose();
        InstanceBuffer.Dispose();
        ConstantBuffer.Dispose();
        base.Dispose();
    }
}
