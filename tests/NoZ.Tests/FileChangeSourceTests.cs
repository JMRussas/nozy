//  NoZ.Tests - File change source tests (Phase 6)
//
//  Tests for IFileChangeSource integration and path-to-asset mapping.

using NoZ.Tests.Helpers;
using Xunit;

namespace NoZ.Tests;

[Collection("Engine")]
public class FileChangeSourceTests : IDisposable
{
    private readonly AssetTestHelper _helper;

    public FileChangeSourceTests()
    {
        _helper = new AssetTestHelper();
        _helper.InitEngine();
    }

    public void Dispose() => _helper.Dispose();

    [Fact]
    public void FileChanged_EnqueuesReload()
    {
        // Arrange
        var source = new TestFileChangeSource();
        var watcher = new AssetWatcher();
        watcher.Subscribe(source);

        _helper.RegisterAssetData(AssetType.Texture, "hero", AssetTestHelper.CreateTextureBytes());
        Asset.Load(AssetType.Texture, "hero");
        _helper.RegisterAssetData(AssetType.Texture, "hero", AssetTestHelper.CreateTextureBytes(16, 16));

        // Act: simulate file change
        source.SimulateChange(AssetType.Texture, "hero");
        watcher.ProcessReloadQueue();

        // Assert
        var reloaded = Asset.Get<Texture>(AssetType.Texture, "hero");
        Assert.NotNull(reloaded);
    }

    [Theory]
    [InlineData("texture", "hero", "TEXR", "hero")]
    [InlineData("shader", "sprite", "SHDR", "sprite")]
    [InlineData("sprite", "attack", "SPRT", "attack")]
    [InlineData("font", "default", "FONT", "default")]
    [InlineData("sound", "click", "SOND", "click")]
    public void MapsSubdirectory_ToAssetType(string subdir, string filename, string expectedFourCC, string expectedName)
    {
        var result = FileSystemWatcherSource.ParseAssetPath($"library/{subdir}/{filename}");
        Assert.NotNull(result);
        Assert.Equal(AssetType.FromString(expectedFourCC), result.Value.Type);
        Assert.Equal(expectedName, result.Value.Name);
    }

    [Fact]
    public void ParseAssetPath_UnknownSubdir_ReturnsNull()
    {
        var result = FileSystemWatcherSource.ParseAssetPath("library/unknown/foo");
        Assert.Null(result);
    }
}

public class TestFileChangeSource : IFileChangeSource
{
    public event Action<AssetType, string>? FileChanged;

    public void Start(string watchPath) { }
    public void Stop() { }

    public void SimulateChange(AssetType type, string name)
        => FileChanged?.Invoke(type, name);
}
