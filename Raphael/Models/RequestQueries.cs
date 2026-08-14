using Microsoft.AspNetCore.Mvc;
using SkiaSharp;

namespace Raphael.Models
{
    public class RequestQueries
    {
        [FromQuery]
        public QueryImage? Img { get; set; }

        // Resize parameters
        [FromQuery]
        public int? Width { get; set; }

        [FromQuery]
        public int? Height { get; set; }

        [FromQuery]
        public bool? PreserveAspectRatio { get; set; }

        // Crop parameters
        [FromQuery]
        public int? CropX { get; set; }

        [FromQuery]
        public int? CropY { get; set; }

        [FromQuery]
        public int? CropWidth { get; set; }

        [FromQuery]
        public int? CropHeight { get; set; }

        [FromQuery]
        public bool? UseAnchor { get; set; }

        [FromQuery]
        public CropAnchor? Anchor { get; set; }

        // Watermark parameters
        [FromQuery]
        public string? WatermarkText { get; set; }

        [FromQuery]
        public string? WatermarkImageUrl { get; set; }

        [FromQuery]
        public WatermarkPosition? WatermarkPosition { get; set; }

        [FromQuery]
        public float? WatermarkOpacity { get; set; }

        [FromQuery]
        public float? WatermarkFontSize { get; set; }

        [FromQuery]
        public bool? WatermarkShadow { get; set; }

        [FromQuery]
        public float? WatermarkScale { get; set; }

        [FromQuery]
        public bool? WatermarkPreserveRatio { get; set; }

        // Output parameters
        [FromQuery]
        public global::Raphael.Models.ImageFormat? Format { get; set; }

        [FromQuery]
        public int? Quality { get; set; }

        [FromQuery]
        public bool? BypassCache { get; set; }
    }

    public class QueryImage
    {
        public string[] Values { get; set; } = [];

        public static implicit operator QueryImage(string value) => new() { Values = [value] };
        public static implicit operator QueryImage(string[] values) => new() { Values = values };

        public static bool TryParse(string? value, out QueryImage? result)
        {
            if (string.IsNullOrEmpty(value))
            {
                result = null;
                return false;
            }

            result = new QueryImage { Values = [value] };
            return true;
        }

        public static bool TryParse(string[]? values, out QueryImage? result)
        {
            if (values == null || values.Length == 0)
            {
                result = null;
                return false;
            }

            result = new QueryImage { Values = values };
            return true;
        }
    }

    public enum ImageFormat
    {
        Jpeg,
        Png,
        WebP,
        Gif,
        Bmp,
        Tiff,
        Ico,
        Avif
    }

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
}