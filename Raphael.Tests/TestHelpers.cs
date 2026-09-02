using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Raphael.Tests
{
    internal static class TestHelpers
    {
        public static SKBitmap CreateTestBitmap(int width, int height)
        {
            var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Red);
            return bitmap;
        }
    }
}
