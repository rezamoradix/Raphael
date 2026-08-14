using SkiaSharp;

namespace Raphael.Models
{
    public class RenderResult(SKImage SKImage)
    {
        SKImage SkImage { get; set; } = SKImage;

        public byte[] ToJpegArray(int quality = 75)
        {
            using var enocded = SkImage.Encode(SKEncodedImageFormat.Jpeg, quality);
            return enocded.ToArray();
        }

        public byte[] ToPngArray(int quality = 75)
        {
            using var enocded = SkImage.Encode(SKEncodedImageFormat.Png, quality);
            return enocded.ToArray();
        }

        public byte[] ToWebpArray(int quality = 75)
        {
            using var enocded = SkImage.Encode(SKEncodedImageFormat.Webp, quality);
            return enocded.ToArray();
        }
    }
}
