using Raphael.Extensions;
using Raphael.Models;
using SkiaSharp;

namespace Raphael.Tests.Extensions;

/// <summary>Covers DetectFormat (used by RaphaelRouteExtensions to pick the *output* format
/// when a request has no explicit "format" query param) and confirms encoding a bitmap with a
/// transparent pixel as Png actually keeps its alpha channel - the bug this exists to catch: a
/// transparent Png silently defaulting to Jpeg (no alpha channel at all) and flattening onto an
/// opaque background.</summary>
public class ImageExtensionsTests
{
    private static byte[] EncodeSourceBytes(ImageFormat format)
    {
        using var bitmap = new SKBitmap(4, 4, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(10, 20, 30, 128));
        return bitmap.Encode(format, quality: 90);
    }

    [Fact]
    public void DetectFormat_recognizes_a_png_source()
    {
        var pngBytes = EncodeSourceBytes(ImageFormat.Png);

        Assert.Equal(ImageFormat.Png, pngBytes.DetectFormat());
    }

    [Fact]
    public void DetectFormat_recognizes_a_jpeg_source()
    {
        var jpegBytes = EncodeSourceBytes(ImageFormat.Jpeg);

        Assert.Equal(ImageFormat.Jpeg, jpegBytes.DetectFormat());
    }

    [Fact]
    public void DetectFormat_falls_back_to_jpeg_for_bytes_that_are_not_a_recognizable_image()
    {
        var garbage = new byte[] { 1, 2, 3, 4, 5 };

        Assert.Equal(ImageFormat.Jpeg, garbage.DetectFormat());
    }

    [Fact]
    public void Encode_as_png_preserves_the_alpha_channel()
    {
        using var bitmap = new SKBitmap(4, 4, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using (var canvas = new SKCanvas(bitmap))
            canvas.Clear(new SKColor(200, 50, 50, 0)); // fully transparent

        var pngBytes = bitmap.Encode(ImageFormat.Png, quality: 90);

        using var roundTripped = SKBitmap.Decode(pngBytes);
        Assert.NotNull(roundTripped);
        Assert.True(roundTripped!.ColorType is SKColorType.Rgba8888 or SKColorType.Bgra8888);
        Assert.Equal(0, roundTripped.GetPixel(0, 0).Alpha);
    }

    [Fact]
    public void Encode_as_jpeg_has_no_alpha_channel()
    {
        using var bitmap = new SKBitmap(4, 4, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using (var canvas = new SKCanvas(bitmap))
            canvas.Clear(new SKColor(200, 50, 50, 0));

        var jpegBytes = bitmap.Encode(ImageFormat.Jpeg, quality: 90);

        using var roundTripped = SKBitmap.Decode(jpegBytes);
        Assert.NotNull(roundTripped);
        // Jpeg has no alpha channel at all - every pixel decodes fully opaque regardless of what
        // was cleared into the source bitmap, unlike the Png round-trip above.
        Assert.Equal(255, roundTripped!.GetPixel(0, 0).Alpha);
    }
}
