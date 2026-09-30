using Raphael.Extensions;
using SkiaSharp;

namespace Raphael.Tests.Extensions;

/// <summary>
/// A phone photo is stored sideways with an EXIF "rotate me" tag; browsers and viewers apply it, SKBitmap.Decode does
/// not - so Raphael rendered such photos rotated. DecodeOriented applies all eight EXIF orientations.
/// </summary>
public class ImageOrientationTests
{
    // A 32x16 picture of four 16x8 quadrants: top-left red, top-right green, bottom-left blue, bottom-right yellow.
    private static readonly SKColor Red = new(255, 0, 0), Green = new(0, 255, 0), Blue = new(0, 0, 255), Yellow = new(255, 255, 0);

    private static byte[] QuadrantJpeg(int? exifOrientation)
    {
        using var bitmap = new SKBitmap(32, 16);
        using (var canvas = new SKCanvas(bitmap))
        {
            using var paint = new SKPaint();
            void Fill(SKColor color, float x, float y)
            {
                paint.Color = color;
                canvas.DrawRect(x, y, 16, 8, paint);
            }
            Fill(Red, 0, 0); Fill(Green, 16, 0); Fill(Blue, 0, 8); Fill(Yellow, 16, 8);
        }
        using var image = SKImage.FromBitmap(bitmap);
        var jpeg = image.Encode(SKEncodedImageFormat.Jpeg, 100).ToArray();
        if (exifOrientation == null)
            return jpeg;
        byte[] app1 =
        [
            0xFF, 0xE1, 0x00, 0x22,                                   // APP1, length 34
            (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0,
            (byte)'M', (byte)'M', 0x00, 0x2A, 0, 0, 0, 8,             // big-endian TIFF header, first IFD at 8
            0x00, 0x01,                                               // one entry
            0x01, 0x12, 0x00, 0x03, 0, 0, 0, 1, 0x00, (byte)exifOrientation.Value, 0, 0, // Orientation (SHORT)
            0, 0, 0, 0,                                               // no next IFD
        ];
        return [.. jpeg[..2], .. app1, .. jpeg[2..]]; // straight after the SOI marker
    }

    private static bool Near(SKColor actual, SKColor expected)
        => Math.Abs(actual.Red - expected.Red) < 60 && Math.Abs(actual.Green - expected.Green) < 60 && Math.Abs(actual.Blue - expected.Blue) < 60;

    [Theory]
    // orientation, upright width, upright height, colour at the output's top-left corner
    [InlineData(1, 32, 16, "red")]
    [InlineData(2, 32, 16, "green")]   // mirrored horizontally: the top-right quadrant comes to the top-left
    [InlineData(3, 32, 16, "yellow")]  // rotated 180
    [InlineData(4, 32, 16, "blue")]    // mirrored vertically
    [InlineData(5, 16, 32, "red")]     // transposed
    [InlineData(6, 16, 32, "blue")]    // rotate 90 clockwise (the usual phone portrait): the bottom-left comes to the top-left
    [InlineData(7, 16, 32, "yellow")]  // transverse
    [InlineData(8, 16, 32, "green")]   // rotate 90 counter-clockwise: the top-right comes to the top-left
    public void Every_exif_orientation_comes_out_upright(int orientation, int width, int height, string topLeft)
    {
        using var bitmap = ImageExtensions.DecodeOriented(QuadrantJpeg(orientation));

        Assert.NotNull(bitmap);
        Assert.Equal((width, height), (bitmap!.Width, bitmap.Height));
        var expected = topLeft switch { "red" => Red, "green" => Green, "blue" => Blue, _ => Yellow };
        Assert.True(Near(bitmap.GetPixel(2, 2), expected), $"orientation {orientation}: top-left was {bitmap.GetPixel(2, 2)}, expected {topLeft}");
    }

    [Fact]
    public void A_picture_without_an_orientation_tag_is_left_exactly_as_stored()
    {
        using var plain = ImageExtensions.DecodeOriented(QuadrantJpeg(null));
        using var raw = SKBitmap.Decode(QuadrantJpeg(null));

        Assert.Equal((raw.Width, raw.Height), (plain!.Width, plain.Height));
        Assert.True(Near(plain.GetPixel(2, 2), Red));
    }

    [Fact]
    public void The_plain_decoder_really_does_ignore_the_tag_which_is_the_bug_this_fixes()
    {
        var sideways = QuadrantJpeg(6);
        using var raw = SKBitmap.Decode(sideways);
        using var upright = ImageExtensions.DecodeOriented(sideways);

        Assert.Equal((32, 16), (raw.Width, raw.Height));      // what Raphael used to render: landscape, sideways
        Assert.Equal((16, 32), (upright!.Width, upright.Height)); // what every viewer shows: portrait
    }

    [Fact]
    public void Garbage_does_not_decode()
        => Assert.Null(ImageExtensions.DecodeOriented([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]));
}
