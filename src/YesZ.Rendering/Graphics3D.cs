//  YesZ - 3D Graphics
//
//  Static entry point for 3D rendering.
//  Begin() enables depth testing and sets the 3D projection.
//  End() restores 2D state for NoZ UI overlay.
//
//  Depends on: YesZ.Core (Camera3D, Transform3D), NoZ (IGraphicsDriver)
//  Used by:    Game code, samples

namespace YesZ.Rendering;

/// <summary>
/// 3D rendering interface. Call Begin() before drawing 3D content,
/// End() when done to restore NoZ's 2D state for UI overlay.
/// </summary>
public static class Graphics3D
{
    /// <summary>
    /// Begin 3D rendering pass. Enables depth testing and sets
    /// the perspective projection from the given camera.
    /// </summary>
    public static void Begin(Camera3D camera)
    {
        // Phase 2: Enable depth test, set 3D projection uniform
        // For now this is a stub
    }

    /// <summary>
    /// End 3D rendering pass. Disables depth testing and restores
    /// NoZ's 2D orthographic projection for UI overlay.
    /// </summary>
    public static void End()
    {
        // Phase 2: Disable depth test, restore 2D projection
    }
}
