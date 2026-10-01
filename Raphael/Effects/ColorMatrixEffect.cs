using Raphael.Attributes;
using SkiaSharp;

namespace Raphael.Effects
{
    [Effect("ColorMatrix", Description = "Apply color transformation using a color matrix", Category = "Color")]
    public class ColorMatrixEffect : IEffect
    {
        public required float[] Matrix { get; set; }
        public float Brightness { get; set; } = 1.0f;
        public float Contrast { get; set; } = 1.0f;
        public float Saturation { get; set; } = 1.0f;
        public float HueDegrees { get; set; } = 1.0f;
        public float Intensity { get; set; } = 1.0f;
        public bool PreserveAlpha { get; set; } = true;


        public void Apply(SKBitmap bitmap)
        {
            ArgumentNullException.ThrowIfNull(bitmap, nameof(bitmap));

            using var filter = SKColorFilter.CreateColorMatrix(Matrix);
            using var paint = new SKPaint { ColorFilter = filter };

            // Match the source's color type; a default SKImageInfo is Bgra8888 on most platforms,
            // which would swap red/blue when copied back into an Rgba8888 bitmap.
            using var result = new SKBitmap(bitmap.Info);
            using var canvas = new SKCanvas(result);
            canvas.DrawBitmap(bitmap, 0, 0, paint);

            // Copy the pixels: SetPixels would only point at `result`'s buffer, which is freed on dispose.
            result.GetPixelSpan().CopyTo(bitmap.GetPixelSpan());
        }
    }
}
