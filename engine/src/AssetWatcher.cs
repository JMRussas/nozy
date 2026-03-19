//  NoZ - AssetWatcher
//
//  Queue-based asset reload coordinator. External systems (FileSystemWatcher,
//  CommandServer) enqueue reload requests. ProcessReloadQueue() drains the
//  queue and executes reloads in dependency order during a safe frame window.
//
//  Depends on: Asset, Graphics
//  Used by:    Application (frame loop), CommandServer, IFileChangeSource

namespace NoZ;

public class AssetWatcher
{
    private readonly HashSet<(AssetType, string)> _pendingSet = new();
    private readonly Queue<(AssetType Type, string Name)> _pendingQueue = new();
    private readonly object _lock = new();

    public event Action<AssetType, string>? OnAssetReloaded;

    public void Subscribe(IFileChangeSource source)
    {
        source.FileChanged += (type, name) => EnqueueReload(type, name);
    }

    public void EnqueueReload(AssetType type, string name)
    {
        lock (_lock)
        {
            if (_pendingSet.Add((type, name)))
                _pendingQueue.Enqueue((type, name));
        }
    }

    public void ProcessReloadQueue()
    {
        List<(AssetType Type, string Name)> items;

        lock (_lock)
        {
            if (_pendingQueue.Count == 0)
                return;

            items = new List<(AssetType, string)>(_pendingQueue);
            _pendingQueue.Clear();
            _pendingSet.Clear();
        }

        items.Sort((a, b) => GetReloadPriority(a.Type).CompareTo(GetReloadPriority(b.Type)));

        var shaderReloaded = false;
        foreach (var (type, name) in items)
        {
            Asset.Reload(type, name);
            if (type == AssetType.Shader)
                shaderReloaded = true;
            OnAssetReloaded?.Invoke(type, name);
        }

        if (shaderReloaded)
            Graphics.ResolveAssets();
    }

    private static int GetReloadPriority(AssetType type)
    {
        if (type == AssetType.Shader) return 0;
        if (type == AssetType.Atlas) return 1;
        if (type == AssetType.Texture) return 2;
        if (type == AssetType.Font) return 3;
        if (type == AssetType.Sound) return 4;
        return 5;
    }
}
