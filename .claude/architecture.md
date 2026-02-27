# YesZ Architecture

## Layer Diagram

```
┌─────────────────────────────────────────────────┐
│                    Game Code                     │
│         (uses YesZ.Core + NoZ for 2D UI)        │
├────────────────────┬────────────────────────────┤
│   NoZ 2D Layer     │      YesZ 3D Layer         │
│  (immediate-mode   │  (scene graph, materials,   │
│   UI, sprites,     │   lighting, model loading,  │
│   text, particles) │   3D camera, transforms)    │
├────────────────────┴────────────────────────────┤
│              NoZ Engine Core (forked)            │
│  IGraphicsDriver + IVertex + Shader + Asset     │
│  + depth buffer + cull mode (additive changes)  │
├─────────────────────────────────────────────────┤
│           WebGPU Backend (NoZ fork)             │
│  + depth texture + 3D pipeline states           │
└─────────────────────────────────────────────────┘
```

## Design Principles

1. **YesZ is parallel to NoZ's 2D, not a replacement.** Both systems share the same IGraphicsDriver and WebGPU context.
2. **Changes to NoZ fork are additive only.** New methods, new enums — never modify existing behavior.
3. **Graphics3D.Begin()/End() brackets 3D rendering.** Enables depth testing, sets perspective projection. End() restores 2D state.
4. **Game loop pattern:** 3D scene first, then NoZ 2D UI overlay.

## Project Dependencies

| Project | Depends On | Role |
|---------|-----------|------|
| YesZ.Core | NoZ.Engine | 3D math, Camera3D, Transform3D, Mesh3D |
| YesZ.Rendering | YesZ.Core, NoZ.Engine | Graphics3D, materials, lighting, shaders |
| YesZ.Desktop | YesZ.Core, YesZ.Rendering, NoZ.Desktop, NoZ.WebGPU | Desktop app launcher |
| HelloCube | All of the above | Sample application |
| YesZ.Core.Tests | YesZ.Core | Unit tests for math/transforms |
| YesZ.Rendering.Tests | YesZ.Rendering | Unit tests for rendering logic |

## NoZ Engine Key Types (Reference)

| Type | Location | Purpose |
|------|----------|---------|
| `IApplication` | engine/src/ApplicationConfig.cs | Game interface: Update(), UpdateUI(), LoadAssets() |
| `Application` | engine/src/Application.cs | Init/Run/Shutdown lifecycle |
| `Graphics` | engine/src/graphics/Graphics.cs | 2D draw calls, state stack, batching |
| `IGraphicsDriver` | engine/src/platform/IGraphicsDriver.cs | GPU abstraction (our extension point) |
| `IVertex` | engine/src/graphics/MeshVertex.cs | Vertex format descriptor interface |
| `Camera` | engine/src/graphics/Camera.cs | 2D camera (Matrix3x2) |
| `SDLPlatform` | platform/desktop/ | SDL3 windowing |
| `WebGPUGraphicsDriver` | platform/webgpu/ | WebGPU rendering |

## Render Flow (Target State)

```
Frame Start
├── Graphics3D.Begin(camera3D)
│   ├── Enable depth test + depth write
│   ├── Set perspective projection uniform
│   ├── Clear depth buffer
│   └── Set cull mode (back-face)
├── [Game draws 3D content via Graphics3D.DrawMesh()]
├── Graphics3D.End()
│   ├── Disable depth test
│   └── Restore 2D orthographic projection
├── [Game draws 2D UI via NoZ Graphics/UI]
└── Frame End (NoZ batches and submits all draw commands)
```
