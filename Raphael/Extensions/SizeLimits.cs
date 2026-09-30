using Raphael.Models;

namespace Raphael.Extensions
{
    public static class SizeLimits
    {
        /// <summary>
        /// For requests with <c>Upscale=false</c>: drops the requested Width/Height when honouring them would ENLARGE the
        /// picture (measured on what is left after the crop, since the crop runs before the resize), so a small source
        /// is served at its own size instead of a blown-up, bigger, no-sharper file. Resize fits the picture inside
        /// Width x Height keeping its aspect ratio, so the scale is min(Width/w, Height/h) - enlarging when that is over 1.
        /// </summary>
        public static void LimitToSource(this RequestQueries queries, int sourceWidth, int sourceHeight)
        {
            if (queries.Upscale != false)
                return;

            var width = sourceWidth;
            var height = sourceHeight;
            if (queries.CropWidth is > 0 && queries.CropHeight is > 0)
            {
                width = Math.Min(queries.CropWidth.Value, Math.Max(1, sourceWidth - Math.Max(0, queries.CropX ?? 0)));
                height = Math.Min(queries.CropHeight.Value, Math.Max(1, sourceHeight - Math.Max(0, queries.CropY ?? 0)));
            }

            var requestedWidth = queries.Width is > 0 ? queries.Width : null;
            var requestedHeight = queries.Height is > 0 ? queries.Height : null;
            double scale;
            if (requestedWidth != null && requestedHeight != null)
                scale = Math.Min((double)requestedWidth.Value / width, (double)requestedHeight.Value / height);
            else if (requestedWidth != null)
                scale = (double)requestedWidth.Value / width;
            else if (requestedHeight != null)
                scale = (double)requestedHeight.Value / height;
            else
                return;

            if (scale > 1)
            {
                queries.Width = null;
                queries.Height = null;
            }
        }
    }
}
