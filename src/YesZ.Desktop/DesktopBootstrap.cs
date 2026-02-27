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
            Graphics = new GraphicsConfig
            {
                Driver = new WebGPUGraphicsDriver()
            }
        });

        Application.Run();
        Application.Shutdown();
    }
}
