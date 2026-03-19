//  NoZ.Tests - Mock IPlatform
//
//  OpenAssetStream returns from an in-memory dictionary keyed by (AssetType, name).
//  Tests register fake binary asset data before loading.

using System.Numerics;
using NoZ;
using NoZ.Platform;

namespace NoZ.Tests.Mocks;

public class TestPlatform : IPlatform
{
    private readonly Dictionary<(AssetType, string), byte[]> _assetData = new();

    public void RegisterAssetData(AssetType type, string name, byte[] data)
    {
        _assetData[(type, name)] = data;
    }

    public Stream? OpenAssetStream(AssetType type, string name, string extension, string? libraryPath = null)
    {
        if (_assetData.TryGetValue((type, name), out var data))
            return new MemoryStream(data);
        return null;
    }

    public void Init(PlatformConfig config) { }
    public void Shutdown() { }
    public bool PollEvents() => true;
    public void SwapBuffers() { }

    public Vector2Int WindowSize => new(800, 600);
    public Vector2Int WindowPosition => new(100, 100);
    public void SetWindowSize(int width, int height) { }
    public void SetWindowPosition(int x, int y) { }
    public float DisplayScale => 1.0f;

    public event Action<PlatformEvent>? OnEvent;
    public void FireEvent(PlatformEvent evt) => OnEvent?.Invoke(evt);

    public void SetResizeCallback(Action? callback) { }

    public void ShowTextbox(Rect rect, string text, NativeTextboxStyle style) { }
    public void HideTextbox() { }
    public void UpdateTextboxRect(Rect rect, int fontSize) { }
    public bool UpdateTextboxText(ref string text) => false;
    public bool IsTextboxVisible => false;

    public void SetClipboardText(string text) { }
    public string? GetClipboardText() => null;

    public bool IsMouseInWindow => true;
    public bool IsMouseCaptured => false;
    public void SetMouseCapture(bool enabled) { }
    public void SetCursor(SystemCursor cursor) { }

    public bool IsFullscreen => false;
    public void SetFullscreen(bool fullscreen) { }
    public void SetVSync(bool vsync) { }

    public nint WindowHandle => nint.Zero;
    public nint GetGraphicsProcAddress(string name) => nint.Zero;

    public Stream? LoadPersistentData(string name, string? appName = null) => null;
    public void SavePersistentData(string name, Stream data, string? appName = null) { }

    public void Log(string message) { }
    public void OpenURL(string url) { }
}
