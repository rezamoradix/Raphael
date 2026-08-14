using Raphael.Models;
using SkiaSharp;

namespace Raphael.Effects
{
    public interface IEffect
    {
        void Apply(SKBitmap bitmap);
    }
}
