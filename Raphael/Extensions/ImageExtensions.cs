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

        /// <summary>
        /// Decodes an image with its EXIF orientation APPLIED, so the pixels come out the way an image viewer or a
        /// browser shows the file. <c>SKBitmap.Decode</c> alone returns the raw stored pixels and ignores the
        /// orientation tag, so a phone photo (stored sideways, tagged "rotate 90") that looks upright everywhere
        /// else rendered sideways here. Everything downstream - the crop rectangle (which callers record against
        /// the upright picture) and the resize - must see the upright bitmap, so this is the one decode to use.
        /// Returns null when the data can't be decoded (same as <c>SKBitmap.Decode</c>).
        /// </summary>
        public static SKBitmap? DecodeOriented(byte[] imageData)
        {
            using var codec = SKCodec.Create(new SKMemoryStream(imageData));
            if (codec == null)
                return null;
            var decoded = SKBitmap.Decode(codec);
            if (decoded == null)
                return null;

            var origin = codec.EncodedOrigin;
            if (!TryGetOriginTransform(origin, decoded.Width, decoded.Height, out var matrix, out var width, out var height))
                return decoded; // TopLeft / unknown: already upright

            var upright = new SKBitmap(width, height, decoded.ColorType, decoded.AlphaType);
            using (var canvas = new SKCanvas(upright))
            {
                canvas.SetMatrix(matrix);
                canvas.DrawBitmap(decoded, 0, 0);
            }
            decoded.Dispose();
            return upright;
        }

        /// <summary>The transform that turns the stored pixels of an EXIF orientation (1-8) into the upright picture,
        /// with the upright size. False for orientation 1 (nothing to do).</summary>
        private static bool TryGetOriginTransform(SKEncodedOrigin origin, int w, int h, out SKMatrix matrix, out int width, out int height)
        {
            width = w;
            height = h;
            switch (origin)
            {
                case SKEncodedOrigin.TopRight:     // 2: mirrored horizontally
                    matrix = new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1);
                    return true;
                case SKEncodedOrigin.BottomRight:  // 3: rotated 180
                    matrix = new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1);
                    return true;
                case SKEncodedOrigin.BottomLeft:   // 4: mirrored vertically
                    matrix = new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1);
                    return true;
                case SKEncodedOrigin.LeftTop:      // 5: transposed
                    (width, height) = (h, w);
                    matrix = new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1);
                    return true;
                case SKEncodedOrigin.RightTop:     // 6: rotate 90 clockwise (the usual phone portrait)
                    (width, height) = (h, w);
                    matrix = new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1);
                    return true;
                case SKEncodedOrigin.RightBottom:  // 7: transverse
                    (width, height) = (h, w);
                    matrix = new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1);
                    return true;
                case SKEncodedOrigin.LeftBottom:   // 8: rotate 90 counter-clockwise
                    (width, height) = (h, w);
                    matrix = new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1);
                    return true;
                default:
                    matrix = SKMatrix.Identity;
                    return false;
            }
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
