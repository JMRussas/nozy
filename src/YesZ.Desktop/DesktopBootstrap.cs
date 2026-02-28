//  YesZ - Desktop Bootstrap
//
//  Helper for launching YesZ apps on the desktop platform.
//  Wraps NoZ's Application.Init/Run/Shutdown with YesZ defaults.
//  Automatically loads built-in NoZ assets (shaders, fonts) before the app's LoadAssets.
//
//  Depends on: NoZ (Application, SDLPlatform, WebGPUGraphicsDriver, IApplication, Asset)
//  Used by:    Desktop samples and games

using NoZ;
using NoZ.Platform;
using NoZ.Platform.WebGPU;

namespace YesZ.Desktop;

public static class DesktopBootstrap
{
    public static void Run(IApplication app, string title = "YesZ", int width = 1280, int height = 720)
    {
        var assetPath = FindAssetLibrary();

        Application.Init(new ApplicationConfig
        {
            Title = title,
            Width = width,
            Height = height,
            Platform = new SDLPlatform(),
            AudioBackend = new SDLAudioDriver(),
            Vtable = new BootstrappedApp(app),
            ResourceAssembly = app.GetType().Assembly,
            AssetPath = assetPath,
            Graphics = new GraphicsConfig
            {
                Driver = new WebGPUGraphicsDriver()
            },
            UI = new UIConfig
            {
                DefaultFont = "seguisb"
            }
        });

        Application.Run();
        Application.Shutdown();
    }

    /// <summary>
    /// Walks up from the executing assembly directory to find the solution root
    /// (containing yesz.slnx), then resolves the NoZ editor asset library path.
    /// </summary>
    private static string FindAssetLibrary()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "yesz.slnx")))
            {
                var path = Path.Combine(dir, "engine", "noz", "editor", "library");
                if (Directory.Exists(path))
                    return path;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }

        // Fallback: CWD-relative path (works when running from repo root)
        var fallback = Path.Combine(Directory.GetCurrentDirectory(), "engine", "noz", "editor", "library");
        if (!Directory.Exists(fallback))
            throw new DirectoryNotFoundException(
                $"NoZ asset library not found. Expected at: engine/noz/editor/library/ relative to solution root (yesz.slnx). Searched from: {AppContext.BaseDirectory}");

        return fallback;
    }

    /// <summary>
    /// Wraps a user IApplication to load built-in NoZ assets before the app's own LoadAssets.
    /// </summary>
    private class BootstrappedApp(IApplication inner) : IApplication
    {
        public void LoadAssets()
        {
            // Built-in NoZ assets required by Graphics, TextRender, and UI systems.
            Asset.Load(AssetType.Shader, "sprite");
            Asset.Load(AssetType.Shader, "text");
            Asset.Load(AssetType.Shader, "ui");
            Asset.Load(AssetType.Shader, "texture");
            Asset.Load(AssetType.Font, "seguisb");

            inner.LoadAssets();
        }

        public void Update() => inner.Update();
        public void UpdateUI() => inner.UpdateUI();
        public void LateUpdate() => inner.LateUpdate();
        public void LoadConfig(ApplicationConfig config) => inner.LoadConfig(config);
        public void SaveConfig() => inner.SaveConfig();
        public void UnloadAssets() => inner.UnloadAssets();
        public void ReloadAssets() => inner.ReloadAssets();
    }
}
