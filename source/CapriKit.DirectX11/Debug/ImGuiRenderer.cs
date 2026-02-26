using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Resources;
using ImGuiNET;
using System.Numerics;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace CapriKit.DirectX11.Debug;

// TODO: For IMGUI I should probably just render on the ImmediateContext as its a debug view
// for other rendering Tasks is probably not a good idea as I can't really await anything and
// I need to task.Run(..) and have overhead. Instead I should come up with something similar like I did
// in the old engine, but with better ergonomics. Like A DirectX11Job system that immediately starts work when
// its enqueued, separate from the simulation.
public sealed class ImGuiRenderer : IDisposable
{
    private const int FONT_TEXTURE_ID = 1;

    private readonly Device Device;
    private readonly VertexBuffer<ImDrawVert> VertexBuffer;
    private readonly IndexBufferU16 IndexBuffer;
    private readonly ConstantBuffer<Matrix4x4> ConstantBuffer;
    private readonly ImGuiEffect Effect;
    private readonly ID3D11Texture2D FontTexture;
    private readonly ID3D11ShaderResourceView FontTextureView;

    public ImGuiRenderer(Device device)
    {
        Device = device;
        VertexBuffer = new VertexBuffer<ImDrawVert>(device, nameof(ImGuiRenderer));
        IndexBuffer = new IndexBufferU16(device, nameof(ImGuiRenderer));
        ConstantBuffer = new ConstantBuffer<Matrix4x4>(device, nameof(ImGuiRenderer));
        Effect = new ImGuiEffect(device);

        var io = ImGui.GetIO();
        unsafe
        {
            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height);
            var format = Format.R8G8B8A8_UNorm;
            var pitch = (int)format.GetBytesPerPixel();
            var pixelSpan = new ReadOnlySpan<byte>(pixels, width * height * pitch);
            FontTexture = Device.ID3D11Device.CreateTexture2D<byte>(pixelSpan, format, (uint)width, (uint)height);
            FontTextureView = Device.ID3D11Device.CreateShaderResourceView(FontTexture);
        }
        io.Fonts.TexID = FONT_TEXTURE_ID;
    }

    internal void Render(ImDrawDataPtr data)
    {
        if (data.DisplaySize.X <= 0.0f || data.DisplaySize.Y <= 0.0f || data.TotalVtxCount <= 0)
        {
            return;
        }

        VertexBuffer.EnsureCapacity(data.TotalVtxCount, data.TotalVtxCount / 10);
        IndexBuffer.EnsureCapacity(data.TotalIdxCount, data.TotalIdxCount / 10);

        var vertexOffset = 0;
        var indexOffset = 0;
        using (var vertexWriter = VertexBuffer.OpenWriter(Device.ImmediateDeviceContext))
        using (var indexWriter = IndexBuffer.OpenWriter(Device.ImmediateDeviceContext))
        {
            for (var n = 0; n < data.CmdListsCount; n++)
            {
                var cmdlList = data.CmdLists[n];
                unsafe
                {
                    vertexWriter.Write(new Span<ImDrawVert>(cmdlList.VtxBuffer.Data.ToPointer(), cmdlList.VtxBuffer.Size), vertexOffset);
                    vertexOffset += cmdlList.VtxBuffer.Size;

                    indexWriter.Write(new Span<ushort>(cmdlList.IdxBuffer.Data.ToPointer(), cmdlList.IdxBuffer.Size), indexOffset);
                    indexOffset += cmdlList.IdxBuffer.Size;
                }
            }
        }

        // Setup orthographic projection matrix into our constant buffer
        var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, data.DisplaySize.X, data.DisplaySize.Y, 0, -1.0f, 1.0f);
        ConstantBuffer.Write(Device.ImmediateDeviceContext, [projectionMatrix]);

        SetupRenderState(data, Device.ImmediateDeviceContext);

        // Render command lists
        // (Because we merged all buffers into a single one, we maintain our own offset into them)
        var globalIndexOffset = 0u;
        var localVertexOffset = 0u;
        for (var n = 0; n < data.CmdListsCount; n++)
        {
            var cmdList = data.CmdLists[n];
            for (var i = 0; i < cmdList.CmdBuffer.Size; i++)
            {
                var cmd = cmdList.CmdBuffer[i];
                var left = (int)(cmd.ClipRect.X - data.DisplayPos.X);
                var top = (int)(cmd.ClipRect.Y - data.DisplayPos.Y);
                var right = (int)(cmd.ClipRect.Z - data.DisplayPos.X);
                var bottom = (int)(cmd.ClipRect.W - data.DisplayPos.Y);

                Device.ImmediateDeviceContext.RS.SetScissorRect(left, top, right - left, bottom - top);
                if (cmd.TextureId != FONT_TEXTURE_ID)
                {
                    throw new NotSupportedException("Unexpected texture id");
                }
                Device.ID3D11Device.ImmediateContext.PSSetShaderResource(0, FontTextureView);
                Device.ImmediateDeviceContext.DrawIndexed(cmd.ElemCount, cmd.IdxOffset + globalIndexOffset, (int)(cmd.VtxOffset + localVertexOffset));
            }
            globalIndexOffset += (uint)cmdList.IdxBuffer.Size;
            localVertexOffset += (uint)cmdList.VtxBuffer.Size;
        }
    }

    private void SetupRenderState(ImDrawDataPtr drawData, ImmediateDeviceContext context)
    {
        var output = new System.Drawing.Rectangle(0, 0, (int)drawData.DisplaySize.X, (int)drawData.DisplaySize.Y);
        context.Setup(Effect.InputLayout, PrimitiveTopology.TriangleList, Effect.VertexShader, Device.RasterizerStates.CullNone, in output, Effect.PixelShader, Device.BlendStates.NonPreMultiplied, Device.DepthStencilStates.None);
        context.IA.SetVertexBuffer(VertexBuffer);
        context.IA.SetIndexBuffer(IndexBuffer);
        context.VS.SetConstantBuffer(0, ConstantBuffer);
        context.PS.SetSampler(0, Device.SamplerStates.LinearWrap);
    }

    public void Dispose()
    {
        FontTextureView.Dispose();
        FontTexture.Dispose();
        Effect.Dispose();
        ConstantBuffer.Dispose();
        IndexBuffer.Dispose();
        VertexBuffer.Dispose();
    }

}
