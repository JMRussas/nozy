//  NoZ.Tests - Font cross-asset dependency reload tests (Phase 3)
//
//  Font owns a child Texture (_atlasTexture). Reload must dispose the old
//  atlas texture and create a new one.

using NoZ.Tests.Helpers;
using Xunit;

namespace NoZ.Tests;

[Collection("Engine")]
public class FontReloadTests : IDisposable
{
    private readonly AssetTestHelper _helper;

    public FontReloadTests()
    {
        _helper = new AssetTestHelper();
        _helper.InitEngine();
    }

    public void Dispose() => _helper.Dispose();

    [Fact]
    public void FontReload_DisposesChildAtlasTexture()
    {
        // Arrange: load a font (creates internal _atlasTexture)
        var fontData = AssetTestHelper.CreateFontBytes(fontSize: 16, atlasWidth: 4, atlasHeight: 4);
        _helper.RegisterAssetData(AssetType.Font, "test_font", fontData);
        var original = Asset.Load(AssetType.Font, "test_font") as Font;
        Assert.NotNull(original);
        Assert.NotNull(original!.AtlasTexture);
        var oldAtlasHandle = original.AtlasTexture!.Handle;

        // Prepare v2 font data (different atlas size)
        var fontData2 = AssetTestHelper.CreateFontBytes(fontSize: 24, atlasWidth: 8, atlasHeight: 8);
        _helper.RegisterAssetData(AssetType.Font, "test_font", fontData2);

        // Act
        var reloaded = Asset.Reload(AssetType.Font, "test_font") as Font;

        // Assert
        Assert.NotNull(reloaded);
        Assert.NotSame(original, reloaded);
        // Old atlas texture GPU handle was destroyed
        Assert.Contains(oldAtlasHandle, _helper.Graphics.DestroyedTextures);
        // New font has a new atlas texture with a different handle
        Assert.NotNull(reloaded!.AtlasTexture);
        Assert.NotEqual(oldAtlasHandle, reloaded.AtlasTexture!.Handle);
    }
}
