//  NoZ - CommandServer
//
//  Parses JSON commands from external tools and dispatches them to AssetWatcher.
//  Commands arrive as UTF-8 bytes in WebSocket Binary frames.
//
//  Depends on: AssetWatcher, AssetType, INetworkDriver
//  Used by:    Application (frame loop)

using System.Text;
using System.Text.Json;
using NoZ.Platform;

namespace NoZ;

public class CommandServer
{
    private readonly AssetWatcher _watcher;
    private INetworkDriver? _network;
    private int _port;

    public CommandServer(AssetWatcher watcher)
    {
        _watcher = watcher;
    }

    public void Start(INetworkDriver network, int port = 19999)
    {
        _network = network;
        _port = port;
        _network.StartServer(_port, 4);
        Log.Info($"CommandServer listening on port {_port}");
    }

    public void Stop()
    {
        _network?.StopServer();
        _network = null;
    }

    public void ProcessMessages()
    {
        if (_network == null)
            return;

        while (_network.TryReceiveServer(out var connId, out var msg))
        {
            var response = HandleMessage(msg.Data.AsSpan(0, msg.Length));
            if (response != null)
                _network.SendTo(connId, response);
        }
    }

    public byte[]? HandleMessage(ReadOnlySpan<byte> data)
    {
        try
        {
            var json = Encoding.UTF8.GetString(data);
            // Defensive: strip markdown fences if present
            json = json.Trim();
            if (json.StartsWith("```"))
            {
                var firstNewline = json.IndexOf('\n');
                if (firstNewline >= 0)
                    json = json[(firstNewline + 1)..];
                if (json.EndsWith("```"))
                    json = json[..^3];
                json = json.Trim();
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("cmd", out var cmdElement))
                return MakeError("missing 'cmd' field");

            var cmd = cmdElement.GetString();

            return cmd switch
            {
                "ping" => MakeOk("pong"),
                "reload" => HandleReload(root),
                "list" => HandleList(root),
                _ => MakeError($"unknown command: {cmd}"),
            };
        }
        catch (JsonException)
        {
            return MakeError("invalid JSON");
        }
        catch (Exception ex)
        {
            return MakeError(ex.Message);
        }
    }

    private byte[] HandleReload(JsonElement root)
    {
        if (!root.TryGetProperty("type", out var typeEl) || !root.TryGetProperty("name", out var nameEl))
            return MakeError("reload requires 'type' and 'name'");

        var typeName = typeEl.GetString();
        var name = nameEl.GetString();
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(name))
            return MakeError("type and name must be non-empty");

        var assetType = ParseAssetTypeName(typeName!);
        if (assetType == AssetType.Unknown)
            return MakeError($"unknown asset type: {typeName}");

        _watcher.EnqueueReload(assetType, name!);
        return MakeOk($"queued reload: {typeName}/{name}");
    }

    private static byte[] HandleList(JsonElement root)
    {
        // Return all registered asset type names
        var defs = Asset.GetAllDefs().Select(d => d.Name).ToArray();
        var json = JsonSerializer.Serialize(new { types = defs });
        return Encoding.UTF8.GetBytes(json);
    }

    private static AssetType ParseAssetTypeName(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "texture" => AssetType.Texture,
            "sprite" => AssetType.Sprite,
            "shader" => AssetType.Shader,
            "font" => AssetType.Font,
            "sound" => AssetType.Sound,
            "animation" => AssetType.Animation,
            "skeleton" => AssetType.Skeleton,
            "atlas" => AssetType.Atlas,
            "vfx" => AssetType.Vfx,
            _ => AssetType.Unknown,
        };
    }

    private static byte[] MakeOk(string message)
        => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { ok = true, message }));

    private static byte[] MakeError(string message)
        => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { ok = false, error = message }));
}
