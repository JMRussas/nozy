//  NoZ - IFileChangeSource
//
//  Interface for pluggable file change detection. FileSystemWatcher is one
//  implementation; tests provide a manual trigger.
//
//  Depends on: AssetType
//  Used by:    AssetWatcher

namespace NoZ;

public interface IFileChangeSource
{
    event Action<AssetType, string>? FileChanged;
    void Start(string watchPath);
    void Stop();
}
