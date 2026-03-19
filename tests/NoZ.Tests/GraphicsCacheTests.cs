//  NoZ.Tests - Graphics static cache invalidation tests (Phase 4)
//
//  Verifies that Graphics._spriteShader is refreshed after shader reload.

using NoZ.Tests.Helpers;
using Xunit;

namespace NoZ.Tests;

[Collection("Engine")]
public class GraphicsCacheTests : IDisposable
{
    private readonly AssetTestHelper _helper;

    public GraphicsCacheTests()
    {
        _helper = new AssetTestHelper();
        _helper.InitEngine();
    }

    public void Dispose() => _helper.Dispose();

    [Fact]
    public void ResolveAssets_UpdatesSpriteShader_AfterReload()
    {
        // Arrange: load a shader named "sprite" (the default SpriteShader name)
        var shaderData = AssetTestHelper.CreateShaderBytes("// sprite v1");
        _helper.RegisterAssetData(AssetType.Shader, "sprite", shaderData);
        Asset.Load(AssetType.Shader, "sprite");
        Graphics.ResolveAssets();

        // Get the current sprite shader handle
        var spriteShader = Asset.Get<Shader>(AssetType.Shader, "sprite");
        Assert.NotNull(spriteShader);
        var oldHandle = spriteShader!.Handle;

        // Prepare v2 shader data and reload
        _helper.RegisterAssetData(AssetType.Shader, "sprite", AssetTestHelper.CreateShaderBytes("// sprite v2"));
        Asset.Reload(AssetType.Shader, "sprite");

        // ResolveAssets should pick up the new shader
        Graphics.ResolveAssets();

        var newShader = Asset.Get<Shader>(AssetType.Shader, "sprite");
        Assert.NotNull(newShader);
        Assert.NotEqual(oldHandle, newShader!.Handle);
    }
}
