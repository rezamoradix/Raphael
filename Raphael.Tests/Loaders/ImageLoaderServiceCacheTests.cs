using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Raphael.Loaders;

namespace Raphael.Tests.Loaders;

/// <summary>A source replaced in place (same name, new bytes) must not keep being served from the cache of the old bytes: the
/// caller's version is part of the cache key.</summary>
public sealed class ImageLoaderServiceCacheTests : IDisposable
{
    private readonly string _cache = Directory.CreateTempSubdirectory("raphael-cache-tests-").FullName;

    public void Dispose() => Directory.Delete(_cache, recursive: true);

    private sealed class CountingLoader : IImageLoader
    {
        public byte[] Bytes { get; set; } = [1];
        public int Loads { get; private set; }
        public bool CanLoad(string source) => true;
        public Task<byte[]> LoadAsync(string source) { Loads++; return Task.FromResult(Bytes); }
        public int Priority => 1;
        public string Name => "Counting";
    }

    private (ImageLoaderService Service, CountingLoader Loader) Build(bool memory, bool file)
    {
        var loader = new CountingLoader();
        var registry = new LoaderRegistry(NullLogger<LoaderRegistry>.Instance);
        registry.RegisterLoader("Counting", loader);
        var service = new ImageLoaderService(registry, memory ? new MemoryCache(new MemoryCacheOptions()) : null,
            NullLogger<ImageLoaderService>.Instance,
            new ImageLoaderOptions { EnableMemoryCache = memory, EnableFileCache = file, FileCacheDirectory = _cache, EnableFileCacheCleanup = false });
        return (service, loader);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task A_new_version_of_the_same_source_is_loaded_again_and_an_unchanged_one_is_not(bool memory, bool file)
    {
        var (service, loader) = Build(memory, file);

        Assert.Equal([1], await service.LoadCachedAsync("/uploads/a.png", "0"));
        Assert.Equal([1], await service.LoadCachedAsync("/uploads/a.png", "0"));
        Assert.Equal(1, loader.Loads); // same source, same version: from the cache

        loader.Bytes = [2];            // the file is replaced in place...
        Assert.Equal([1], await service.LoadCachedAsync("/uploads/a.png", "0")); // ...an old URL still gets the old bytes (immutable cache)
        Assert.Equal([2], await service.LoadCachedAsync("/uploads/a.png", "1")); // ...a new version gets the new ones
        Assert.Equal(2, loader.Loads);
        Assert.Equal([2], await service.LoadCachedAsync("/uploads/a.png", "1"));
        Assert.Equal(2, loader.Loads);
    }

    [Fact]
    public async Task Without_a_version_the_behaviour_is_unchanged()
    {
        var (service, loader) = Build(memory: true, file: false);
        await service.LoadCachedAsync("/uploads/a.png");
        await service.LoadCachedAsync("/uploads/a.png", null);
        await service.LoadCachedAsync("/uploads/a.png", "");
        Assert.Equal(1, loader.Loads);
    }
}
