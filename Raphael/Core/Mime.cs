namespace Raphael.Core
{
    public static class Mime
    {
        public static ImageFormat Detect(string uri)
        {
            if (uri.Contains(".png", StringComparison.InvariantCultureIgnoreCase)) return ImageFormat.Png;
            if (uri.Contains(".webp", StringComparison.InvariantCultureIgnoreCase)) return ImageFormat.WebP;

            return ImageFormat.Jpeg;
        }

        public static string ToContentType(this ImageFormat format)
        {
            return format switch
            {
                ImageFormat.Jpeg => "image/jpeg",
                ImageFormat.Png => "image/png",
                ImageFormat.WebP => "image/webp",

                _ => throw new NotImplementedException(),
            };
        }
    }
}
