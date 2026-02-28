//  YesZ - 3D Graphics
//
//  Static entry point for 3D rendering.
//  Begin() sets the 3D perspective projection via the globals system and clears per-frame light state.
//  DrawMesh() computes per-object MVP and issues draw commands through NoZ's batch system.
//  End() restores 2D state for NoZ UI overlay.
//
//  Design: MVP (model × view × projection) is stored in the globals system
//  as the "projection" matrix. This integrates with NoZ's batch system without
//  per-draw uniform buffer tracking. Each DrawMesh creates a unique globals
//  snapshot (different MVP → different batch state → separate draw call).
//
//  Materials: SetMaterial() binds a Material3D's shader, texture, and uniform
//  data. Per-batch uniform snapshots in NoZ's Graphics ensure each batch gets
//  the correct material uniform values during deferred execution.
//
//  Lighting: SetDirectionalLight(), SetAmbientLight(), AddPointLight() configure
//  the LightEnvironment for the current frame. Point lights are cleared each frame
//  in Begin(). Light data is uploaded to the GPU in Phase 3b.
//
//  Depends on: YesZ.Core (Camera3D, Mesh3D, MeshVertex3D, LightEnvironment, DirectionalLight, PointLight, AmbientLight),
//              YesZ.Rendering (Material3D, MaterialUniforms),
//              NoZ (Graphics, Shader, ShaderFlags, ShaderBinding, ShaderBindingType, TextureFormat, TextureFilter)
//  Used by:    Game code, samples

using System.Numerics;
using System.Reflection;
using NoZ;

namespace YesZ.Rendering;

public static class Graphics3D
{
    private static Shader? _unlitShader;
    private static Shader? _texturedShader;
    private static Camera3D? _camera;
    private static Matrix4x4 _savedProjection;
    private static Matrix4x4 _viewProjection;
    private static bool _initialized;
    private static nuint _defaultWhiteTexture;
    private static Material3D? _currentMaterial;
    private static readonly LightEnvironment _lights = new();

    /// <summary>
    /// Initialize the 3D rendering system. Must be called after Graphics is initialized
    /// (e.g., during LoadAssets).
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        var flags = ShaderFlags.Depth | ShaderFlags.DepthLess;

        // Unlit shader (vertex color only, no textures)
        _unlitShader = CreateShaderFromEmbedded(
            "YesZ.Rendering.Shaders.unlit3d.wgsl",
            "unlit3d",
            flags,
            new List<ShaderBinding>
            {
                new() { Binding = 0, Type = ShaderBindingType.UniformBuffer, Name = "globals" },
            });

        // Textured shader (texture × vertex color × material color factor)
        _texturedShader = CreateShaderFromEmbedded(
            "YesZ.Rendering.Shaders.textured3d.wgsl",
            "textured3d",
            flags,
            new List<ShaderBinding>
            {
                new() { Binding = 0, Type = ShaderBindingType.UniformBuffer, Name = "globals" },
                new() { Binding = 1, Type = ShaderBindingType.UniformBuffer, Name = "material" },
                new() { Binding = 2, Type = ShaderBindingType.Texture2D, Name = "base_color_texture" },
                new() { Binding = 3, Type = ShaderBindingType.Sampler, Name = "base_color_sampler" },
            });

        // Default 1×1 white texture — untextured materials sample this (identity multiply)
        var white = new byte[] { 255, 255, 255, 255 };
        _defaultWhiteTexture = Graphics.Driver.CreateTexture(1, 1, white, TextureFormat.RGBA8, TextureFilter.Point, "DefaultWhite");

