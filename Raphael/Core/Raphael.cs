using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Raphael.Attributes;
using Raphael.Effects;
using Raphael.Models;
using SkiaSharp;
using System.Diagnostics;
using System.Reflection;

namespace Raphael.Core
{
    public class Raphael
    {
        public static async Task<Canvas> LoadImage(string url)
        {
            // url might be local file
            return new(SKImage.FromEncodedData(await Http.FetchBytesAsync(url)));
        }

        public static Canvas LoadImageFromBytes(byte[] imageBytes)
        {
            return new(SKImage.FromEncodedData(imageBytes));
        }
    }
}
