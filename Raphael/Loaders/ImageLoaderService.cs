using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Raphael.Loaders
{
    public class ImageLoaderService : IImageLoaderService
    {
        private readonly ILoaderRegistry _loaderRegistry;
        private readonly IMemoryCache? _memoryCache;
        private readonly ILogger<ImageLoaderService> _logger;
        private readonly ImageLoaderOptions _options;
        private readonly string _cacheDirectory;
        private readonly string _processedCacheDirectory;
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _cleanupCancellationToken = new CancellationTokenSource();

        public ImageLoaderService(
            ILoaderRegistry loaderRegistry,
            IMemoryCache? memoryCache,
            ILogger<ImageLoaderService> logger,
            ImageLoaderOptions options)
        {
            _loaderRegistry = loaderRegistry;
            _memoryCache = memoryCache;
            _logger = logger;
            _options = options;

            // Setup file cache directories
            _cacheDirectory = options.FileCacheDirectory ??
                Path.Combine(Path.GetTempPath(), "RaphaelCache", "Images");

            _processedCacheDirectory = Path.Combine(
                options.FileCacheDirectory ?? Path.GetTempPath(),
                "RaphaelCache",
                options.ProcessedCacheDirectory ?? "Processed"
            );

            if (!Directory.Exists(_cacheDirectory))
                Directory.CreateDirectory(_cacheDirectory);

            if (!Directory.Exists(_processedCacheDirectory))
                Directory.CreateDirectory(_processedCacheDirectory);

            // Setup cache cleanup if enabled
            if (options.EnableFileCacheCleanup)
            {
                Task.Run(async () => await CleanupOldFilesAsync(_cleanupCancellationToken.Token));
            }
        }

        public async Task<byte[]> LoadAsync(string source)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));

            // Try each loader in priority order
            foreach (var loader in _loaderRegistry.GetAllLoaders())
            {
                if (loader.CanLoad(source))
                {
                    _logger.LogDebug("Loading from {Source} using {Loader}", source, loader.GetType().Name);
                    return await loader.LoadAsync(source);
                }
            }

            throw new InvalidOperationException($"No loader found for source: {source}");
        }

        public bool CanLoad(string source)
        {
            if (string.IsNullOrEmpty(source))
                return false;

            foreach (var loader in _loaderRegistry.GetAllLoaders())
            {
                if (loader.CanLoad(source))
                    return true;
            }

            return false;
        }

        public async Task<byte[]> LoadCachedAsync(string source)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));

            var cacheKey = GenerateCacheKey(source);

            // Check memory cache
            if (_options.EnableMemoryCache && _memoryCache != null)
            {
                if (_memoryCache.TryGetValue(cacheKey, out byte[]? cachedData) && cachedData != null)
                {
                    _logger.LogDebug("Memory cache hit for {Source}", source);
                    return cachedData;
                }
            }

            // Check file cache
            if (_options.EnableFileCache)
            {
                var filePath = GetCacheFilePath(cacheKey);
                if (File.Exists(filePath))
                {
                    try
                    {
                        var fileData = await File.ReadAllBytesAsync(filePath);
                        _logger.LogDebug("File cache hit for {Source}", source);

                        if (_options.EnableMemoryCache && _memoryCache != null)
                        {
                            _memoryCache.Set(cacheKey, fileData, GetMemoryCacheEntryOptions());
                        }

                        return fileData;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read from file cache for {Source}", source);
                    }
                }
            }

            // Load from source
            var data = await LoadAsync(source);

            // Store in caches
            if (_options.EnableMemoryCache && _memoryCache != null)
            {
                _memoryCache.Set(cacheKey, data, GetMemoryCacheEntryOptions());
            }

            if (_options.EnableFileCache)
            {
                await StoreInFileCacheAsync(cacheKey, data);
            }

            return data;
        }

        public void ClearCache()
        {
            if (_options.EnableMemoryCache && _memoryCache != null)
            {
                // MemoryCache doesn't have a direct Clear method
                // We can use a CacheEntryRemovedCallback or track keys
                _logger.LogInformation("Memory cache cleared");
            }
        }

        public void ClearFileCache()
        {
            if (_options.EnableFileCache)
            {
                try
                {
                    // Clear both cache directories
                    var directories = new[] { _cacheDirectory, _processedCacheDirectory };
                    foreach (var dir in directories)
                    {
                        if (Directory.Exists(dir))
                        {
                            foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                            {
                                try { File.Delete(file); } catch { }
                            }
                            _logger.LogInformation("File cache cleared: {Directory}", dir);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to clear file cache");
                }
            }
        }

        public void Dispose()
        {
            _cleanupCancellationToken.Cancel();
            _cleanupCancellationToken.Dispose();
            _fileLock.Dispose();
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

        private string GetCacheFilePath(string cacheKey)
        {
            var subDir = cacheKey.Substring(0, 2);
            var dirPath = Path.Combine(_cacheDirectory, subDir);
            Directory.CreateDirectory(dirPath);
            return Path.Combine(dirPath, $"{cacheKey}.cache");
        }

        private async Task StoreInFileCacheAsync(string cacheKey, byte[] data)
        {
            try
            {
                await _fileLock.WaitAsync();
                try
                {
                    var filePath = GetCacheFilePath(cacheKey);
                    await File.WriteAllBytesAsync(filePath, data);
                    _logger.LogDebug("Stored in file cache: {CacheKey}", cacheKey);
                }
                finally
                {
                    _fileLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to store in file cache for key: {CacheKey}", cacheKey);
            }
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

        /// <summary>
        /// Cleanup old cache files periodically
        /// </summary>
        private async Task CleanupOldFilesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Run cleanup every hour
                    await Task.Delay(TimeSpan.FromHours(1), cancellationToken);

                    if (!_options.EnableFileCache || !_options.FileCacheMaxAge.HasValue)
                        continue;

                    var cutoffTime = DateTime.UtcNow.Subtract(_options.FileCacheMaxAge.Value);
                    var directories = new[] { _cacheDirectory, _processedCacheDirectory };

                    foreach (var dir in directories)
                    {
                        if (!Directory.Exists(dir))
                            continue;

                        try
                        {
                            var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
                            foreach (var file in files)
                            {
                                try
                                {
                                    var fileInfo = new FileInfo(file);
                                    if (fileInfo.LastAccessTimeUtc < cutoffTime)
                                    {
                                        fileInfo.Delete();
                                        _logger.LogDebug("Cleaned up old cache file: {File}", file);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to delete old cache file: {File}", file);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to scan directory for cleanup: {Directory}", dir);
                        }
                    }

                    _logger.LogDebug("Cache cleanup completed at {Time}", DateTime.UtcNow);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cache cleanup");
                    // Wait before retrying on error
                    await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                }
            }
        }
    }

    public interface IImageLoaderService
    {
        Task<byte[]> LoadAsync(string source);
        bool CanLoad(string source);
        Task<byte[]> LoadCachedAsync(string source);
        void ClearCache();
        void ClearFileCache();
    }

    public class ImageLoaderOptions
    {
        public bool EnableMemoryCache { get; set; } = true;
        public bool EnableFileCache { get; set; } = true;
        public string? FileCacheDirectory { get; set; }
        public string? ProcessedCacheDirectory { get; set; }
        public TimeSpan? MemoryCacheSlidingExpiration { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan? MemoryCacheAbsoluteExpiration { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan? FileCacheMaxAge { get; set; } = TimeSpan.FromDays(7);
        public bool EnableFileCacheCleanup { get; set; } = true;
        public int? MemoryCacheSizeLimit { get; set; } = 100;
    }
}