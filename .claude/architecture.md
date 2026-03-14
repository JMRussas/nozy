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
│  IGraphicsDriver + IGraphicsDriver3D + Shader   │
│  + IVertex + Asset + depth buffer (additive)    │
├─────────────────────────────────────────────────┤
│           WebGPU Backend (NoZ fork)             │
│  + depth texture arrays + 3D scene pass         │
│  + depth-only layer pass + comparison sampler   │
└─────────────────────────────────────────────────┘
```

## Roadmap

See [roadmap.md](roadmap.md) for the full phase plan, milestones, and open design decisions.

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
| `IGraphicsDriver` | engine/src/platform/IGraphicsDriver.cs | GPU abstraction (2D rendering) |
| `IGraphicsDriver3D` | engine/src/platform/IGraphicsDriver3D.cs | 3D GPU abstraction (depth textures, 3D passes) |
| `IVertex` | engine/src/graphics/MeshVertex.cs | Vertex format descriptor interface |
| `Camera` | engine/src/graphics/Camera.cs | 2D camera (Matrix3x2) |
| `SDLPlatform` | platform/desktop/ | SDL3 windowing |
| `WebGPUGraphicsDriver` | platform/webgpu/ | WebGPU rendering (implements both IGraphicsDriver + IGraphicsDriver3D) |
| `UI` | engine/src/ui/UI.cs | Immediate-mode UI (Text, Container, Button, etc.) |

## Render Flow (Current — Phase 6c)

```
Frame Start
├── Graphics3D.Begin(camera3D)
│   └── Store camera, reset light/shadow state
├── Graphics3D.SetDirectionalLight / AddPointLight
├── Graphics3D.RenderShadowPass()          ← enables shadow collection
├── Graphics3D.DrawModel(model, world)     ← per object
│   ├── Compute MVP, normal matrix
│   ├── Upload uniforms via direct driver.SetUniform()
│   └── Collect shadow casters
├── Graphics3D.End()
│   ├── Execute shadow depth passes (per cascade)
│   │   └── BeginDepthOnlyPassLayer → draw casters → EndDepthOnlyPass
│   ├── BeginScenePass3D (with prepass flag)
│   ├── Draw lit meshes with shadow sampling
│   └── EndScenePass3D → preserves depth for 2D overlay
├── [Game draws 2D UI via NoZ UI.Text/UI.BeginContainer]
└── Frame End
```

### Key Design: Direct Driver Calls (Post-Refactor)

Graphics3D bypasses NoZ's 2D batch system entirely. All 3D rendering uses direct
`IGraphicsDriver` / `IGraphicsDriver3D` calls. Matrices are uploaded via `driver.SetUniform()`
with NO transpose (C# row-major bytes map naturally to WGSL column-major).

Globals snapshots are still used for per-transform data (max 64 per frame).
Shadow maps use `IGraphicsDriver3D.CreateDepthTextureArray` for cascaded shadows.
