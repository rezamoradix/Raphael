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
                [AsParameters] RequestQueries queries,
                IImageLoaderService loader,
                IEffectProcessor effectProcessor,
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
                    // Determine format and quality
                    var format = queries.Format ?? Models.ImageFormat.Jpeg;
                    var quality = queries.Quality ?? options.Value.Processing.DefaultQuality;

                    // Load image with caching
                    var imageData = await loader.LoadCachedAsync(url);
                    using var bitmap = SKBitmap.Decode(imageData);

                    if (bitmap == null)
                        return Results.BadRequest("Unable to decode image");

                    // Apply effects automatically based on query parameters
                    var processedBitmap = await effectProcessor.ApplyEffectsAsync(bitmap, queries);

                    // Encode result
                    var encoded = processedBitmap.Encode(format, quality);
                    var contentType = format.GetContentType();

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
}