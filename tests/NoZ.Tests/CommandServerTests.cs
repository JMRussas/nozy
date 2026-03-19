//  NoZ.Tests - CommandServer tests (Phase 7)
//
//  Tests for WebSocket command parsing, dispatch, and error handling.

using System.Text;
using System.Text.Json;
using NoZ.Tests.Helpers;
using Xunit;

namespace NoZ.Tests;

[Collection("Engine")]
public class CommandServerTests : IDisposable
{
    private readonly AssetTestHelper _helper;

    public CommandServerTests()
    {
        _helper = new AssetTestHelper();
        _helper.InitEngine();
    }

    public void Dispose() => _helper.Dispose();

    [Fact]
    public void ParseCommand_ReloadSingleAsset()
    {
        // Arrange
        _helper.RegisterAssetData(AssetType.Texture, "hero", AssetTestHelper.CreateTextureBytes());
        Asset.Load(AssetType.Texture, "hero");
        _helper.RegisterAssetData(AssetType.Texture, "hero", AssetTestHelper.CreateTextureBytes(16, 16));

        var watcher = new AssetWatcher();
        var server = new CommandServer(watcher);

        var json = """{"cmd":"reload","type":"Texture","name":"hero"}""";
        var bytes = Encoding.UTF8.GetBytes(json);

        // Act
        var response = server.HandleMessage(bytes);
        watcher.ProcessReloadQueue();

        // Assert
        var reloaded = Asset.Get<Texture>(AssetType.Texture, "hero");
        Assert.NotNull(reloaded);
        Assert.NotNull(response);
        Assert.Contains("ok", Encoding.UTF8.GetString(response!));
    }

    [Fact]
    public void ParseCommand_Ping_ReturnsOk()
    {
        var watcher = new AssetWatcher();
        var server = new CommandServer(watcher);

        var json = """{"cmd":"ping"}""";
        var response = server.HandleMessage(Encoding.UTF8.GetBytes(json));

        Assert.NotNull(response);
        var responseStr = Encoding.UTF8.GetString(response!);
        Assert.Contains("ok", responseStr);
    }

    [Fact]
    public void ParseCommand_InvalidJson_NoThrow()
    {
        var watcher = new AssetWatcher();
        var server = new CommandServer(watcher);

        // Garbage input should not throw
        var response = server.HandleMessage(Encoding.UTF8.GetBytes("not json at all {{{"));
        Assert.NotNull(response);
        var responseStr = Encoding.UTF8.GetString(response!);
        Assert.Contains("error", responseStr);
    }

    [Fact]
    public void ParseCommand_UnknownCommand_ReturnsError()
    {
        var watcher = new AssetWatcher();
        var server = new CommandServer(watcher);

        var json = """{"cmd":"explode"}""";
        var response = server.HandleMessage(Encoding.UTF8.GetBytes(json));

        Assert.NotNull(response);
        var responseStr = Encoding.UTF8.GetString(response!);
        Assert.Contains("error", responseStr);
    }
}
