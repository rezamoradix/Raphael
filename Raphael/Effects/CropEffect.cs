using Raphael.Attributes;
using SkiaSharp;

namespace Raphael.Effects;

public enum CropAnchor
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

[Effect("Crop", Description = "Crop image to specified area", Order = 2, Category = "Transform")]
public class CropEffect : IEffect
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public CropAnchor Anchor { get; set; } = CropAnchor.TopLeft;
    public bool UseAnchor { get; set; } = false;

    public void Apply(SKBitmap bitmap)
    {
        if (Width <= 0 || Height <= 0)
            return;

        int cropX = X;
        int cropY = Y;

        if (UseAnchor)
        {
            // Calculate position based on anchor
            switch (Anchor)
            {
                case CropAnchor.TopLeft:
                    cropX = 0;
                    cropY = 0;
                    break;
                case CropAnchor.TopCenter:
                    cropX = (bitmap.Width - Width) / 2;
                    cropY = 0;
                    break;
                case CropAnchor.TopRight:
                    cropX = bitmap.Width - Width;
                    cropY = 0;
                    break;
                case CropAnchor.CenterLeft:
                    cropX = 0;
                    cropY = (bitmap.Height - Height) / 2;
                    break;
                case CropAnchor.Center:
                    cropX = (bitmap.Width - Width) / 2;
                    cropY = (bitmap.Height - Height) / 2;
                    break;
                case CropAnchor.CenterRight:
                    cropX = bitmap.Width - Width;
                    cropY = (bitmap.Height - Height) / 2;
                    break;
                case CropAnchor.BottomLeft:
                    cropX = 0;
                    cropY = bitmap.Height - Height;
                    break;
                case CropAnchor.BottomCenter:
                    cropX = (bitmap.Width - Width) / 2;
                    cropY = bitmap.Height - Height;
                    break;
                case CropAnchor.BottomRight:
                    cropX = bitmap.Width - Width;
                    cropY = bitmap.Height - Height;
                    break;
            }
        }

        cropX = Math.Max(0, cropX);
        cropY = Math.Max(0, cropY);
        int cropWidth = Math.Min(Width, bitmap.Width - cropX);
        int cropHeight = Math.Min(Height, bitmap.Height - cropY);

        if (cropWidth <= 0 || cropHeight <= 0)
            return;

        var cropRect = new SKRectI(cropX, cropY, cropX + cropWidth, cropY + cropHeight);
        var cropped = new SKBitmap();

        if (bitmap.ExtractSubset(cropped, cropRect))
        {
            cropped.CopyTo(bitmap);
        }
    }
}