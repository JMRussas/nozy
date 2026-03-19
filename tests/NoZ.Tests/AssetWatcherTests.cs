//  NoZ.Tests - AssetWatcher queue tests (Phase 5)
//
//  Tests for queue-based reload coordination, deduplication, and dependency ordering.

using NoZ.Tests.Helpers;
using Xunit;

namespace NoZ.Tests;

[Collection("Engine")]
public class AssetWatcherTests : IDisposable
{
    private readonly AssetTestHelper _helper;

    public AssetWatcherTests()
    {
        _helper = new AssetTestHelper();
        _helper.InitEngine();
    }

    public void Dispose() => _helper.Dispose();

    [Fact]
    public void EnqueueReload_ProcessesOnNextCall()
    {
        // Arrange
        var texData = AssetTestHelper.CreateTextureBytes();
        _helper.RegisterAssetData(AssetType.Texture, "watcher_tex", texData);
        var original = Asset.Load(AssetType.Texture, "watcher_tex") as Texture;
        Assert.NotNull(original);
        var oldHandle = original!.Handle;

        // Prepare new data
        _helper.RegisterAssetData(AssetType.Texture, "watcher_tex", AssetTestHelper.CreateTextureBytes(8, 8));

        var watcher = new AssetWatcher();
        watcher.EnqueueReload(AssetType.Texture, "watcher_tex");

        // Act
        watcher.ProcessReloadQueue();

        // Assert: asset was reloaded (new handle)
        var reloaded = Asset.Get<Texture>(AssetType.Texture, "watcher_tex");
        Assert.NotNull(reloaded);
        Assert.NotEqual(oldHandle, reloaded!.Handle);
    }

    [Fact]
    public void EnqueueReload_Deduplicates_SameAsset()
    {
        // Arrange
        var texData = AssetTestHelper.CreateTextureBytes();
        _helper.RegisterAssetData(AssetType.Texture, "dedup_tex", texData);
        Asset.Load(AssetType.Texture, "dedup_tex");
        _helper.RegisterAssetData(AssetType.Texture, "dedup_tex", AssetTestHelper.CreateTextureBytes(8, 8));

        var watcher = new AssetWatcher();

        // Enqueue the same asset 3 times
        watcher.EnqueueReload(AssetType.Texture, "dedup_tex");
        watcher.EnqueueReload(AssetType.Texture, "dedup_tex");
        watcher.EnqueueReload(AssetType.Texture, "dedup_tex");

        // Act
        var createsBefore = _helper.Graphics.CreatedTextures.Count;
        watcher.ProcessReloadQueue();
        var createsAfter = _helper.Graphics.CreatedTextures.Count;

        // Assert: only 1 new texture created (not 3)
        Assert.Equal(1, createsAfter - createsBefore);
    }

    [Fact]
    public void ProcessQueue_ReloadsInDependencyOrder()
    {
        // Arrange: register all three asset types
        // Also need "sprite" shader so ResolveAssets() doesn't throw when a shader reload triggers it
        _helper.RegisterAssetData(AssetType.Shader, "sprite", AssetTestHelper.CreateShaderBytes("// sprite"));
        Asset.Load(AssetType.Shader, "sprite");

        _helper.RegisterAssetData(AssetType.Sprite, "order_sprite", CreateMinimalSpriteBytes());
        _helper.RegisterAssetData(AssetType.Texture, "order_tex", AssetTestHelper.CreateTextureBytes());
        _helper.RegisterAssetData(AssetType.Shader, "order_shader", AssetTestHelper.CreateShaderBytes());

        Asset.Load(AssetType.Sprite, "order_sprite");
        Asset.Load(AssetType.Texture, "order_tex");
        Asset.Load(AssetType.Shader, "order_shader");

        // Prepare new data for all
        _helper.RegisterAssetData(AssetType.Sprite, "order_sprite", CreateMinimalSpriteBytes());
        _helper.RegisterAssetData(AssetType.Texture, "order_tex", AssetTestHelper.CreateTextureBytes(8, 8));
        _helper.RegisterAssetData(AssetType.Shader, "order_shader", AssetTestHelper.CreateShaderBytes("// v2"));

        var reloadOrder = new List<AssetType>();
        var watcher = new AssetWatcher();
        watcher.OnAssetReloaded += (type, _) => reloadOrder.Add(type);

        // Enqueue in wrong order: Sprite, Texture, Shader
        watcher.EnqueueReload(AssetType.Sprite, "order_sprite");
        watcher.EnqueueReload(AssetType.Texture, "order_tex");
        watcher.EnqueueReload(AssetType.Shader, "order_shader");

        // Act
        watcher.ProcessReloadQueue();

        // Assert: Shader before Texture before Sprite
        Assert.Equal(3, reloadOrder.Count);
        Assert.Equal(AssetType.Shader, reloadOrder[0]);
        Assert.Equal(AssetType.Texture, reloadOrder[1]);
        Assert.Equal(AssetType.Sprite, reloadOrder[2]);
    }

    [Fact]
    public void ProcessQueue_CallsResolveAssets_WhenShaderReloaded()
    {
        // Arrange: load the sprite shader
        var shaderData = AssetTestHelper.CreateShaderBytes("// sprite v1");
        _helper.RegisterAssetData(AssetType.Shader, "sprite", shaderData);
        Asset.Load(AssetType.Shader, "sprite");
        Graphics.ResolveAssets();

        var oldShader = Asset.Get<Shader>(AssetType.Shader, "sprite");
        Assert.NotNull(oldShader);
        var oldHandle = oldShader!.Handle;

        // Prepare new shader
        _helper.RegisterAssetData(AssetType.Shader, "sprite", AssetTestHelper.CreateShaderBytes("// sprite v2"));

        var watcher = new AssetWatcher();
        watcher.EnqueueReload(AssetType.Shader, "sprite");

        // Act
        watcher.ProcessReloadQueue();

        // Assert: Graphics._spriteShader was refreshed (new shader in registry)
        var newShader = Asset.Get<Shader>(AssetType.Shader, "sprite");
        Assert.NotNull(newShader);
        Assert.NotEqual(oldHandle, newShader!.Handle);
    }

    private static byte[] CreateMinimalSpriteBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        AssetTestHelper.WriteAssetHeader(writer, AssetType.Sprite, Sprite.Version);
        writer.Write((ushort)1);  // frameCount
        writer.Write((ushort)0);  // atlasIndex
        writer.Write((short)0);   // bounds left
        writer.Write((short)0);   // bounds top
        writer.Write((short)32);  // bounds right
        writer.Write((short)32);  // bounds bottom
        writer.Write(64.0f);      // pixelsPerUnit
        writer.Write((byte)0);    // filter
        writer.Write((short)-1);  // boneIndex
        writer.Write((ushort)1);  // meshCount
        writer.Write(12.0f);      // frameRate
        // edges
        writer.Write((short)0); writer.Write((short)0);
        writer.Write((short)0); writer.Write((short)0);
        writer.Write((ushort)0);  // sliceMask
        // mesh[0]: UV rect + sort order + bone index + offset + size
        writer.Write(0.0f); writer.Write(0.0f); writer.Write(1.0f); writer.Write(1.0f);
        writer.Write((short)0); writer.Write((short)-1);
        writer.Write((short)0); writer.Write((short)0);
        writer.Write((short)32); writer.Write((short)32);
        // frameTable[0]
        writer.Write((ushort)0);  // meshStart
        writer.Write((ushort)1);  // meshCount
        return ms.ToArray();
    }
}
