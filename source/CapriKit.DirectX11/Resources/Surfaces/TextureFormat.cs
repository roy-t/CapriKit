using System.Text;
using Vortice.DXGI;

namespace CapriKit.DirectX11.Resources.Surfaces;

public readonly record struct TextureFormat
{
    public static readonly TextureFormat BC5_UNORM = new(Format.BC5_UNorm, 16, 4, 4);
    public static readonly TextureFormat BC7_UNORM = new(Format.BC7_UNorm, 16, 4, 4);
    public static readonly TextureFormat BC7_UNORM_SRGB = new(Format.BC7_UNorm_SRgb, 16, 4, 4);

    private TextureFormat(Format internalFormat, int bytesPerBlock, int blockWidth = 1, int blockHeight = 1)
    {
        InternalFormat = internalFormat;
        BytesPerBlock = bytesPerBlock;
        BlockWidth = blockWidth;
        BlockHeight = blockHeight;
    }

    public int BytesPerBlock { get; }
    public int BlockWidth { get; }
    public int BlockHeight { get; }
    internal Format InternalFormat { get; }

    /// <summary>
    /// Returns the size in bytes of one row of an image that is <paramref name="width"/> pixels wide.
    /// For block-compressed formats this is one row of blocks, a partially covered block counts as a whole block.
    /// </summary>
    public int GetRowPitch(int width)
    {
        var blocks = (width + BlockWidth - 1) / BlockWidth;
        return blocks * BytesPerBlock;
    }

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{nameof(InternalFormat)} = {InternalFormat}, {nameof(BytesPerBlock)} = {BytesPerBlock}, ");
        builder.Append($"{nameof(BlockWidth)} = {BlockWidth}, {nameof(BlockHeight)} = {BlockHeight}");
        return true;
    }
}
