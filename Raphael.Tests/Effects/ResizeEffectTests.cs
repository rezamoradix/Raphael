using Raphael.Effects;
using SkiaSharp;
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
    public void Apply_ZeroWidthNegativeHeight_EarlyReturn_DoesNothing()
    {
        // Guard clause is "if (Width <= 0 && Height <= 0) return;". Width=0 and
        // Height=-5 are BOTH <= 0, so the method returns immediately and no
        // resize (and no clamping) ever happens.
        var bitmap = CreateTestBitmap(100, 100);

        var effect = new ResizeEffect { Width = 0, Height = -5, PreserveAspectRatio = false };
        effect.Apply(bitmap);

        Assert.Equal(100, bitmap.Width);
        Assert.Equal(100, bitmap.Height);
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

    [Fact]
    public void Apply_WidthOnly_NoAspectRatio_WidthChangesHeightStaysSame()
    {
        var bitmap = CreateTestBitmap(200, 100);
        var effect = new ResizeEffect { Width = 150, Height = 0, PreserveAspectRatio = false };
        effect.Apply(bitmap);

        Assert.Equal(150, bitmap.Width);
        Assert.Equal(100, bitmap.Height);
    }

    [Fact]
    public void Apply_HeightOnly_NoAspectRatio_HeightChangesWidthStaysSame()
    {
        var bitmap = CreateTestBitmap(200, 100);
        var effect = new ResizeEffect { Width = 0, Height = 75, PreserveAspectRatio = false };
        effect.Apply(bitmap);

        Assert.Equal(200, bitmap.Width);
        Assert.Equal(75, bitmap.Height);
    }

    [Fact]
    public void Apply_BothDimensionsZero_DoesNothing()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var originalWidth = bitmap.Width;
        var originalHeight = bitmap.Height;

        var effect = new ResizeEffect { Width = 0, Height = 0, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(originalWidth, bitmap.Width);
        Assert.Equal(originalHeight, bitmap.Height);
    }

    [Fact]
    public void Apply_NegativeWidth_ZeroHeight_EarlyReturn_DoesNothing()
    {
        // Width=-10, Height=0: both <= 0, so the guard clause returns immediately
        // before any clamping logic runs.
        var bitmap = CreateTestBitmap(100, 100);

        var effect = new ResizeEffect { Width = -10, Height = 0, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(100, bitmap.Width);
        Assert.Equal(100, bitmap.Height);
    }

    [Fact]
    public void Apply_ZeroWidth_NegativeHeight_EarlyReturn_DoesNothing()
    {
        // Width=0, Height=-10: both <= 0, so the guard clause returns immediately.
        var bitmap = CreateTestBitmap(100, 200);

        var effect = new ResizeEffect { Width = 0, Height = -10, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(100, bitmap.Width);
        Assert.Equal(200, bitmap.Height);
    }

    [Fact]
    public void Apply_NegativeDimensions_NoAspectRatio_EarlyReturn_DoesNothing()
    {
        // Width=-10, Height=-20: both <= 0, so the guard clause returns immediately,
        // regardless of PreserveAspectRatio.
        var bitmap = CreateTestBitmap(100, 100);

        var effect = new ResizeEffect { Width = -10, Height = -20, PreserveAspectRatio = false };
        effect.Apply(bitmap);

        Assert.Equal(100, bitmap.Width);
        Assert.Equal(100, bitmap.Height);
    }

    [Fact]
    public void Apply_WidthOnly_WithAspectRatio_ZeroWidthPreventsChange()
    {
        var bitmap = CreateTestBitmap(100, 100);

        var effect = new ResizeEffect { Width = 0, Height = 50, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_HeightOnly_WithAspectRatio_ZeroHeightPreventsChange()
    {
        var bitmap = CreateTestBitmap(200, 100);
        var effect = new ResizeEffect { Width = 100, Height = 0, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(100, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_UpScale_PreservesAspectRatio()
    {
        var bitmap = CreateTestBitmap(50, 50);
        var effect = new ResizeEffect { Width = 200, Height = 200, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(200, bitmap.Width);
        Assert.Equal(200, bitmap.Height);
    }

    [Fact]
    public void Apply_UpScale_NoAspectRatio()
    {
        var bitmap = CreateTestBitmap(50, 50);
        var effect = new ResizeEffect { Width = 200, Height = 100, PreserveAspectRatio = false };
        effect.Apply(bitmap);

        Assert.Equal(200, bitmap.Width);
        Assert.Equal(100, bitmap.Height);
    }

    [Fact]
    public void Apply_DownScale_PreservesAspectRatio()
    {
        var bitmap = CreateTestBitmap(200, 100);
        var effect = new ResizeEffect { Width = 50, Height = 25, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(25, bitmap.Height);
    }

    [Fact]
    public void Apply_DownScale_NoAspectRatio()
    {
        var bitmap = CreateTestBitmap(200, 100);
        var effect = new ResizeEffect { Width = 50, Height = 50, PreserveAspectRatio = false };
        effect.Apply(bitmap);

        Assert.Equal(50, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_AspectRatio_WithWidthOnly_UsesAspectRatio()
    {
        var bitmap = CreateTestBitmap(300, 200);
        var effect = new ResizeEffect { Width = 150, Height = 0, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        // 300:200 = 3:2, so 150 width → 100 height
        Assert.Equal(150, bitmap.Width);
        Assert.Equal(100, bitmap.Height);
    }

    [Fact]
    public void Apply_AspectRatio_WithHeightOnly_UsesAspectRatio()
    {
        var bitmap = CreateTestBitmap(300, 200);
        var effect = new ResizeEffect { Width = 0, Height = 100, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        // 300:200 = 3:2, so 100 height → 150 width
        Assert.Equal(150, bitmap.Width);
        Assert.Equal(100, bitmap.Height);
    }

    [Fact]
    public void Apply_AspectRatio_BothDimensions_ChooseSmallerRatio()
    {
        var bitmap = CreateTestBitmap(400, 300);
        var effect = new ResizeEffect { Width = 200, Height = 100, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        // widthRatio = 200/400 = 0.5, heightRatio = 100/300 = 0.333
        // minRatio = 0.333, so 400*0.333=133, 300*0.333=100
        Assert.Equal(133, bitmap.Width);
        Assert.Equal(100, bitmap.Height);
    }

    [Fact]
    public void Apply_ZeroWidth_NoAspectRatio_UsesOriginalWidth()
    {
        var bitmap = CreateTestBitmap(100, 50);
        var effect = new ResizeEffect { Width = 0, Height = 75, PreserveAspectRatio = false };
        effect.Apply(bitmap);

        Assert.Equal(100, bitmap.Width);
        Assert.Equal(75, bitmap.Height);
    }

    [Fact]
    public void Apply_ZeroHeight_NoAspectRatio_UsesOriginalHeight()
    {
        var bitmap = CreateTestBitmap(100, 50);
        var effect = new ResizeEffect { Width = 150, Height = 0, PreserveAspectRatio = false };
        effect.Apply(bitmap);

        Assert.Equal(150, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_BothZero_NoAspectRatio_DoesNothing()
    {
        // Both <= 0 hits the guard clause, so this returns before the
        // PreserveAspectRatio=false branch (which would otherwise fall back to
        // bitmap.Width/bitmap.Height, i.e. a no-op resize) ever runs.
        var bitmap = CreateTestBitmap(100, 100);

        var effect = new ResizeEffect { Width = 0, Height = 0, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(100, bitmap.Width);
        Assert.Equal(100, bitmap.Height);
    }

    [Fact]
    public void Apply_NegativeWidth_PreserveAspectRatio_EarlyReturn_DoesNothing()
    {
        // Width=-200, Height=0: both <= 0, so the guard clause returns immediately.
        // Negative values are never actually clamped to 1 in the current code -
        // that only happens for combinations that pass the initial guard
        // (e.g. one positive dimension and one negative one).
        var bitmap = CreateTestBitmap(100, 50);

        var effect = new ResizeEffect { Width = -200, Height = 0, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(100, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Apply_PreserveAspectRatio_BothDimensionsNegative_EarlyReturn_DoesNothing()
    {
        // Width=-50, Height=-50: both <= 0, guard clause returns immediately.
        var bitmap = CreateTestBitmap(100, 100);

        var effect = new ResizeEffect { Width = -50, Height = -50, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(100, bitmap.Width);
        Assert.Equal(100, bitmap.Height);
    }

    [Fact]
    public void Apply_ZeroWidthWithNegativeHeight_NoAspectRatio_EarlyReturn_DoesNothing()
    {
        // Width=0, Height=-10: both <= 0, guard clause returns immediately -
        // the "Width=0 keeps original, Height clamps to 1" path never executes
        // because the method never gets past the initial check.
        var bitmap = CreateTestBitmap(100, 100);

        var effect = new ResizeEffect { Width = 0, Height = -10, PreserveAspectRatio = false };
        effect.Apply(bitmap);

        Assert.Equal(100, bitmap.Width);
        Assert.Equal(100, bitmap.Height);
    }

    [Fact]
    public void Apply_ResizeToSameSize_DoesNothing()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var originalWidth = bitmap.Width;
        var originalHeight = bitmap.Height;

        var effect = new ResizeEffect { Width = 100, Height = 100, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        Assert.Equal(originalWidth, bitmap.Width);
        Assert.Equal(originalHeight, bitmap.Height);
    }

    [Fact]
    public void Apply_ResizeToSameSize_NoAspectRatio_DoesNothing()
    {
        var bitmap = CreateTestBitmap(100, 100);
        var originalWidth = bitmap.Width;
        var originalHeight = bitmap.Height;

        var effect = new ResizeEffect { Width = 100, Height = 100, PreserveAspectRatio = false };
        effect.Apply(bitmap);

        Assert.Equal(originalWidth, bitmap.Width);
        Assert.Equal(originalHeight, bitmap.Height);
    }

    [Fact]
    public void Apply_ResizeToSameAspectRatio_DifferentSize()
    {
        var bitmap = CreateTestBitmap(200, 100);
        var effect = new ResizeEffect { Width = 400, Height = 0, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        // 200:100 = 2:1, so 400 width → 200 height
        Assert.Equal(400, bitmap.Width);
        Assert.Equal(200, bitmap.Height);
    }

    [Fact]
    public void Apply_ResizeWithAspectRatio_FloatingPointTruncation()
    {
        var bitmap = CreateTestBitmap(300, 200);
        var effect = new ResizeEffect { Width = 100, Height = 0, PreserveAspectRatio = true };
        effect.Apply(bitmap);

        // 300:200 = 3:2, 200 * (100/300) = 66.666...
        // Result is cast to int, which truncates (not rounds) to 66.
        Assert.Equal(100, bitmap.Width);
        Assert.Equal(66, bitmap.Height);
    }
}