using Raphael.Effects;
using SkiaSharp;
using static Raphael.Tests.TestHelpers;

namespace Raphael.Tests.Effects;

public class CropEffectTests
{
    [Fact]
    public void Apply_ZeroDimensions_DoesNothing()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var originalWidth = bitmap.Width;
        var originalHeight = bitmap.Height;

        var effect = new CropEffect { X = 0, Y = 0, Width = 0, Height = 0 };
        effect.Apply(bitmap);

        Assert.Equal(originalWidth, bitmap.Width);
        Assert.Equal(originalHeight, bitmap.Height);
    }

    [Fact]
    public void Apply_NegativeDimensions_DoesNothing()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var originalWidth = bitmap.Width;
        var originalHeight = bitmap.Height;

        var effect = new CropEffect { X = 10, Y = 10, Width = -5, Height = 50 };
        effect.Apply(bitmap);

        Assert.Equal(originalWidth, bitmap.Width);
        Assert.Equal(originalHeight, bitmap.Height);
    }

    [Fact]
    public void Apply_BasicCrop_Works()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { X = 10, Y = 10, Width = 50, Height = 50, UseAnchor = false };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_CropAtOrigin_Works()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { X = 0, Y = 0, Width = 50, Height = 50, UseAnchor = false };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_CropExceedsBounds_Clamped()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { X = 80, Y = 80, Width = 50, Height = 50, UseAnchor = false };
        effect.Apply(bitmap);

        Assert.Equal(20, bitmap.Width);
        Assert.Equal(20, bitmap.Height);
    }

    [Fact]
    public void Apply_CropCompletelyOutsideBounds_DoesNothing()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var originalWidth = bitmap.Width;
        var originalHeight = bitmap.Height;

        var effect = new CropEffect { X = 100, Y = 100, Width = 50, Height = 50, UseAnchor = false };
        effect.Apply(bitmap);

        Assert.Equal(originalWidth, bitmap.Width);
        Assert.Equal(originalHeight, bitmap.Height);
    }

    [Fact]
    public void Apply_CropCompletelyOutsideBounds_Negative_ClampsAndCropsFromZero()
    {
        // X/Y clamp to 0 before cropWidth/cropHeight are computed, and the code uses
        // the CLAMPED x/y (not the original negative value) when computing
        // cropWidth = Math.Min(Width, bitmap.Width - cropX). So a large negative
        // offset does NOT push the crop fully out of bounds - it just becomes
        // equivalent to cropping from (0,0) with the given Width/Height.
        var bitmap = CreateTestBitmap(100, 100);

        var effect = new CropEffect { X = -150, Y = -150, Width = 50, Height = 50, UseAnchor = false };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_CropStartsOutside_LeftEdge()
    {
        // X=-20 clamps to cropX=0. cropWidth = Math.Min(Width, bitmap.Width - cropX)
        // uses the clamped cropX, so this evaluates to Math.Min(50, 100 - 0) = 50,
        // NOT 30 as the "visible pixels from -20" interpretation would suggest.
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { X = -20, Y = 10, Width = 50, Height = 40, UseAnchor = false };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(40, bitmap.Height);
    }

    [Fact]
    public void Apply_CropStartsOutside_TopEdge()
    {
        // Same clamping quirk as the left-edge case, applied to Y/Height.
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { X = 10, Y = -20, Width = 40, Height = 50, UseAnchor = false };
        effect.Apply(bitmap);

        Assert.Equal(40, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_CropExtendsBeyondRightEdge()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { X = 60, Y = 10, Width = 60, Height = 40, UseAnchor = false };
        effect.Apply(bitmap);

        Assert.Equal(40, bitmap.Width);  // 100 - 60 = 40px visible
        Assert.Equal(40, bitmap.Height);
    }

    [Fact]
    public void Apply_CropExtendsBeyondBottomEdge()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { X = 10, Y = 60, Width = 40, Height = 60, UseAnchor = false };
        effect.Apply(bitmap);

        Assert.Equal(40, bitmap.Width);
        Assert.Equal(40, bitmap.Height); // 100 - 60 = 40px visible
    }

    [Fact]
    public void Apply_UseAnchor_TopLeft()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { Width = 50, Height = 50, UseAnchor = true, Anchor = CropAnchor.TopLeft };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_UseAnchor_TopCenter()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { Width = 50, Height = 50, UseAnchor = true, Anchor = CropAnchor.TopCenter };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_UseAnchor_TopRight()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { Width = 50, Height = 50, UseAnchor = true, Anchor = CropAnchor.TopRight };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_UseAnchor_CenterLeft()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { Width = 50, Height = 50, UseAnchor = true, Anchor = CropAnchor.CenterLeft };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_UseAnchor_Center()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { Width = 50, Height = 50, UseAnchor = true, Anchor = CropAnchor.Center };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_UseAnchor_CenterRight()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { Width = 50, Height = 50, UseAnchor = true, Anchor = CropAnchor.CenterRight };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_UseAnchor_BottomLeft()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { Width = 50, Height = 50, UseAnchor = true, Anchor = CropAnchor.BottomLeft };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_UseAnchor_BottomCenter()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { Width = 50, Height = 50, UseAnchor = true, Anchor = CropAnchor.BottomCenter };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_UseAnchor_BottomRight()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { Width = 50, Height = 50, UseAnchor = true, Anchor = CropAnchor.BottomRight };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_CropLargerThanImage_Clamped()
    {
        var bitmap = CreateTestBitmap(50, 50);
        var effect = new CropEffect { X = 0, Y = 0, Width = 100, Height = 100, UseAnchor = false };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_NegativeCoordinates_ClampedToZero()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { X = -10, Y = -10, Width = 50, Height = 50, UseAnchor = false };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_EmptyCropAfterClamp_DoesNothing()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var originalWidth = bitmap.Width;
        var originalHeight = bitmap.Height;

        var effect = new CropEffect { X = 100, Y = 100, Width = 50, Height = 50, UseAnchor = false };
        effect.Apply(bitmap);

        Assert.Equal(originalWidth, bitmap.Width);
        Assert.Equal(originalHeight, bitmap.Height);
    }

    [Fact]
    public void Apply_AnchorWithDimensionsExceedingBounds_ClampsCorrectly()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var effect = new CropEffect { Width = 150, Height = 150, UseAnchor = true, Anchor = CropAnchor.Center };
        effect.Apply(bitmap);

        // Should clamp to image bounds
        Assert.Equal(100, bitmap.Width);
        Assert.Equal(100, bitmap.Height);
    }

    [Fact]
    public void Apply_AnchorWithNegativeCalculations_ClampsCorrectly()
    {
        var bitmap = CreateTestBitmap(50, 50);
        var effect = new CropEffect { Width = 100, Height = 100, UseAnchor = true, Anchor = CropAnchor.Center };
        effect.Apply(bitmap);

        // Should clamp to image bounds
        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }
}