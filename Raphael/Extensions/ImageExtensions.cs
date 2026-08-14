using Raphael.Models;
using SkiaSharp;

namespace Raphael.Extensions
{
    public static class ImageExtensions
    {
        public static byte[] Encode(this SKBitmap bitmap, Models.ImageFormat format, int quality = 75)
        {
            var skFormat = format switch
            {
                Models.ImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
                Models.ImageFormat.Png => SKEncodedImageFormat.Png,
                Models.ImageFormat.WebP => SKEncodedImageFormat.Webp,
                Models.ImageFormat.Gif => SKEncodedImageFormat.Gif,
                Models.ImageFormat.Bmp => SKEncodedImageFormat.Bmp,
                Models.ImageFormat.Ico => SKEncodedImageFormat.Ico,
                Models.ImageFormat.Tiff => SKEncodedImageFormat.Jpeg,
                Models.ImageFormat.Avif => SKEncodedImageFormat.Avif,
                _ => SKEncodedImageFormat.Jpeg
            };

            return bitmap.Encode(skFormat, quality).ToArray();
        }

        public static string GetContentType(this Models.ImageFormat format)
        {
            return format switch
            {
                Models.ImageFormat.Jpeg => "image/jpeg",
                Models.ImageFormat.Png => "image/png",
                Models.ImageFormat.WebP => "image/webp",
                Models.ImageFormat.Gif => "image/gif",
                Models.ImageFormat.Bmp => "image/bmp",
                Models.ImageFormat.Tiff => "image/tiff",
                Models.ImageFormat.Ico => "image/x-icon",
                Models.ImageFormat.Avif => "image/avif",
                _ => "application/octet-stream"
            };
        }
    }
}
