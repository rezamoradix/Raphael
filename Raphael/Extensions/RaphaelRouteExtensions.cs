using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raphael.Configuration;
using Raphael.Effects;
using Raphael.Loaders;
using Raphael.Models;
using SkiaSharp;
using System.Reflection;

namespace Raphael.Extensions
{
    public static class RaphaelRouteExtensions
    {
        public static void AddRaphaelRoutes(this WebApplication app)
        {
            var g = app.MapGroup("_raphael");

            // Main image processing endpoint
            g.MapGet("/", async (
                HttpContext httpContext,
                [AsParameters] RequestQueries queries,
                IImageLoaderService loader,
                IEffectProcessor effectProcessor,
                ICachedImageProcessor cachedProcessor,
                IOptions<RaphaelConfig> options,
                [FromServices] ILogger<RaphaelConfig> logger) =>
            {
                if (queries.Img == null)
                    return Results.BadRequest("Image URL is required");

                var url = queries.Img.Values[0];
                if (string.IsNullOrEmpty(url))
                    return Results.BadRequest("Invalid image URL");

                try
                {
                    // loader.LoadCachedAsync is itself cached (raw source bytes, keyed by url) so
                    // this is cheap even on a processed-cache hit below - loading it unconditionally
                    // (rather than only inside the cache-miss factory) lets an explicit "format"
                    // query param be honored, and when there isn't one, lets the *source's own*
                    // encoded format decide the output format (see DetectFormat) - both before we
                    // know whether this request is a processed-cache hit or miss, so the response's
                    // Content-Type header is always right either way.
                    // `v` is a version the caller changes whenever the file behind `img` is replaced in place: part of the source cache key.
                    var imageData = await loader.LoadCachedAsync(url, httpContext.Request.Query["v"].ToString());
                    var format = queries.Format ?? imageData.DetectFormat();
                    var quality = queries.Quality ?? options.Value.Processing.DefaultQuality;
                    queries.ResizeQuality ??= options.Value.Processing.DefaultResizeQuality;

                    // The fully processed (decoded, cropped/resized/watermarked, re-encoded)
                    // output is cached by the exact incoming query string - this is the expensive
                    // CPU work, and the overwhelmingly common case is the same URL+params (a
                    // product thumbnail, a media-library grid tile, ...) requested repeatedly by
                    // many visitors, not a one-off. Keyed on the whole query string (not just
                    // `url`) so different crop/resize/format/quality combinations of the same
                    // source never collide on one cache entry. See CachedImageProcessor's own
                    // doc comment for why this is safe to cache aggressively.
                    var cacheKey = httpContext.Request.QueryString.HasValue
                        ? httpContext.Request.QueryString.Value!
                        : url;
                    byte[] encoded;
                    try
                    {
                        encoded = await cachedProcessor.GetOrCreateAsync(cacheKey, async () =>
                        {
                            // Orientation applied (phone photos are stored sideways + an EXIF tag): the crop rectangle and the
                            // resize below are both in terms of the upright picture.
                            using var bitmap = ImageExtensions.DecodeOriented(imageData);
                            if (bitmap == null)
                                throw new ImageDecodeException("Unable to decode image");

                            queries.LimitToSource(bitmap.Width, bitmap.Height); // Upscale=false: never enlarge a small source
                            var processedBitmap = await effectProcessor.ApplyEffectsAsync(bitmap, queries);
                            return processedBitmap.Encode(format, quality);
                        });
                    }
                    catch (ImageDecodeException ex)
                    {
                        return Results.BadRequest(ex.Message);
                    }

                    var contentType = format.GetContentType();
                    // Uploaded files get a fresh GUID filename on every upload (LocalImageStorage
                    // in the main app), never reused - a given URL+query's bytes are effectively
                    // immutable once produced, so browsers/any CDN in front can cache this
                    // response for a long time rather than re-requesting it every time.
                    httpContext.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";

                    return Results.File(encoded, contentType, enableRangeProcessing: true);
                }
                catch (SecurityException ex)
                {
                    logger.LogWarning(ex, "Security violation loading image: {Url}", url);
                    return Results.Problem($"Security error: {ex.Message}", statusCode: 403);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing image: {Url}", url);
                    return Results.Problem($"Error processing image: {ex.Message}", statusCode: 500);
                }
            });

            // Health check endpoint
            g.MapGet("/health", (IOptions<RaphaelConfig> options, ILoaderRegistry registry) =>
            {
                var config = options.Value;
                var loaders = registry.GetAllLoaders().ToList();

                return Results.Ok(new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Environment = config.Environment,
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                    Loaders = new
                    {
                        Count = loaders.Count,
                        Available = loaders.Select(l => l.GetType().Name)
                    },
                    Caching = new
                    {
                        Enabled = config.Cache.EnableMemoryCache || config.Cache.EnableFileCache,
                        MemoryCache = config.Cache.EnableMemoryCache,
                        FileCache = config.Cache.EnableFileCache
                    },
                    Security = new
                    {
                        Validating = config.Security.ValidateImageBeforeProcessing,
                        RateLimit = config.Security.RateLimitPerMinute,
                        HttpsOnly = config.Security.EnableHttpsOnly
                    }
                });
            });

            // Version endpoint
            g.MapGet("/version", () =>
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                var informationalVersion = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

                return Results.Ok(new
                {
                    Version = version?.ToString() ?? "Unknown",
                    InformationalVersion = informationalVersion ?? "Unknown",
                    Framework = Environment.Version.ToString(),
                    OS = Environment.OSVersion.ToString(),
                    Process = Environment.ProcessId,
                    Memory = Environment.WorkingSet / 1024 / 1024 + " MB"
                });
            });
        }
    }

    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
        public SecurityException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>An input file that isn't a decodable image - a 400 (bad request), not a 500, even
    /// though it's thrown from inside the cached-processing factory alongside exceptions that
    /// really are unexpected server errors.</summary>
    public class ImageDecodeException : Exception
    {
        public ImageDecodeException(string message) : base(message) { }
    }
}