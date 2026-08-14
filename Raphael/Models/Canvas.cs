using Raphael.Effects;
using SkiaSharp;

namespace Raphael.Models
{
    public sealed class Canvas(SKImage Image) : IDisposable
    {
        public SKImage Image { get; } = Image;

        List<IEffect> _effects { get; } = [];

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Canvas Use<TEffect>(Action<TEffect> configure) where TEffect : IEffect, new()
        {
            var effect = new TEffect();
            configure(effect);
            _effects.Add(effect);
            return this;
        }

        public async Task<RenderResult> RenderAsync()
        {
            using var bitmap = SKBitmap.FromImage(Image);

            foreach (var effect in _effects)
            {
                await effect.Apply(bitmap);
            }

            return new RenderResult(SKImage.FromBitmap(bitmap));
        }
    }
}
