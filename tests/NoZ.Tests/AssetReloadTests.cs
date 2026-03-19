//  NoZ.Tests - Asset reload tests (Phase 1 + Phase 2)
//
//  Tests for Asset.Reload(), registry management, and GPU resource lifecycle.
//  Phase 1: CPU-only registry operations
//  Phase 2: GPU asset reload (Texture, Shader, Sound)

using NoZ.Tests.Helpers;
using NoZ.Tests.Mocks;
using Xunit;

namespace NoZ.Tests;

[Collection("Engine")]
public class AssetReloadTests : IDisposable
{
    private readonly AssetTestHelper _helper;

    public AssetReloadTests()
    {
        _helper = new AssetTestHelper();
        _helper.InitEngine();
    }

    public void Dispose() => _helper.Dispose();

    // ── Phase 1: Registry Operations ──────────────────────────────────

    [Fact]
    public void Reload_ReplacesRegistryEntry()
    {
        // Arrange: load a texture
        var texData = AssetTestHelper.CreateTextureBytes(4, 4);
        _helper.RegisterAssetData(AssetType.Texture, "test_tex", texData);
        var original = Asset.Load(AssetType.Texture, "test_tex");
        Assert.NotNull(original);

        // Prepare v2 data (different dimensions)
        var texData2 = AssetTestHelper.CreateTextureBytes(8, 8);
        _helper.RegisterAssetData(AssetType.Texture, "test_tex", texData2);

        // Act: reload
        var reloaded = Asset.Reload(AssetType.Texture, "test_tex");

        // Assert: registry returns new asset, not the old one
        Assert.NotNull(reloaded);
        Assert.NotSame(original, reloaded);
        var fetched = Asset.Get<Texture>(AssetType.Texture, "test_tex");
        Assert.Same(reloaded, fetched);
    }

    [Fact]
    public void Reload_DisposesOldAsset()
    {
        // Arrange
        var texData = AssetTestHelper.CreateTextureBytes();
        _helper.RegisterAssetData(AssetType.Texture, "dispose_test", texData);
        var original = Asset.Load(AssetType.Texture, "dispose_test") as Texture;
        Assert.NotNull(original);
        var oldHandle = original!.Handle;
        Assert.NotEqual(nuint.Zero, oldHandle);

        // Prepare new data
        _helper.RegisterAssetData(AssetType.Texture, "dispose_test", AssetTestHelper.CreateTextureBytes());

        // Act
        Asset.Reload(AssetType.Texture, "dispose_test");

        // Assert: old GPU handle was destroyed
        Assert.Contains(oldHandle, _helper.Graphics.DestroyedTextures);
    }

    [Fact]
    public void Reload_NonexistentAsset_LoadsFresh()
    {
        // Arrange: register data but don't pre-load
        var texData = AssetTestHelper.CreateTextureBytes();
        _helper.RegisterAssetData(AssetType.Texture, "fresh_load", texData);

        // Act
        var loaded = Asset.Reload(AssetType.Texture, "fresh_load");

        // Assert
        Assert.NotNull(loaded);
        var fetched = Asset.Get<Texture>(AssetType.Texture, "fresh_load");
        Assert.Same(loaded, fetched);
    }

    [Fact]
    public void Dispose_RemovesFromRegistry()
    {
        // Arrange
        var texData = AssetTestHelper.CreateTextureBytes();
        _helper.RegisterAssetData(AssetType.Texture, "dispose_reg", texData);
        var asset = Asset.Load(AssetType.Texture, "dispose_reg");
        Assert.NotNull(asset);
        Assert.NotNull(Asset.Get<Texture>(AssetType.Texture, "dispose_reg"));

        // Act
        asset!.Dispose();

        // Assert: registry no longer contains the asset
        Assert.Null(Asset.Get<Texture>(AssetType.Texture, "dispose_reg"));
    }

    // ── Phase 2: GPU Asset Reload ─────────────────────────────────────

    [Fact]
    public void TextureReload_DestroysOldHandle_CreatesNew()
    {
        // Arrange
        var texData = AssetTestHelper.CreateTextureBytes();
        _helper.RegisterAssetData(AssetType.Texture, "gpu_tex", texData);
        var original = Asset.Load(AssetType.Texture, "gpu_tex") as Texture;
        Assert.NotNull(original);
        var oldHandle = original!.Handle;
        var createCountBefore = _helper.Graphics.CreatedTextures.Count;

        // Prepare new data and reload
        _helper.RegisterAssetData(AssetType.Texture, "gpu_tex", AssetTestHelper.CreateTextureBytes(16, 16));

        // Act
        var reloaded = Asset.Reload(AssetType.Texture, "gpu_tex") as Texture;

        // Assert
        Assert.NotNull(reloaded);
        Assert.Contains(oldHandle, _helper.Graphics.DestroyedTextures);
        Assert.NotEqual(oldHandle, reloaded!.Handle);
        Assert.True(_helper.Graphics.CreatedTextures.Count > createCountBefore);
    }

    [Fact]
    public void ShaderReload_DestroysOldHandle_CreatesNew()
    {
        // Arrange
        var shaderData = AssetTestHelper.CreateShaderBytes("// v1");
        _helper.RegisterAssetData(AssetType.Shader, "test_shader", shaderData);
        var original = Asset.Load(AssetType.Shader, "test_shader") as Shader;
        Assert.NotNull(original);
        var oldHandle = original!.Handle;

        // Prepare new data
        _helper.RegisterAssetData(AssetType.Shader, "test_shader", AssetTestHelper.CreateShaderBytes("// v2"));

        // Act
        var reloaded = Asset.Reload(AssetType.Shader, "test_shader") as Shader;

        // Assert
        Assert.NotNull(reloaded);
        Assert.Contains(oldHandle, _helper.Graphics.DestroyedShaders);
        Assert.NotEqual(oldHandle, reloaded!.Handle);
    }

    [Fact]
    public void SoundReload_DestroysOldHandle_CreatesNew()
    {
        // Arrange
        var soundData = AssetTestHelper.CreateSoundBytes();
        _helper.RegisterAssetData(AssetType.Sound, "test_sound", soundData);
        var original = Asset.Load(AssetType.Sound, "test_sound") as Sound;
        Assert.NotNull(original);
        var oldHandle = original!.PlatformHandle;

        // Prepare new data
        _helper.RegisterAssetData(AssetType.Sound, "test_sound", AssetTestHelper.CreateSoundBytes(48000));

        // Act
        var reloaded = Asset.Reload(AssetType.Sound, "test_sound") as Sound;

        // Assert
        Assert.NotNull(reloaded);
        Assert.Contains(oldHandle, _helper.Audio.DestroyedSounds);
        Assert.NotEqual(oldHandle, reloaded!.PlatformHandle);
    }
}
