using Raphael.Loaders;

namespace Raphael.Tests.Loaders;

public sealed class LocalFileLoaderTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("raphael-tests-").FullName;
    private readonly byte[] _payload = [1, 2, 3, 4];

    public LocalFileLoaderTests()
    {
        File.WriteAllBytes(Path.Combine(_root, "pic.png"), _payload);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Constructor_NoDirectories_Throws()
    {
        Assert.Throws<ArgumentException>(() => new LocalFileLoader([]));
    }

    [Fact]
    public void Constructor_MissingDirectory_Throws()
    {
        var missing = Path.Combine(_root, "nope");
        Assert.Throws<DirectoryNotFoundException>(() => new LocalFileLoader([missing]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("notes.txt")]
    public void CanLoad_RejectsNonImageSources(string source)
    {
        var loader = new LocalFileLoader([_root]);
        Assert.False(loader.CanLoad(source));
    }

    [Fact]
    public void CanLoad_AcceptsImageFilename()
    {
        var loader = new LocalFileLoader([_root]);
        Assert.True(loader.CanLoad("pic.png"));
    }

    [Fact]
    public async Task LoadAsync_ByFilename_ReturnsBytes()
    {
        var loader = new LocalFileLoader([_root]);
        Assert.Equal(_payload, await loader.LoadAsync("pic.png"));
    }

    [Fact]
    public async Task LoadAsync_ByAbsolutePath_ReturnsBytes()
    {
        var loader = new LocalFileLoader([_root]);
        Assert.Equal(_payload, await loader.LoadAsync(Path.Combine(_root, "pic.png")));
    }

    [Fact]
    public async Task LoadAsync_RecursiveFindsFileInSubdirectory()
    {
        var sub = Directory.CreateDirectory(Path.Combine(_root, "sub"));
        File.WriteAllBytes(Path.Combine(sub.FullName, "deep.png"), _payload);

        var loader = new LocalFileLoader([_root], recursive: true);

        Assert.Equal(_payload, await loader.LoadAsync("deep.png"));
    }

    [Fact]
    public async Task LoadAsync_NonRecursiveDoesNotFindSubdirectoryFile()
    {
        var sub = Directory.CreateDirectory(Path.Combine(_root, "sub"));
        File.WriteAllBytes(Path.Combine(sub.FullName, "deep.png"), _payload);

        var loader = new LocalFileLoader([_root]);

        await Assert.ThrowsAsync<FileNotFoundException>(() => loader.LoadAsync("deep.png"));
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ThrowsFileNotFound()
    {
        var loader = new LocalFileLoader([_root]);
        await Assert.ThrowsAsync<FileNotFoundException>(() => loader.LoadAsync("missing.png"));
    }

    [Theory]
    [InlineData("../secret.png")]
    [InlineData("sub/../../secret.png")]
    public async Task LoadAsync_RejectsPathTraversal(string source)
    {
        var loader = new LocalFileLoader([_root]);
        // Either the traversal check or the outside-directory check must fire; nothing may be read.
        await Assert.ThrowsAnyAsync<Exception>(() => loader.LoadAsync(source));
    }

    [Fact]
    public async Task LoadAsync_EmptySource_Throws()
    {
        var loader = new LocalFileLoader([_root]);
        await Assert.ThrowsAsync<ArgumentException>(() => loader.LoadAsync(""));
    }
}
