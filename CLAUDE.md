# YesZ

3D extension of the NoZ game engine. Adds perspective cameras, 3D mesh rendering, glTF model loading, PBR materials, and lighting to NoZ's 2D foundation.

## Build & Run

```bash
dotnet build yesz.slnx          # Build all projects
dotnet test yesz.slnx           # Run all tests (214 currently)
dotnet run --project samples/HelloCube/HelloCube.csproj   # Run sample
```

## Project Structure

```
yesz/                            Fork of nozgames/noz-cs (JMRussas/noz-cs)
  engine/                        NoZ engine core (NoZ.Engine.dll)
    src/
      graphics/                  Graphics, Shader, batch system
      platform/                  IGraphicsDriver, IGraphicsDriver3D
      ui/                        Immediate-mode UI (ElementTree, widgets)
  platform/
    desktop/                     SDL3 windowing (NoZ.Desktop)
    webgpu/                      WebGPU rendering backend (NoZ.WebGPU)
    cli/                         CLI platform with null drivers
  generators/                    Source generators (WidgetId)
  editor/                        NoZ editor (not used by YesZ directly)
  src/
    YesZ.Core/                   3D math, transforms, camera (no rendering dependency)
    YesZ.Rendering/              3D render pipeline, Graphics3D, materials, lighting
    YesZ.Desktop/                Desktop launcher (wraps NoZ SDLPlatform + WebGPU)
  samples/
    HelloCube/                   Minimal sample — spinning cube + shadows + animation
  tests/
    YesZ.Core.Tests/             Transform3D, Camera3D, glTF tests (xUnit, 163 tests)
    YesZ.Rendering.Tests/        Rendering pipeline tests (xUnit, 51 tests)
  yesz.slnx                     Solution file (XML format)
```

## Deep-Dive Docs

| Doc | When to read |
|-----|-------------|
| [.claude/architecture.md](.claude/architecture.md) | System architecture, layer diagram, NoZ integration |
| [.claude/roadmap.md](.claude/roadmap.md) | Phase plan, milestones, open design decisions |
| [.claude/maintenance.md](.claude/maintenance.md) | Upstream merge procedures, fork change log |
| [.claude/noz-internals.md](.claude/noz-internals.md) | NoZ engine architecture reference |

## Conventions

- **Namespace**: `YesZ` for core, `YesZ.Rendering` for rendering, `YesZ.Desktop` for platform
- **Naming**: PascalCase for classes/types, camelCase for locals, follows NoZ conventions
- **Target**: .NET 10.0, same as NoZ
- **Config**: No hardcoded values — expose via constructor params or config objects
- **3D/2D coexistence**: `Graphics3D.Begin()` / `Graphics3D.End()` brackets 3D rendering; NoZ's 2D UI renders after `End()`
- **Fork changes**: All modifications to NoZ engine code (`engine/`, `platform/`, `generators/`) must be documented in `.claude/maintenance.md`
- **Testing**: xUnit, test pure math/logic without GPU dependencies

## Git Workflow

| Setting | Value |
|---------|-------|
| workflow | pr |
| base_branch | main |
| branch_protection | no |
| ci_gate | required |
| squash_merge | yes |

## Trust Zones

| Directory | Trust Level | Rule |
|-----------|-------------|------|
| `src/YesZ.Core/` | L3 | Never modify without L3 plan + explicit user approval. This is the foundational math, camera, and transform layer shared by all rendering code. Breaking changes here break everything above it. |
| `src/YesZ.Rendering/` | L2 | L2 plan required before any change. This is the render pipeline: Graphics3D, materials, lighting, shader management. Changes here risk visual regressions and GPU resource leaks. |
| `engine/`, `platform/`, `generators/` | L3 | Fork changes require L3 plan + explicit user approval. Every modification must be logged in `.claude/maintenance.md`. Prefer extending over modifying. Never alter existing behavior — additive changes only. |
| `samples/` | L1 | L1 plan acceptable. Sample code demonstrates features; it does not own production logic. |
| `tests/` | Unrestricted | Edit freely. Tests are the safety net — adding or fixing tests is always welcome. |

## Performance Budget

Frame budget target: **16ms (60 FPS)**. Every render-path decision must be evaluated against this budget.

| Constraint | Limit | Notes |
|------------|-------|-------|
| Total frame time | < 16ms | Measured end-to-end on Main (4090) |
| Shadow PCF pass | < 3ms | All cascades combined |
| Depth-only prepass | < 1ms | Per shadow-casting light |
| GPU allocations on hot path | Zero after warmup | No `CreateMesh`, `CreateTexture`, `CreateDepthTexture` in `Update()` or draw calls |
| Shader uploads per draw | 0 N+1 patterns | Batch material uniforms; never loop over objects uploading one uniform at a time |
| Render pass count | Minimized | Minimize `BeginScenePass3D` / `BeginDepthOnlyPassLayer` calls; group shadow casters by cascade |
| Globals snapshot count | ≤ 64 per frame | Hard limit from NoZ's globals buffer. Phase 7+ must revisit if object count exceeds this. |

**Allocation rule:** After the first frame completes (warmup), zero GPU buffer allocations are permitted on the render hot path. If new resources are needed (e.g., dynamic scene growth), pre-allocate a pool at scene load time.

**N+1 shader upload rule:** Never upload per-object uniforms in a loop where each iteration calls `UpdateUniformBuffer`. Instead, use the globals snapshot system (per-transform) or batch material data into a single buffer update.

## Upstream Merge Procedure

This repo is a fork of `nozgames/noz-cs`. NoZ engine code lives at the root level (`engine/`, `platform/`, `generators/`), not in a submodule. YesZ additions (`src/`, `samples/`, `tests/`, `yesz.slnx`) coexist in the same repo.

```bash
# Merge latest upstream NoZ into this repo
git fetch upstream
git merge upstream/main --allow-unrelated-histories
# Resolve conflicts — keep YesZ versions for CLAUDE.md, LICENSE, README.md
# Merge .gitignore (combine both sides)
dotnet build yesz.slnx    # Verify build
dotnet test yesz.slnx     # Verify tests
```

**Key points:**
- Remotes: `origin` = `JMRussas/noz-cs` (fork), `upstream` = `nozgames/noz-cs`
- `--allow-unrelated-histories` is required because the fork was bootstrapped as a new repo
- Fork changes are on `engine/src/platform/`, `engine/src/graphics/`, `platform/webgpu/` — check these for conflicts
- Upstream changes are typically in `editor/`, `engine/src/ui/`, docs — usually non-overlapping
- After merge, check `Directory.Build.props` for settings that break YesZ (e.g., `GenerateAssemblyInfo=false` breaks `InternalsVisibleTo`)
- See `.claude/maintenance.md` for full conflict prediction list and failure modes
