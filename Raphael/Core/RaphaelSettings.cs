namespace Raphael.Core
{
    public class RaphaelSettings
    {
        public int? MaxAllowedWidth { get; set; }
        public int? MaxAllowedHeight { get; set; }
        public int DefaultImageQuality { get; set; } = 80;
        public ImageFormat? ForceOuputFormat { get; set; }

        public Dictionary<string, SourceValue> Sources { get; set; } = [];
    }

    public enum ImageFormat
    {
        Jpeg, Png, WebP,
        Bmp,
        Gif,
        Tiff,
        Ico,
        Avif,
    }

    public class SourceValue()
    {
        public string[] Values { get; set; } = null!;

        public static implicit operator SourceValue(string value) => new() { Values = [value] };
        public static implicit operator SourceValue(string[] values) => new() { Values = values };
    }
}
