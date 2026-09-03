using Raphael.Attributes;
using SkiaSharp;

namespace Raphael.Effects;

public enum ResizeQuality
{
    /// <summary>Nearest-neighbor sampling. Cheapest, blocky/jagged edges.</summary>
    Fast,
    /// <summary>Bilinear sampling. Good balance of speed and smooth edges.</summary>
    Smooth,
    /// <summary>Cubic (Mitchell) resampling. Slowest, sharpest and smoothest result.</summary>
    High
}

[Effect("Resize", Description = "Resize image to specified dimensions", Order = 1, Category = "Transform")]
public class ResizeEffect : IEffect
{
    public int Width { get; set; }
    public int Height { get; set; }
    public bool PreserveAspectRatio { get; set; } = true;
    public ResizeQuality ResizeQuality { get; set; } = ResizeQuality.Smooth;

    public void Apply(SKBitmap bitmap)
    {
        if (Width <= 0 && Height <= 0)
            return;

        int newWidth, newHeight;

        if (PreserveAspectRatio)
        {
            int originalWidth = bitmap.Width;
            int originalHeight = bitmap.Height;

            if (Width > 0 && Height > 0)
            {
                // Both dimensions specified - preserve aspect ratio
                float widthRatio = (float)Width / originalWidth;
                float heightRatio = (float)Height / originalHeight;
                float minRatio = Math.Min(widthRatio, heightRatio);

                newWidth = (int)(originalWidth * minRatio);
                newHeight = (int)(originalHeight * minRatio);
            }
            else if (Width > 0)
            {
                // Only width specified - calculate height maintaining aspect ratio
                newWidth = Width;
                newHeight = (int)(originalHeight * (float)Width / originalWidth);
            }
            else // Height > 0
            {
                // Only height specified - calculate width maintaining aspect ratio
                newHeight = Height;
                newWidth = (int)(originalWidth * (float)Height / originalHeight);
            }
        }
        else
        {
            // Stretch to exact dimensions
            newWidth = Width > 0 ? Width : bitmap.Width;
            newHeight = Height > 0 ? Height : bitmap.Height;
        }

        // Ensure at least 1x1
        newWidth = Math.Max(1, newWidth);
        newHeight = Math.Max(1, newHeight);

        var resized = bitmap.Resize(new SKImageInfo(newWidth, newHeight), GetSamplingOptions(ResizeQuality));
        resized?.CopyTo(bitmap);
    }

    private static SKSamplingOptions GetSamplingOptions(ResizeQuality quality) => quality switch
    {
        ResizeQuality.Fast => new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None),
        ResizeQuality.Smooth => new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear),
        ResizeQuality.High => new SKSamplingOptions(SKCubicResampler.Mitchell),
        _ => SKSamplingOptions.Default
    };
}