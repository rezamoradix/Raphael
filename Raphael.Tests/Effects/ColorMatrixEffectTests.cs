using Raphael.Effects;
using SkiaSharp;
using static Raphael.Tests.TestHelpers;

namespace Raphael.Tests.Effects;

public class ColorMatrixEffectTests
{
    private static readonly float[] Identity =
    [
        1, 0, 0, 0, 0,
        0, 1, 0, 0, 0,
        0, 0, 1, 0, 0,
        0, 0, 0, 1, 0,
    ];

    // Swaps the red and blue channels.
    private static readonly float[] SwapRedBlue =
    [
        0, 0, 1, 0, 0,
        0, 1, 0, 0, 0,
        1, 0, 0, 0, 0,
        0, 0, 0, 1, 0,
    ];

    [Fact]
    public void Apply_NullBitmap_Throws()
    {
        var effect = new ColorMatrixEffect { Matrix = Identity };
        Assert.Throws<ArgumentNullException>(() => effect.Apply(null!));
    }

    [Fact]
    public void Apply_IdentityMatrix_LeavesPixelsUnchanged()
    {
        using var bitmap = CreateTestBitmap(10, 10);

        new ColorMatrixEffect { Matrix = Identity }.Apply(bitmap);

        Assert.Equal(SKColors.Red, bitmap.GetPixel(5, 5));
    }

    [Fact]
    public void Apply_SwapMatrix_TurnsRedIntoBlue()
    {
        using var bitmap = CreateTestBitmap(10, 10);

        new ColorMatrixEffect { Matrix = SwapRedBlue }.Apply(bitmap);

        Assert.Equal(SKColors.Blue, bitmap.GetPixel(5, 5));
    }

    [Fact]
    public void Apply_KeepsDimensions()
    {
        using var bitmap = CreateTestBitmap(30, 20);

        new ColorMatrixEffect { Matrix = SwapRedBlue }.Apply(bitmap);

        Assert.Equal(30, bitmap.Width);
        Assert.Equal(20, bitmap.Height);
    }
}
