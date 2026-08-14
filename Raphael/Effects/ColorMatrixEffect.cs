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


        public async ValueTask Apply(SKBitmap bitmap)
        {
            ArgumentNullException.ThrowIfNull(bitmap, nameof(bitmap));

            await Task.Run(() =>
            {
                using var filter = SKColorFilter.CreateColorMatrix(Matrix);
                using var paint = new SKPaint { ColorFilter = filter };

                var info = new SKImageInfo(bitmap.Width, bitmap.Height);
                using var result = new SKBitmap(info);
                using var canvas = new SKCanvas(result);
                canvas.DrawBitmap(bitmap, 0, 0, paint);

                using var pixmap = result.PeekPixels();
                bitmap.SetPixels(result.GetPixels());
            });
        }
    }
}
