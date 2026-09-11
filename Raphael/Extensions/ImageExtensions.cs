using Raphael.Models;
using SkiaSharp;

namespace Raphael.Extensions
{
    public static class ImageExtensions
    {
        /// <summary>Sniffs the source bytes' own encoded format (via SKCodec, header-only - no
        /// full decode) so a request with no explicit "format" query param re-encodes to the same
        /// family the source already was, rather than always falling back to Jpeg. This matters
        /// most for Png: Jpeg has no alpha channel, so defaulting a transparent Png to Jpeg used
        /// to silently flatten its transparency onto an opaque background. Anything SkiaSharp
        /// can't map onto one of Raphael's own output formats (or can't identify at all) falls
        /// back to Jpeg, same as before.</summary>
        public static Models.ImageFormat DetectFormat(this byte[] imageData)
        {
            using var codec = SKCodec.Create(new SKMemoryStream(imageData));
            return codec?.EncodedFormat switch
            {
                SKEncodedImageFormat.Png => Models.ImageFormat.Png,
                SKEncodedImageFormat.Jpeg => Models.ImageFormat.Jpeg,
                SKEncodedImageFormat.Webp => Models.ImageFormat.WebP,
                SKEncodedImageFormat.Gif => Models.ImageFormat.Gif,
                SKEncodedImageFormat.Bmp => Models.ImageFormat.Bmp,
                SKEncodedImageFormat.Ico => Models.ImageFormat.Ico,
                SKEncodedImageFormat.Avif => Models.ImageFormat.Avif,
                _ => Models.ImageFormat.Jpeg
            };
        }

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
