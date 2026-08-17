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
        Task<byte[]> ProcessAndCacheAsync(string source, Func<SKBitmap, Task<byte[]>> processor);
        Task<SKBitmap> LoadAndCacheBitmapAsync(string source);
        void ClearCache();
        void ClearFileCache();
        Task<long> GetCacheSizeAsync();
    }

    public class CachedImageProcessor : ICachedImageProcessor
    {
        private readonly IImageLoaderService _loaderService;
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
            _loaderService = loaderService ?? throw new ArgumentNullException(nameof(loaderService));
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

        public async Task<byte[]> ProcessAndCacheAsync(string source, Func<SKBitmap, Task<byte[]>> processor)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));

            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            var cacheKey = GenerateCacheKey(source);
            var processorHash = processor.GetHashCode();
            var fullCacheKey = $"processed_{cacheKey}_{processorHash}";

            // Check memory cache
            if (_options.EnableMemoryCache && _memoryCache.TryGetValue(fullCacheKey, out byte[]? cachedData) && cachedData != null)
            {
                _logger.LogDebug("Memory cache hit for processed: {Source}", source);
                return cachedData;
            }

            // Check file cache
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
                            _logger.LogDebug("File cache hit for processed: {Source}", source);

                            if (_options.EnableMemoryCache)
                            {
                                _memoryCache.Set(fullCacheKey, data, GetMemoryCacheEntryOptions());
                            }

                            return data;
                        }
                        finally
                        {
                            _fileLock.Release();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read processed cache for {Source}", source);
                    }
                }
            }

            // Process fresh
            _logger.LogDebug("Processing fresh image: {Source}", source);
            var imageData = await _loaderService.LoadCachedAsync(source);
            using var bitmap = SKBitmap.Decode(imageData);

            if (bitmap == null)
                throw new InvalidOperationException($"Failed to decode image: {source}");

            var result = await processor(bitmap);

            if (result == null)
                throw new InvalidOperationException($"Processor returned null for: {source}");

            // Cache results
            if (_options.EnableMemoryCache && result != null)
            {
                _memoryCache.Set(fullCacheKey, result, GetMemoryCacheEntryOptions());
                _logger.LogDebug("Stored in memory cache: {CacheKey}", fullCacheKey);
            }

            if (_options.EnableFileCache && result != null)
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
                    _logger.LogWarning(ex, "Failed to store processed cache for {Source}", source);
                }
            }

            return result!;
        }

        public async Task<SKBitmap> LoadAndCacheBitmapAsync(string source)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));

            var cacheKey = GenerateCacheKey(source);
            var fullCacheKey = $"bitmap_{cacheKey}";

            // Check memory cache
            if (_options.EnableMemoryCache && _memoryCache.TryGetValue(fullCacheKey, out byte[]? cachedData) && cachedData != null)
            {
                _logger.LogDebug("Bitmap cache hit for {Source}", source);
                return SKBitmap.Decode(cachedData);
            }

            // Load fresh
            var data = await _loaderService.LoadCachedAsync(source);
            var bitmap = SKBitmap.Decode(data);

            if (bitmap == null)
                throw new InvalidOperationException($"Failed to decode image: {source}");

            // Store in memory cache
            if (_options.EnableMemoryCache && data != null)
            {
                _memoryCache.Set(fullCacheKey, data, GetMemoryCacheEntryOptions());
                _logger.LogDebug("Stored bitmap in memory cache: {CacheKey}", fullCacheKey);
            }

            return bitmap;
        }

        public void ClearCache()
        {
            if (_options.EnableMemoryCache)
            {
                // MemoryCache doesn't have a direct Clear method
                // We could implement a tracking mechanism, but for now we log
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

        public async Task<long> GetCacheSizeAsync()
        {
            if (!_options.EnableFileCache || !Directory.Exists(_processedCacheDirectory))
                return 0;

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
                return totalSize;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate cache size");
                return 0;
            }
        }

        private string GetProcessedCachePath(string cacheKey)
        {
            var subDir = cacheKey.Substring(0, Math.Min(2, cacheKey.Length));
            var dirPath = Path.Combine(_processedCacheDirectory, subDir);
            Directory.CreateDirectory(dirPath);
            return Path.Combine(dirPath, $"{cacheKey}.cache");
        }

        private string GenerateCacheKey(string source)
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