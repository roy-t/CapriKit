using CapriKit.SuperCompressed;

namespace CapriKit.Tests.SuperCompressed;

internal class NativeMethodsTests
{
    [Test]
    public async Task NativeLibraryVersionMatchesWrapper()
    {
        await Assert.That(NativeMethods.bu_get_version()).IsEqualTo(NativeMethods.ExpectedLibVersion);
        await Assert.That(NativeMethods.bt_get_version()).IsEqualTo(NativeMethods.ExpectedLibVersion);
    }

    [Test]
    public async Task CompParamsLifecycle()
    {
        using var compParams = NativeMethods.bu_new_comp_params();
        await Assert.That(compParams.IsInvalid).IsFalse();

        var pixels = new byte[4 * 4 * 4];
        var imageSet = NativeMethods.bu_comp_params_set_image_rgba32(compParams, 0, pixels, 4, 4, 4 * 4);
        await Assert.That(imageSet).IsTrue();

        // Nothing has been compressed yet
        await Assert.That(NativeMethods.bu_comp_params_get_comp_data_size(compParams)).IsEqualTo(0ul);
        await Assert.That(NativeMethods.bu_comp_params_clear(compParams)).IsTrue();
    }

    [Test]
    public async Task TranscodeFormatHelpers()
    {
        // 4x4 pixels at 4 bytes per pixel
        var rgbaSize = NativeMethods.bt_basis_compute_transcoded_image_size_in_bytes(TranscodeFormat.Rgba32, 4, 4);
        await Assert.That(rgbaSize).IsEqualTo(64u);

        await Assert.That(NativeMethods.bt_basis_get_bytes_per_block_or_pixel(TranscodeFormat.Bc7Rgba)).IsEqualTo(16u);
        await Assert.That(NativeMethods.bt_basis_transcoder_format_is_uncompressed(TranscodeFormat.Rgba32)).IsTrue();
        await Assert.That(NativeMethods.bt_basis_transcoder_format_has_alpha(TranscodeFormat.Bc7Rgba)).IsTrue();
    }
}
