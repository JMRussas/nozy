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
| `IGraphicsDriver` | engine/src/platform/IGraphicsDriver.cs | GPU abstraction (our extension point) |
| `IVertex` | engine/src/graphics/MeshVertex.cs | Vertex format descriptor interface |
| `Camera` | engine/src/graphics/Camera.cs | 2D camera (Matrix3x2) |
| `SDLPlatform` | platform/desktop/ | SDL3 windowing |
| `WebGPUGraphicsDriver` | platform/webgpu/ | WebGPU rendering |

## Render Flow (Current — Phase 1b)

```
Frame Start
├── Graphics3D.Begin(camera3D)
│   ├── Save current 2D pass projection
│   └── Store camera reference for MVP computation
├── Graphics3D.DrawMesh(mesh, worldMatrix)    ← per object
│   ├── Compute MVP = worldMatrix × camera.ViewProjectionMatrix
│   ├── SetPassProjection(MVP) → creates globals snapshot
│   ├── SetShader(unlit3d) + SetMesh(mesh)
│   └── DrawElements() → records batch command
├── Graphics3D.End()
│   └── Restore 2D pass projection
├── [Game draws 2D UI via NoZ Graphics/UI]
└── Frame End
    ├── Sort + batch all draw commands
    ├── 3D draws: depth test (Less), depth write → back faces hidden
    └── 2D draws: depth test (Always), no depth write → renders on top
```

### Key Design: MVP in Globals

3D uses MVP (model × view × projection) stored in the globals system as the "projection"
matrix. Each DrawMesh creates a unique globals snapshot, producing a unique batch state.
This integrates with NoZ's batch system without per-draw uniform buffer tracking.

Limitation: max 64 unique transforms per frame (globals buffer limit). Phase 2+ will
revisit when lighting needs separate world-space positions.
