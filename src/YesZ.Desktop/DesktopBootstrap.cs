//  YesZ - Desktop Bootstrap
//
//  Helper for launching YesZ apps on the desktop platform.
//  Wraps NoZ's Application.Init/Run/Shutdown with YesZ defaults.
//
//  Depends on: NoZ (Application, SDLPlatform, WebGPUGraphicsDriver)
//  Used by:    Desktop samples and games

using NoZ;
using NoZ.Platform;
using NoZ.Platform.WebGPU;

namespace YesZ.Desktop;

public static class DesktopBootstrap
{
    public static void Run(IApplication app, string title = "YesZ", int width = 1280, int height = 720)
    {
        Application.Init(new ApplicationConfig
        {
            Title = title,
            Width = width,
            Height = height,
            Platform = new SDLPlatform(),
            AudioBackend = new SDLAudioDriver(),
            Vtable = app,
            ResourceAssembly = app.GetType().Assembly,
            AssetPath = FindAssetLibrary(),
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
        // Walk up from the output directory to find the solution root (yesz.slnx),
        // then resolve the NoZ editor asset library path.
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "yesz.slnx")))
                return Path.Combine(dir, "engine", "noz", "editor", "library");
            dir = Directory.GetParent(dir)?.FullName;
        }

        // Fallback: CWD-relative path (works when running from repo root)
        return Path.Combine(Directory.GetCurrentDirectory(), "engine", "noz", "editor", "library");
    }
}
