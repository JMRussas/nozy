//  YesZ - 3D Graphics
//
//  Static entry point for 3D rendering.
//  Begin() sets the 3D perspective projection via the globals system.
//  DrawMesh() computes per-object MVP and issues draw commands through NoZ's batch system.
//  End() restores 2D state for NoZ UI overlay.
//
//  Design: MVP (model × view × projection) is stored in the globals system
//  as the "projection" matrix. This integrates with NoZ's batch system without
//  per-draw uniform buffer tracking. Each DrawMesh creates a unique globals
//  snapshot (different MVP → different batch state → separate draw call).
//
//  Depends on: YesZ.Core (Camera3D, Mesh3D, MeshVertex3D), NoZ (Graphics, Shader, ShaderFlags)
//  Used by:    Game code, samples

using System.Numerics;
using System.Reflection;
using NoZ;

namespace YesZ.Rendering;

public static class Graphics3D
{
    private static Shader? _shader;
    private static Camera3D? _camera;
    private static Matrix4x4 _savedProjection;
    private static Matrix4x4 _viewProjection;
    private static bool _initialized;

    /// <summary>
    /// Initialize the 3D rendering system. Must be called after Graphics is initialized
    /// (e.g., during LoadAssets).
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        var wgslSource = LoadEmbeddedShader("YesZ.Rendering.Shaders.unlit3d.wgsl");

        var bindings = new List<ShaderBinding>
        {
            new() { Binding = 0, Type = ShaderBindingType.UniformBuffer, Name = "globals" },
        };

        var handle = Graphics.Driver.CreateShader("unlit3d", wgslSource, wgslSource, bindings);
        var flags = ShaderFlags.Depth | ShaderFlags.DepthLess;

        try
        {
            _shader = Shader.CreateRaw("unlit3d", handle, flags, bindings, MeshVertex3D.VertexHash);
        }
        catch
        {
            Graphics.Driver.DestroyShader(handle);
            throw;
        }

        _initialized = true;
    }

    /// <summary>
    /// Begin 3D rendering pass. Saves the current 2D projection and stores
    /// the camera for per-draw MVP computation.
    /// </summary>
    public static void Begin(Camera3D camera)
    {
        if (!_initialized)
            Initialize();

        _savedProjection = Graphics.GetPassProjection();
        _camera = camera;
        _viewProjection = camera.ViewProjectionMatrix;
    }

    /// <summary>
    /// Draw a 3D mesh with the given world transform.
    /// Computes MVP and issues a draw command through NoZ's batch system.
    /// </summary>
    public static void DrawMesh(Mesh3D mesh, Matrix4x4 worldMatrix)
    {
        if (_camera == null || _shader == null) return;

        // MVP = model × view × projection (row-major multiplication order)
        var mvp = worldMatrix * _viewProjection;

        // NoZ's UploadGlobals transposes the projection before uploading to the GPU.
        // NoZ's 2D SetCamera constructs matrices pre-transposed for this cycle, but
        // System.Numerics helpers (CreateLookAt, CreatePerspectiveFieldOfView) produce
        // row-vector convention matrices. Pre-transposing here cancels NoZ's transpose,
        // so the GPU receives the correct bytes for WGSL's column-major `M * v`.
        Graphics.SetPassProjection(Matrix4x4.Transpose(mvp));

        // Set shader and mesh in batch state
        Graphics.SetShader(_shader);
        Graphics.SetMesh(mesh.RenderMesh);

        // Issue draw command
        Graphics.DrawElements(mesh.IndexCount, 0);
    }

    /// <summary>
    /// End 3D rendering pass. Restores the 2D orthographic projection
    /// for NoZ UI overlay rendering.
    /// </summary>
    public static void End()
    {
        // Restore 2D projection so subsequent UI draws use the correct matrix
        Graphics.SetPassProjection(_savedProjection);
        _camera = null;
    }

    private static string LoadEmbeddedShader(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded shader resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
