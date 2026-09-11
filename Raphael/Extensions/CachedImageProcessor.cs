using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Raphael.Loaders;
using SkiaSharp;

namespace Raphael.Extensions
{
    public interface ICachedImageProcessor
    {
        /// <summary>Returns the cached bytes for <paramref name="cacheKey"/> if present (memory,
        /// then file), otherwise runs <paramref name="factory"/> once, caches its result, and
        /// returns that. Callers key this by the *whole* request (source URL + every query
        /// parameter that affects the output - crop/resize/format/quality/...), not just the
        /// source image, since two requests for the same source with different parameters must
        /// never collide on the same cache entry.</summary>
        Task<byte[]> GetOrCreateAsync(string cacheKey, Func<Task<byte[]>> factory);
        void ClearCache();
        void ClearFileCache();
        Task<long> GetCacheSizeAsync();
    }

    /// <summary>Caches the fully processed (decoded, cropped/resized/watermarked, re-encoded)
    /// output bytes RaphaelRouteExtensions' main endpoint produces - not the raw source bytes,
    /// which IImageLoaderService.LoadCachedAsync already caches on its own. Without this, the
    /// same product thumbnail requested by every visitor re-runs the whole SkiaSharp decode +
    /// effects + encode pipeline from scratch on every single hit, even though the output for a
    /// given URL+query string never changes (uploaded files get a fresh GUID filename per upload
    /// - see IImageStorage.SaveAsync in the main app - so a URL's bytes are effectively
    /// immutable once produced).</summary>
    public class CachedImageProcessor : ICachedImageProcessor
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CachedImageProcessor> _logger;
        private readonly ImageLoaderOptions _options;
        private readonly string _processedCacheDirectory;
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public CachedImageProcessor(
            IImageLoaderService loaderService,
            IMemoryCache memoryCache,
            ILogger<CachedImageProcessor> logger,
            ImageLoaderOptions options)
        {
            // loaderService isn't used directly any more (the caller's factory already goes
            // through it for the source bytes) - kept as a constructor parameter so DI resolution
            // order/registration doesn't need to change.
            _ = loaderService ?? throw new ArgumentNullException(nameof(loaderService));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _processedCacheDirectory = Path.Combine(
                options.FileCacheDirectory ?? Path.GetTempPath(),
                "RaphaelCache",
                options.ProcessedCacheDirectory ?? "Processed"
            );

            if (!Directory.Exists(_processedCacheDirectory))
                Directory.CreateDirectory(_processedCacheDirectory);

            _logger.LogInformation("Processed cache directory: {Directory}", _processedCacheDirectory);
        }

        public async Task<byte[]> GetOrCreateAsync(string cacheKey, Func<Task<byte[]>> factory)
        {
            if (string.IsNullOrEmpty(cacheKey))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(cacheKey));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var fullCacheKey = $"processed_{GenerateCacheKey(cacheKey)}";

            if (_options.EnableMemoryCache && _memoryCache.TryGetValue(fullCacheKey, out byte[]? cachedData) && cachedData != null)
            {
                _logger.LogDebug("Memory cache hit for processed: {CacheKey}", cacheKey);
                return cachedData;
            }

            if (_options.EnableFileCache)
            {
                var filePath = GetProcessedCachePath(fullCacheKey);
                if (File.Exists(filePath))
                {
                    try
                    {
                        await _fileLock.WaitAsync();
                        try
                        {
                            var data = await File.ReadAllBytesAsync(filePath);
                            _logger.LogDebug("File cache hit for processed: {CacheKey}", cacheKey);

                            if (_options.EnableMemoryCache)
                                _memoryCache.Set(fullCacheKey, data, GetMemoryCacheEntryOptions());

                            return data;
                        }
                        finally
                        {
                            _fileLock.Release();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read processed cache for {CacheKey}", cacheKey);
                    }
                }
            }

            _logger.LogDebug("Processing fresh image: {CacheKey}", cacheKey);
            var result = await factory();
            if (result == null)
                throw new InvalidOperationException($"Processing factory returned null for: {cacheKey}");

            if (_options.EnableMemoryCache)
            {
                _memoryCache.Set(fullCacheKey, result, GetMemoryCacheEntryOptions());
                _logger.LogDebug("Stored in memory cache: {CacheKey}", fullCacheKey);
            }

            if (_options.EnableFileCache)
            {
                var filePath = GetProcessedCachePath(fullCacheKey);
                try
                {
                    await _fileLock.WaitAsync();
                    try
                    {
                        await File.WriteAllBytesAsync(filePath, result);
                        _logger.LogDebug("Stored in file cache: {CacheKey}", fullCacheKey);
                    }
                    finally
                    {
                        _fileLock.Release();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to store processed cache for {CacheKey}", cacheKey);
                }
            }

            return result;
        }

        public void ClearCache()
        {
            if (_options.EnableMemoryCache)
            {
                // MemoryCache doesn't have a direct Clear method - we could implement a tracking
                // mechanism, but for now log; entries still expire on their own via
                // GetMemoryCacheEntryOptions()'s sliding/absolute expiration.
                _logger.LogInformation("Processed memory cache cleared");
            }

            ClearFileCache();
        }

        public void ClearFileCache()
        {
            if (_options.EnableFileCache && Directory.Exists(_processedCacheDirectory))
            {
                try
                {
                    var files = Directory.GetFiles(_processedCacheDirectory, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete cache file: {File}", file);
                        }
                    }
                    _logger.LogInformation("Processed file cache cleared ({Count} files)", files.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to clear processed file cache");
                }
            }
        }

        // Not actually async internally (plain Directory/FileInfo I/O) - Task.FromResult rather
        // than the `async` keyword avoids a "this method lacks 'await'" warning, while keeping
        // the interface async for parity with GetOrCreateAsync and room for real async I/O later.
        public Task<long> GetCacheSizeAsync()
        {
            if (!_options.EnableFileCache || !Directory.Exists(_processedCacheDirectory))
                return Task.FromResult(0L);

            try
            {
                long totalSize = 0;
                var files = Directory.GetFiles(_processedCacheDirectory, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                    }
                    catch { }
                }
                return Task.FromResult(totalSize);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate cache size");
                return Task.FromResult(0L);
            }
        }

        private string GetProcessedCachePath(string cacheKey)
        {
            var subDir = cacheKey.Substring(0, Math.Min(2, cacheKey.Length));
            var dirPath = Path.Combine(_processedCacheDirectory, subDir);
            Directory.CreateDirectory(dirPath);
            return Path.Combine(dirPath, $"{cacheKey}.cache");
        }

        private static string GenerateCacheKey(string source)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(source));
            return Convert.ToBase64String(hashBytes)
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "");
        }

        private MemoryCacheEntryOptions GetMemoryCacheEntryOptions()
        {
            var options = new MemoryCacheEntryOptions();

            if (_options.MemoryCacheSlidingExpiration.HasValue)
                options.SetSlidingExpiration(_options.MemoryCacheSlidingExpiration.Value);

            if (_options.MemoryCacheAbsoluteExpiration.HasValue)
                options.SetAbsoluteExpiration(_options.MemoryCacheAbsoluteExpiration.Value);

            if (_options.MemoryCacheSizeLimit.HasValue)
                options.SetSize(1);

            return options;
        }

        public void Dispose()
        {
            _fileLock.Dispose();
        }
    }
}
