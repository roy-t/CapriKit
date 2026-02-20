using CapriKit.DirectX11.Resources;
using CapriKit.DirectX11.Resources.Shaders;
using System.Runtime.InteropServices;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace CapriKit.DirectX11.Debug;

[StructLayout(LayoutKind.Sequential)]
internal struct VS_INPUT
{
    public System.Numerics.Vector2 Pos;
    public System.Numerics.Vector2 Tex;
    public System.Numerics.Vector4 Col;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PS_INPUT
{
    public System.Numerics.Vector4 Pos;
    public System.Numerics.Vector2 Tex;
    public System.Numerics.Vector4 Col;
}

[StructLayout(LayoutKind.Explicit, Size = 64)]
internal struct Constants
{
    [FieldOffset(0)]
    public System.Numerics.Matrix4x4 ProjectionMatrix;
}

internal sealed class ImGuiShader : IVertexShader, IPixelShader, IInputLayout, IDisposable
{
    private static readonly ShaderMacro[] Defines = [];

    internal ID3D11VertexShader VertexShader { get; }
    internal ID3D11PixelShader PixelShader { get; }

    internal ID3D11InputLayout InputLayout { get; }
    ID3D11VertexShader IVertexShader.ID3D11VertexShader => VertexShader;
    ID3D11PixelShader IPixelShader.ID3D11PixelShader => PixelShader;

    ID3D11InputLayout IInputLayout.ID3D11InputLayout => InputLayout;

    internal ImGuiShader(ID3D11Device device)
    {
        Compiler.Compile(ShaderSource, [], "VS", "ImGuiShader.vs", "vs_5_0", out var vsBlob, out var vsErrors);
        ShaderBlobAnalyzer.ThrowOnWarningOrError(vsErrors.AsSpan());

        Compiler.Compile(ShaderSource, [], "PS", "ImGuiShader.ps", "ps_5_0", out var psBlob, out var psErrors);
        ShaderBlobAnalyzer.ThrowOnWarningOrError(psErrors.AsSpan());

        VertexShader = device.CreateVertexShader(vsBlob);
        PixelShader = device.CreatePixelShader(psBlob);

        var elements = new InputElementDescription[]
        {
            new("POSITION", 0, Format.R32G32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new("TEXCOORD", 0, Format.R32G32_Float, 8, 0, InputClassification.PerVertexData, 0),
            new("COLOR", 0, Format.R8G8B8A8_UNorm, 16, 0, InputClassification.PerVertexData, 0)
        };

        InputLayout = device.CreateInputLayout(elements, vsBlob);

        vsBlob.Dispose();
        psBlob.Dispose();
    }

    public void Dispose()
    {
        VertexShader.Dispose();
        PixelShader.Dispose();
        InputLayout.Dispose();
    }

    private static readonly string ShaderSource = """
        struct VS_INPUT
        {
            float2 pos : POSITION;
            float2 tex : TEXCOORD;
            float4 col : COLOR;
        };

        struct PS_INPUT
        {
            float4 pos : SV_POSITION;
            float2 tex : TEXCOORD;
            float4 col : COLOR;
        };

        cbuffer Constants : register(b0)
        {
            float4x4 ProjectionMatrix;
        };

        sampler TextureSampler : register(s0);
        Texture2D Texture : register(t0);

        static const float GAMMA = 2.2f;
        static float4 ToLinear(float4 v)
        {
            float3 rgb = pow(abs(v.rgb), float3(GAMMA, GAMMA, GAMMA));
            return float4(rgb.rgb, v.a);
        }

        PS_INPUT VS(VS_INPUT input)
        {
            PS_INPUT output;
            output.pos = mul(ProjectionMatrix, float4(input.pos.xy, 0.0f, 1.0f));
            output.col = ToLinear(input.col);
            output.tex = input.tex;
            return output;
        }

        float4 PS(PS_INPUT input) : SV_Target
        {
            float4 out_col = input.col * Texture.Sample(TextureSampler, input.tex);
            return out_col;
        }
        """;
}
