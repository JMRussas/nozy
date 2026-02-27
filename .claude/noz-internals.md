# NoZ Engine Internals Reference

Quick reference for the NoZ engine types and patterns that YesZ builds on. Use the NoZ RAG (`search_noz` / `lookup_noz`) for full source when needed.

## Game Loop

```
Application.RunFrame()
├── Time.Update()
├── Input.BeginFrame() / PollEvents() / Update()
├── Graphics.BeginFrame()
├── IApplication.Update()         ← game logic + 3D rendering here
├── UI.Begin()
├── IApplication.UpdateUI()       ← 2D UI here
├── UI.End()
├── IApplication.LateUpdate()
├── VfxSystem.Update()
├── Cursor.Update()
└── Graphics.EndFrame()           ← sorts draw commands, batches, submits to GPU
```

Key insight: `Update()` runs before `UI.Begin()`, so Graphics3D.Begin()/End() in Update() can set up 3D state before the UI pass starts.

## IApplication Interface

```csharp
public interface IApplication
{
    void Update();                              // Required — game logic
    void UpdateUI() { }                         // UI rendering
    void LateUpdate() { }                       // Post-UI logic
    void LoadConfig(ApplicationConfig config) { }
    void SaveConfig() { }
    void LoadAssets() { }
    void UnloadAssets() { }
    void ReloadAssets() { }
    bool WantsToQuit() => true;
    void BeforeQuit() { }
}
```

Only `Update()` lacks a default implementation. All others are optional.

## Graphics System

`Graphics` is the static 2D rendering entry point. Key members:

| Member | Purpose |
|--------|---------|
| `Graphics.Driver` | The `IGraphicsDriver` (WebGPU on desktop) |
| `Graphics.ClearColor` | Background color (set per frame) |
| `Graphics.Transform` | Current 2D transform (Matrix3x2) |
| `Graphics.Color` | Current tint color |
| `Graphics.BeginFrame()` / `EndFrame()` | Frame lifecycle (internal) |
| `Graphics.PushState()` / `PopState()` | State stack for shader/texture/blend |
| `Graphics.SetShader()` | Bind a shader for subsequent draws |
| `Graphics.SetTexture()` | Bind a texture |
| `Graphics.SetBlendMode()` | Set blend mode |
| `Graphics.DrawElements()` | Submit index draw command |

### Draw Command Pipeline

Graphics uses deferred rendering:
1. During the frame, `AddTriangles()` / `DrawElements()` record `DrawCommand` structs with sort keys
2. `EndFrame()` sorts all commands by sort key (pass → layer → group → order → index)
3. Commands are batched by identical `BatchState` (shader, textures, blend mode, viewport, scissor)
4. Batches are submitted to `IGraphicsDriver` in order

### Projection

Graphics stores per-pass projection matrices (`_passProjections`). The globals uniform buffer contains:
```csharp
struct GlobalsSnapshot {
    Matrix4x4 Projection;  // Transposed for GPU (column-major)
    float Time;
}
```

For 3D, YesZ will need to inject its own projection matrix into this system.

## IGraphicsDriver Interface

The GPU abstraction layer. Current methods (no depth/cull support):

| Category | Methods |
|----------|---------|
| **Lifecycle** | `Init`, `Shutdown`, `BeginFrame`, `EndFrame` |
| **Viewport** | `SetViewport`, `SetScissor`, `ClearScissor` |
| **Mesh** | `CreateMesh<T>`, `DestroyMesh`, `BindMesh`, `UpdateMesh` |
| **Uniform** | `CreateUniformBuffer`, `DestroyBuffer`, `UpdateUniformBuffer`, `BindUniformBuffer` |
| **Texture** | `CreateTexture`, `UpdateTexture`, `DestroyTexture`, `BindTexture` |
| **Shader** | `CreateShader`, `DestroyShader`, `BindShader` |
| **State** | `SetBlendMode`, `SetTextureFilter` |
| **Globals** | `SetGlobalsCount`, `SetGlobals`, `BindGlobals` |
| **Draw** | `DrawElements` |
| **Passes** | `BeginScenePass`, `ResumeScenePass`, `EndScenePass` |
| **Render Texture** | `CreateRenderTexture`, `BeginRenderTexturePass`, `EndRenderTexturePass` |

**Not present (needed for Phase 1):** `SetDepthTest`, `SetDepthWrite`, `SetCullMode`, `ClearDepth`, `CullMode` enum.

## ShaderFlags

```csharp
[Flags]
public enum ShaderFlags : byte
{
    None = 0,
    Blend = 1 << 0,
    Depth = 1 << 1,          // Exists but NOT wired to pipeline
    DepthLess = 1 << 2,       // Exists but NOT wired to pipeline
    PremultipliedAlpha = 1 << 3,
}
```

`Shader.Load()` reads flags from the binary asset but does **not** pass them to `IGraphicsDriver.CreateShader()`. Phase 1 must thread these through.

## Shader Class

```csharp
public class Shader : Asset
{
    public ShaderFlags Flags { get; }
    public List<ShaderBinding> Bindings { get; }
    public string Source { get; }
}
```

Shaders are loaded from binary assets. `CreateShader(name, vertexSource, fragmentSource, bindings)` — note the same source string is passed for both vertex and fragment; the WebGPU backend splits them.

## UI System

Immediate-mode UI. Key types:

| Type | Purpose |
|------|---------|
| `ContainerStyle` | Layout container (size, color, padding, border, alignment) |
| `LabelStyle` | Text label (font size, color, alignment) |
| `BorderStyle` | Border decoration (radius) |
| `Size2` / `Size` | Dimension specs (`Size.Percent()`, `Size2.Fit`) |
| `EdgeInsets` | Padding/margin (`EdgeInsets.Symmetric(v, h)`) |
| `Align` | Alignment enum (`Center`, `Min`, `Max`) |

Pattern: `using (UI.BeginContainer(style)) { ... }` — the `AutoContainer` disposable handles end.

## Key File Locations

| File | Purpose |
|------|---------|
| `engine/noz/engine/src/Application.cs` | App lifecycle, game loop |
| `engine/noz/engine/src/ApplicationConfig.cs` | `IApplication` interface, config |
| `engine/noz/engine/src/graphics/Graphics.cs` | 2D rendering, batching, state |
| `engine/noz/engine/src/graphics/Shader.cs` | Shader asset, ShaderFlags |
| `engine/noz/engine/src/graphics/BlendMode.cs` | Blend mode enum |
| `engine/noz/engine/src/platform/IGraphicsDriver.cs` | GPU driver interface |
| `engine/noz/engine/src/ui/UI.cs` | Immediate-mode UI |
| `engine/noz/engine/src/ui/ElementStyle.cs` | ContainerStyle, LabelStyle, BorderStyle |
| `engine/noz/platform/webgpu/` | WebGPU backend (not indexed in RAG — read directly) |
