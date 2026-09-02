using Raphael.Effects;
using static Raphael.Tests.TestHelpers;

namespace Raphael.Tests.Effects;

public class ResizeEffectTests
{
    [Fact]
    public void Apply_NoDimensions_DoesNothing()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var originalWidth = bitmap.Width;
        var originalHeight = bitmap.Height;

        var effect = new ResizeEffect { Width = 0, Height = 0 };
        effect.Apply(bitmap);

        Assert.Equal(originalWidth, bitmap.Width);
        Assert.Equal(originalHeight, bitmap.Height);
    }

    [Fact]
    public void Apply_WidthOnly_PreservesAspectRatio()
    {
        var bitmap = CreateTestBitmap(200, 100);
        var effect = new ResizeEffect { Width = 100, Height = 0, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(100, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_HeightOnly_PreservesAspectRatio()
    {
        var bitmap = CreateTestBitmap(200, 100);
        var effect = new ResizeEffect { Width = 0, Height = 50, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(100, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_BothDimensions_PreservesAspectRatio_MinRatio()
    {
        var bitmap = CreateTestBitmap(200, 100);
        var effect = new ResizeEffect { Width = 80, Height = 80, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(80, bitmap.Width);
        Assert.Equal(40, bitmap.Height);
    }

    [Fact]
    public void Apply_BothDimensions_PreserveAspectRatioFalse_Stretches()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new ResizeEffect { Width = 200, Height = 50, PreserveAspectRatio = false };
        effect.Apply(bitmap);

        Assert.Equal(200, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_MinimumSize_AtLeast1x1()
    {
        var bitmap = CreateTestBitmap(10, 10);
        var effect = new ResizeEffect { Width = 1, Height = 1, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(1, bitmap.Width);
        Assert.Equal(1, bitmap.Height);
    }

    [Fact]
    public void Apply_ZeroWidthNegativeHeight_ClampedTo1()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new ResizeEffect { Width = 0, Height = -5, PreserveAspectRatio = false };
        effect.Apply(bitmap);

        Assert.Equal(1, bitmap.Width);
        Assert.Equal(1, bitmap.Height);
    }

    [Fact]
    public void Apply_LargeDimensions_ScalesCorrectly()
    {
        var bitmap = CreateTestBitmap(100, 50);
        var effect = new ResizeEffect { Width = 400, Height = 200, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(400, bitmap.Width);
        Assert.Equal(200, bitmap.Height);
    }
}