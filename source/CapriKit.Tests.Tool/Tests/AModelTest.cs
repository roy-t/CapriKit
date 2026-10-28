using CapriKit.AssetPipeline;
using CapriKit.AssetPipeline.DirectX11.Models;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Resources;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.Mathematics;
using CapriKit.Tests.Tool.Tests.Framework;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace CapriKit.Tests.Tool.Tests;

internal sealed record GenericModelTestBundle(CKTModel Model, IVertexShader VertexShader, IPixelShader PixelShader);

internal abstract class AModelTest : ITestScreen
{
    public static ITestFactory CreateFactory<TTest>(AssetManager assets, string name, AssetId modelId, AssetId vertexShaderId, AssetId pixelShaderId,
         [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        where TTest : AModelTest
    {
        var builder = new AssetBundleBuilder<GenericModelTestBundle>(assets, file, line);
        var model = builder.Request<CKTModel>(modelId);
        var vertexShader = builder.Request<IVertexShader>(vertexShaderId);
        var pixelShader = builder.Request<IPixelShader>(pixelShaderId);
        var bundle = builder.Build(r => new GenericModelTestBundle(r.Get(model), r.Get(vertexShader), r.Get(pixelShader)));
        return new TestFactory<TTest, GenericModelTestBundle>(name, bundle);
    }

    protected readonly IVertexShader VertexShader;
    protected readonly IPixelShader PixelShader;
    protected readonly IInputLayout InputLayout;
    protected readonly CKTModel Model;
    protected readonly SwapChain SwapChain;
    protected readonly TurnTableCamera Camera;

    public AModelTest(Device device, SwapChain swapChain, TurnTableCamera camera, GenericModelTestBundle bundle, InputElementDescription[] elements)
    {
        VertexShader = bundle.VertexShader;
        PixelShader = bundle.PixelShader;
        InputLayout = VertexShader.CreateInputLayout(device, elements);

        Model = bundle.Model;
        SwapChain = swapChain;
        Camera = camera;
    }

    public abstract string Title { get; }

    public void Render(DeviceContext context, float elapsed)
    {
        Camera.Update();

        var view = Camera.GetViewMatrix();
        var projection = Matrix4x4.CreatePerspectiveFieldOfViewReverseZ(MathF.PI / 2, SwapChain.AspectRatio, 0.01f);

        RenderInternal(context, view * projection, elapsed);
    }

    public abstract void RenderInternal(DeviceContext context, Matrix4x4 viewProjection, float elapsed);

    public virtual void Dispose()
    {
        InputLayout.Dispose();
    }
}
