using CapriKit.AssetHandles.Textures;
using CapriKit.AssetLibrary.Shaders;
using CapriKit.AssetPipeline;
using CapriKit.AssetPipeline.DirectX11.Textures;
using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Resources;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.DirectX11.Resources.Surfaces;
using CapriKit.Mathematics;
using CapriKit.Tests.Tool.Tests.Framework;
using Microsoft.Extensions.DependencyInjection;
using System.Numerics;
using static CapriKit.AssetLibrary.Shaders.TextureShader;


namespace CapriKit.Tests.Tool.Tests;

internal sealed record TextureTestBundle(IVertexShader VertexShader, IPixelShader PixelShader, Texture2D Diffuse, Texture2D Normal);

internal sealed class TextureTest : ITestScreen
{
    public static Func<AssetManager, AssetBundle<TextureTestBundle>> CreateBundleFactory()
    {
        return assetManager =>
        {
            var builder = new AssetBundleBuilder<TextureTestBundle>(assetManager);

            var vs = builder.Request<IVertexShader>(new AssetId(TextureShader.Path, TextureShader.Vs));
            var ps = builder.Request<IPixelShader>(new AssetId(TextureShader.Path, TextureShader.Ps));

            var diffuseSettings = new Texture2DSettings(TextureUsage.SRgb, MipMapGeneration.Wrap);
            var diffuse = builder.Request<Texture2D, Texture2DSettings>(new AssetId(GangesRiverPebblesDiff1k.png), diffuseSettings);

            var normalSettings = new Texture2DSettings(TextureUsage.NormalMap, MipMapGeneration.Wrap);
            var normal = builder.Request<Texture2D, Texture2DSettings>(new AssetId(GangesRiverPebblesNorDx1k.png), normalSettings);
            return builder.Build(r => new TextureTestBundle(
                r.Get(vs), r.Get(ps),
                r.Get(diffuse), r.Get(normal)));
        };
    }

    private readonly IVertexShader VertexShader;
    private readonly IPixelShader PixelShader;
    private readonly IInputLayout InputLayout;
    private readonly VertexBuffer<VsInput> VertexBuffer;
    private readonly IndexBuffer<ushort> IndexBuffer;
    private readonly ConstantBuffer<Constants> ConstantBuffer;

    private readonly SwapChain SwapChain;
    private readonly TurnTableCamera Camera;

    private readonly VsInput[] Vertices;
    private readonly ushort[] Indices;

    private readonly Texture2D Diffuse;
    private readonly Texture2D Normal;

    public string Title => "Render Texture with Normal Map";

    private bool isDirty;

    public TextureTest(Device device, SwapChain swapChain, TurnTableCamera camera, [FromKeyedServices(nameof(TextureTest))] TextureTestBundle bundle)
    {
        VertexShader = bundle.VertexShader;
        PixelShader = bundle.PixelShader;
        InputLayout = VertexShader.CreateInputLayout(device, VsInputElementDescription);
        VertexBuffer = new VertexBuffer<VsInput>(device);
        IndexBuffer = IndexBuffers.CreateU16(device);
        ConstantBuffer = new ConstantBuffer<Constants>(device);

        Diffuse = bundle.Diffuse;
        Normal = bundle.Normal;

        SwapChain = swapChain;
        Camera = camera;

        Indices = [0, 1, 2, 2, 3, 0];
        Vertices =
        [
            // br
            new VsInput(){ Position = new Vector4(2.0f, -2.0f, 0.0f, 1.0f), Normal = Vector3.UnitZ, Uv = new Vector2(1.0f, 1.0f)},
            // bl
            new VsInput(){ Position = new Vector4(-2.0f, -2.0f, 0.0f, 1.0f), Normal = Vector3.UnitZ, Uv = new Vector2(0.0f, 1.0f)},
            // tl
            new VsInput(){ Position = new Vector4(-2.0f, 2.0f, 0.0f, 1.0f), Normal = Vector3.UnitZ, Uv = new Vector2(0.0f, 0.0f)},
            // tr
            new VsInput(){ Position = new Vector4(2.0f, 2.0f, 0.0f, 1.0f), Normal = Vector3.UnitZ, Uv = new Vector2(1.0f, 0.0f)},
        ];

        isDirty = true;
    }

    public void Render(DeviceContext context, float _)
    {
        UploadData(context);
        context.Setup(InputLayout, PrimitiveTopology.TriangleList, VertexShader, context.RasterizerStates.CullCounterClockwise, PixelShader, context.BlendStates.NonPreMultiplied, context.DepthStencilStates.None);
        context.IA.SetVertexBuffer(VertexBuffer);
        context.IA.SetIndexBuffer(IndexBuffer);
        context.VS.SetConstantBuffer(TextureShader.ConstantsRegister, ConstantBuffer);
        context.PS.SetConstantBuffer(TextureShader.ConstantsRegister, ConstantBuffer);

        context.PS.SetShaderResource(TextureShader.Diffuse, Diffuse);
        context.PS.SetSampler(TextureShader.DiffuseSampler, context.SamplerStates.LinearWrap);

        context.PS.SetShaderResource(TextureShader.Normal, Normal);
        context.PS.SetSampler(TextureShader.NormalSampler, context.SamplerStates.LinearWrap);

        context.DrawIndexed(6);
    }

    private void UploadData(DeviceContext context)
    {
        if (isDirty)
        {
            VertexBuffer.Write(context, Vertices);
            IndexBuffer.Write(context, Indices);
            isDirty = false;
        }

        Camera.Update();
        var view = Camera.GetViewMatrix();
        var projection = Matrix4x4.CreatePerspectiveFieldOfViewReverseZ(MathF.PI / 2, SwapChain.AspectRatio, 0.01f);
        var constants = new Constants()
        {
            World = Matrix4x4.Identity,
            ViewProjection = view * projection,
        };
        ConstantBuffer.Write(context, [constants]);
    }

    public void Dispose()
    {
        ConstantBuffer.Dispose();
        IndexBuffer.Dispose();
        VertexBuffer.Dispose();
        InputLayout.Dispose();
    }
}