        _initialized = true;
    }

    /// <summary>
    /// Create a new Material3D using the textured shader and default white texture.
    /// </summary>
    public static Material3D CreateMaterial()
    {
        if (!_initialized)
            Initialize();

        return new Material3D(_texturedShader!.Handle, _defaultWhiteTexture);
    }

    /// <summary>
    /// Set the active material for subsequent DrawMesh calls.
    /// Binds the material's shader, texture, and uploads uniform data.
    /// Pass null to revert to the unlit (vertex-color-only) shader.
    /// </summary>
    public static void SetMaterial(Material3D? material)
    {
        _currentMaterial = material;

        if (material == null) return;

        // Upload material uniform data
        var uniforms = new MaterialUniforms
        {
            BaseColorFactor = material.BaseColorFactor,
            Metallic = material.Metallic,
            Roughness = material.Roughness,
        };
        Graphics.SetUniform("material", in uniforms);
    }

    /// <summary>
    /// Set the directional light for the current frame.
    /// </summary>
    public static void SetDirectionalLight(in DirectionalLight light)
    {
        _lights.Directional = light;
    }

    /// <summary>
    /// Set the ambient light for the current frame.
    /// </summary>
    public static void SetAmbientLight(in AmbientLight light)
    {
        _lights.Ambient = light;
    }

    /// <summary>
    /// Add a point light for the current frame.
    /// Up to <see cref="LightEnvironment.MaxPointLights"/> can be added per frame.
    /// </summary>
    public static void AddPointLight(in PointLight light)
    {
        _lights.AddPointLight(in light);
    }

    /// <summary>
    /// Read-only access to the current light environment (for testing and diagnostics).
    /// </summary>
    public static LightEnvironment Lights => _lights;

    /// <summary>
    /// Begin 3D rendering pass. Saves the current 2D projection, stores
    /// the camera for per-draw MVP computation, and clears per-frame light state.
    /// </summary>
    public static void Begin(Camera3D camera)
    {
        if (!_initialized)
            Initialize();

        _savedProjection = Graphics.GetPassProjection();
        _camera = camera;
        _viewProjection = camera.ViewProjectionMatrix;
        _lights.ClearPointLights();
    }

    /// <summary>
    /// Draw a 3D mesh with the given world transform.
    /// Computes MVP and issues a draw command through NoZ's batch system.
    /// Uses the current material's shader if set, otherwise falls back to unlit.
    /// </summary>
    public static void DrawMesh(Mesh3D mesh, Matrix4x4 worldMatrix)
    {
        if (_camera == null) return;

        // MVP = model × view × projection (row-major multiplication order)
        var mvp = worldMatrix * _viewProjection;

        // NoZ's UploadGlobals transposes the projection before uploading to the GPU.
        // NoZ's 2D SetCamera constructs matrices pre-transposed for this cycle, but
        // System.Numerics helpers (CreateLookAt, CreatePerspectiveFieldOfView) produce
        // row-vector convention matrices. Pre-transposing here cancels NoZ's transpose,
        // so the GPU receives the correct bytes for WGSL's column-major `M * v`.
        Graphics.SetPassProjection(Matrix4x4.Transpose(mvp));

        // Use material shader if set, otherwise fall back to unlit
        var shader = _currentMaterial != null ? _texturedShader : _unlitShader;
        if (shader == null) return;

        Graphics.SetShader(shader);

        // Bind material texture if using textured shader
        if (_currentMaterial != null)
        {
            Graphics.SetTexture(_currentMaterial.BaseColorTexture, 0, TextureFilter.Linear);
        }

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
        _currentMaterial = null;
    }

    private static Shader CreateShaderFromEmbedded(
        string resourceName,
        string shaderName,
        ShaderFlags flags,
        List<ShaderBinding> bindings)
    {
        var wgslSource = LoadEmbeddedResource(resourceName);
        var handle = Graphics.Driver.CreateShader(shaderName, wgslSource, wgslSource, bindings);

        try
        {
            return Shader.CreateRaw(shaderName, handle, flags, bindings, MeshVertex3D.VertexHash);
        }
        catch
        {
            Graphics.Driver.DestroyShader(handle);
            throw;
        }
    }

    private static string LoadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded shader resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
