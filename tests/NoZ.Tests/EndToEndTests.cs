//  NoZ.Tests - End-to-end integration tests (Phase 8)
//
//  Full round-trip: file change -> queue -> process -> GPU handle swap.

using NoZ.Tests.Helpers;
using Xunit;

namespace NoZ.Tests;

[Collection("Engine")]
public class EndToEndTests : IDisposable
{
    private readonly AssetTestHelper _helper;

    public EndToEndTests()
    {
        _helper = new AssetTestHelper();
        _helper.InitEngine();
    }

    public void Dispose() => _helper.Dispose();

    [Fact]
    public void FileChange_TriggersReload_ViaWatcher()
    {
        // Arrange: load texture, set up watcher with test file source
        var texData = AssetTestHelper.CreateTextureBytes(4, 4);
        _helper.RegisterAssetData(AssetType.Texture, "e2e_tex", texData);
        var original = Asset.Load(AssetType.Texture, "e2e_tex") as Texture;
        Assert.NotNull(original);
        var oldHandle = original!.Handle;

        var source = new TestFileChangeSource();
        var watcher = new AssetWatcher();
        watcher.Subscribe(source);

        // Prepare new data
        _helper.RegisterAssetData(AssetType.Texture, "e2e_tex", AssetTestHelper.CreateTextureBytes(16, 16));

        // Act: simulate file change, then process (mimics frame loop)
        source.SimulateChange(AssetType.Texture, "e2e_tex");
        watcher.ProcessReloadQueue();

        // Assert
        var reloaded = Asset.Get<Texture>(AssetType.Texture, "e2e_tex");
        Assert.NotNull(reloaded);
        Assert.NotEqual(oldHandle, reloaded!.Handle);
        Assert.Contains(oldHandle, _helper.Graphics.DestroyedTextures);
    }

    [Fact]
    public void CommandServer_ReloadCommand_FullRoundTrip()
    {
        // Arrange: load shader, set up command server + watcher
        var shaderData = AssetTestHelper.CreateShaderBytes("// v1");
        _helper.RegisterAssetData(AssetType.Shader, "sprite", shaderData);
        _helper.RegisterAssetData(AssetType.Shader, "e2e_shader", shaderData);
        Asset.Load(AssetType.Shader, "sprite");
        Asset.Load(AssetType.Shader, "e2e_shader");
        Graphics.ResolveAssets();
        var oldHandle = (Asset.Get<Shader>(AssetType.Shader, "e2e_shader"))!.Handle;

        var watcher = new AssetWatcher();
        var server = new CommandServer(watcher);

        // Prepare new data
        _helper.RegisterAssetData(AssetType.Shader, "e2e_shader", AssetTestHelper.CreateShaderBytes("// v2"));

        // Act: send reload command, then process queue
        var json = """{"cmd":"reload","type":"Shader","name":"e2e_shader"}""";
        server.HandleMessage(System.Text.Encoding.UTF8.GetBytes(json));
        watcher.ProcessReloadQueue();

        // Assert
        var reloaded = Asset.Get<Shader>(AssetType.Shader, "e2e_shader");
        Assert.NotNull(reloaded);
        Assert.NotEqual(oldHandle, reloaded!.Handle);
        Assert.Contains(oldHandle, _helper.Graphics.DestroyedShaders);
    }
}
