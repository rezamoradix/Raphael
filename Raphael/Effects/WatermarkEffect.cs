using Raphael.Attributes;
using SkiaSharp;

namespace Raphael.Effects;

public enum WatermarkPosition
{
    TopLeft,
    TopCenter,
    TopRight,
    CenterLeft,
    Center,
    CenterRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

public enum WatermarkType
{
    Text,
    Image
}

[Effect("Watermark", Description = "Add text or image watermark", Order = 3, Category = "Overlay")]
public class WatermarkEffect : IEffect
{
    // Common properties
    public WatermarkType Type { get; set; } = WatermarkType.Text;
    public WatermarkPosition Position { get; set; } = WatermarkPosition.BottomRight;
    public int Margin { get; set; } = 20;
    public float Opacity { get; set; } = 0.7f; // 0.0 to 1.0

    // Text watermark properties
    public string? Text { get; set; } = "© Watermark";
    public string FontFamily { get; set; } = "Arial";
    public float FontSize { get; set; } = 48;
    public SKColor TextColor { get; set; } = SKColors.White;
    public bool TextShadow { get; set; } = true;
    public SKColor ShadowColor { get; set; } = SKColors.Black;
    public float ShadowOffsetX { get; set; } = 2;
    public float ShadowOffsetY { get; set; } = 2;
    public float ShadowBlur { get; set; } = 4;

    // Image watermark properties
    public byte[]? WatermarkImage { get; set; }
    public string? WatermarkImageUrl { get; set; }
    public float Scale { get; set; } = 0.25f; // Relative to main image size
    public bool PreserveImageAspectRatio { get; set; } = true;

public void Apply(SKBitmap bitmap)
        {
            if (bitmap == null || bitmap.Width == 0 || bitmap.Height == 0)
                return;

            using var canvas = new SKCanvas(bitmap);

            switch (Type)
            {
                case WatermarkType.Text:
                    ApplyTextWatermark(canvas, bitmap);
                    break;
                case WatermarkType.Image:
                    ApplyImageWatermark(canvas, bitmap);
                    break;
            }
        }

private void ApplyTextWatermark(SKCanvas canvas, SKBitmap bitmap)
        {
            if (string.IsNullOrEmpty(Text))
                return;

            using var font = new SKFont(SKTypeface.FromFamilyName(FontFamily), FontSize);
            using var paint = new SKPaint
            {
                Color = TextColor.WithAlpha((byte)(Opacity * 255)),
                IsAntialias = true
            };

            // Measure text
            var textWidth = font.MeasureText(Text, paint);
            var textHeight = font.Metrics.Descent - font.Metrics.Ascent;

            // Calculate position
            var position = CalculatePosition(
                bitmap.Width, bitmap.Height,
                textWidth, textHeight,
                Position, Margin);

            // Draw shadow if enabled
            if (TextShadow)
            {
                using var shadowFont = new SKFont(SKTypeface.FromFamilyName(FontFamily), FontSize);
                using var shadowPaint = new SKPaint
                {
                    Color = ShadowColor.WithAlpha((byte)(Opacity * 128)),
                    IsAntialias = true,
                    MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, ShadowBlur)
                };
                canvas.DrawText(Text, position.X + ShadowOffsetX, position.Y + ShadowOffsetY, SKTextAlign.Left, shadowFont, shadowPaint);
            }

            // Draw text
            canvas.DrawText(Text, position.X, position.Y, SKTextAlign.Left, font, paint);
        }

private void ApplyImageWatermark(SKCanvas canvas, SKBitmap bitmap)
        {
            SKBitmap? watermarkBitmap = null;

            try
            {
                // Load watermark image from bytes only (URL loading removed for sync interface)
                if (WatermarkImage != null && WatermarkImage.Length > 0)
                {
                    watermarkBitmap = SKBitmap.Decode(WatermarkImage);
                }

                if (watermarkBitmap == null)
                    return;

                // Calculate scaled dimensions
                int watermarkWidth = watermarkBitmap.Width;
                int watermarkHeight = watermarkBitmap.Height;

                if (Scale > 0)
                {
                    float scaleFactor = Math.Min(
                        (bitmap.Width * Scale) / watermarkWidth,
                        (bitmap.Height * Scale) / watermarkHeight
                    );

                    if (PreserveImageAspectRatio)
                    {
                        watermarkWidth = (int)(watermarkWidth * scaleFactor);
                        watermarkHeight = (int)(watermarkHeight * scaleFactor);
                    }
                    else
                    {
                        watermarkWidth = (int)(bitmap.Width * Scale);
                        watermarkHeight = (int)(bitmap.Height * Scale);
                    }
                }

                // Ensure minimum size
                watermarkWidth = Math.Max(1, watermarkWidth);
                watermarkHeight = Math.Max(1, watermarkHeight);

                // Resize watermark if needed
                if (watermarkWidth != watermarkBitmap.Width || watermarkHeight != watermarkBitmap.Height)
                {
                    var resizedWatermark = watermarkBitmap.Resize(
                        new SKImageInfo(watermarkWidth, watermarkHeight),
                        SKSamplingOptions.Default);
                    if (resizedWatermark != null)
                    {
                        watermarkBitmap.Dispose();
                        watermarkBitmap = resizedWatermark;
                    }
                }

                // Calculate position
                var position = CalculatePosition(
                    bitmap.Width, bitmap.Height,
                    watermarkWidth, watermarkHeight,
                    Position, Margin);

                // Draw watermark with opacity
                using var paint = new SKPaint
                {
                    Color = SKColors.White.WithAlpha((byte)(Opacity * 255)),
                    IsAntialias = true
                };

                canvas.DrawBitmap(watermarkBitmap, position.X, position.Y, paint);
            }
            finally
            {
                watermarkBitmap?.Dispose();
            }
        }

    private SKPoint CalculatePosition(
        int canvasWidth, int canvasHeight,
        float elementWidth, float elementHeight,
        WatermarkPosition position, int margin)
    {
        float x = margin;
        float y = margin;

        switch (position)
        {
            case WatermarkPosition.TopLeft:
                x = margin;
                y = margin;
                break;
            case WatermarkPosition.TopCenter:
                x = (canvasWidth - elementWidth) / 2;
                y = margin;
                break;
            case WatermarkPosition.TopRight:
                x = canvasWidth - elementWidth - margin;
                y = margin;
                break;
            case WatermarkPosition.CenterLeft:
                x = margin;
                y = (canvasHeight - elementHeight) / 2;
                break;
            case WatermarkPosition.Center:
                x = (canvasWidth - elementWidth) / 2;
                y = (canvasHeight - elementHeight) / 2;
                break;
            case WatermarkPosition.CenterRight:
                x = canvasWidth - elementWidth - margin;
                y = (canvasHeight - elementHeight) / 2;
                break;
            case WatermarkPosition.BottomLeft:
                x = margin;
                y = canvasHeight - elementHeight - margin;
                break;
            case WatermarkPosition.BottomCenter:
                x = (canvasWidth - elementWidth) / 2;
                y = canvasHeight - elementHeight - margin;
                break;
            case WatermarkPosition.BottomRight:
                x = canvasWidth - elementWidth - margin;
                y = canvasHeight - elementHeight - margin;
                break;
        }

        return new SKPoint(x, y);
    }
}