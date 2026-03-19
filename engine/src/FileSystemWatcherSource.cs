//  NoZ - FileSystemWatcherSource
//
//  Watches the asset library directory for file changes and maps paths
//  to (AssetType, name) pairs for the AssetWatcher queue.
//
//  Depends on: IFileChangeSource, AssetType
//  Used by:    AssetWatcher

namespace NoZ;

public class FileSystemWatcherSource : IFileChangeSource
{
    private FileSystemWatcher? _watcher;

    public event Action<AssetType, string>? FileChanged;

    public void Start(string watchPath)
    {
        _watcher = new FileSystemWatcher(watchPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
    }

    public void Stop()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var relativePath = e.FullPath;
        var parsed = ParseAssetPath(relativePath);
        if (parsed.HasValue)
            FileChanged?.Invoke(parsed.Value.Type, parsed.Value.Name);
    }

    public static (AssetType Type, string Name)? ParseAssetPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        var parts = normalized.Split('/');

        // Find "library/{type}/{name}" pattern
        for (int i = 0; i < parts.Length - 2; i++)
        {
            if (string.Equals(parts[i], "library", StringComparison.OrdinalIgnoreCase))
            {
                var typeDir = parts[i + 1].ToLowerInvariant();
                var name = Path.GetFileNameWithoutExtension(parts[i + 2]);

                var assetType = typeDir switch
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

                if (assetType != AssetType.Unknown)
                    return (assetType, name);

                return null;
            }
        }

        return null;
    }
}
