using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Raphael.Extensions;
using Raphael.Loaders;

namespace Raphael.Tests.Extensions;

public class CachedImageProcessorTests : IDisposable
{
    private readonly string _cacheDirectory;
    private readonly CachedImageProcessor _processor;

    public CachedImageProcessorTests()
    {
        _cacheDirectory = Path.Combine(Path.GetTempPath(), "RaphaelCacheTests_" + Guid.NewGuid());
        var options = new ImageLoaderOptions
        {
            EnableMemoryCache = true,
            EnableFileCache = true,
            FileCacheDirectory = _cacheDirectory,
        };
        _processor = new CachedImageProcessor(
            Mock.Of<IImageLoaderService>(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<CachedImageProcessor>.Instance,
            options);
    }

    public void Dispose()
    {
        if (Directory.Exists(_cacheDirectory))
            Directory.Delete(_cacheDirectory, recursive: true);
    }

    [Fact]
    public async Task GetOrCreateAsync_runs_the_factory_only_once_for_the_same_key()
    {
        var calls = 0;
        Task<byte[]> Factory()
        {
            calls++;
            return Task.FromResult(new byte[] { 1, 2, 3 });
        }

        var first = await _processor.GetOrCreateAsync("img=/a.png&Width=100", Factory);
        var second = await _processor.GetOrCreateAsync("img=/a.png&Width=100", Factory);

        Assert.Equal(1, calls);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task GetOrCreateAsync_treats_different_query_strings_for_the_same_source_as_distinct_entries()
    {
        // A crop/resize/format/quality difference on the same source image must never collide on
        // one cache entry - this was the actual bug in the pre-existing (unused) processor, which
        // keyed by a closure's GetHashCode() rather than the request itself.
        var calls = 0;
        Task<byte[]> Factory()
        {
            calls++;
            return Task.FromResult(new byte[] { (byte)calls });
        }

        var width100 = await _processor.GetOrCreateAsync("img=/a.png&Width=100", Factory);
        var width200 = await _processor.GetOrCreateAsync("img=/a.png&Width=200", Factory);

        Assert.Equal(2, calls);
        Assert.NotEqual(width100, width200);
    }

    [Fact]
    public async Task GetOrCreateAsync_survives_a_fresh_instance_via_the_file_cache()
    {
        // A cold-started process (a redeploy, an app-pool recycle) shouldn't lose everything the
        // memory cache held - the file cache is the layer that survives that.
        var expected = new byte[] { 9, 8, 7 };
        await _processor.GetOrCreateAsync("img=/a.png&Width=100", () => Task.FromResult(expected));

        var freshProcessor = new CachedImageProcessor(
            Mock.Of<IImageLoaderService>(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<CachedImageProcessor>.Instance,
            new ImageLoaderOptions { EnableMemoryCache = true, EnableFileCache = true, FileCacheDirectory = _cacheDirectory });

        var neverCalled = true;
        var result = await freshProcessor.GetOrCreateAsync("img=/a.png&Width=100", () =>
        {
            neverCalled = false;
            return Task.FromResult(Array.Empty<byte>());
        });

        Assert.True(neverCalled);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ClearFileCache_removes_previously_cached_entries()
    {
        await _processor.GetOrCreateAsync("img=/a.png&Width=100", () => Task.FromResult(new byte[] { 1 }));
        Assert.True(await _processor.GetCacheSizeAsync() > 0);

        _processor.ClearFileCache();

        Assert.Equal(0, await _processor.GetCacheSizeAsync());
    }
}
