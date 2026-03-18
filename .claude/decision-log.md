# YesZ Decision Log

Rejected approaches and the reasoning behind each switch. Query this before refactoring any module — it prevents re-introducing patterns that were already tried and discarded.

**Before any refactor:** Ask "What decisions were made about this component? What was tried and rejected?" and read the relevant entries below.

---

## DL-001 — Per-Draw MVP Approach Replaced by Globals Snapshot

**Date:** 2026-02-28 (Phase 1b planning)
**Component:** `Graphics3D.DrawMesh`, transform-to-GPU pipeline
**Status:** Rejected — replaced by globals snapshot pattern

### What was proposed

Upload the MVP (model × view × projection) matrix to a per-draw uniform buffer (slot 1) before each `DrawElements` call. Each `DrawMesh` invocation would:
1. Compute `MVP = worldMatrix × camera.ViewProjectionMatrix`
2. Call `Graphics.Driver.UpdateUniformBuffer(_mvpUbo, AsBytes(mvp))`
3. Call `Graphics.Driver.BindUniformBuffer(_mvpUbo, slot: 1)`
4. Call `Graphics.DrawElements(indexCount, 0, order: 0)`

The shader would read the MVP directly from binding 1:
```wgsl
@group(0) @binding(1) var<uniform> mvp: mat4x4f;
@vertex fn vs_main(@location(0) pos: vec3f) -> @builtin(position) vec4f {
    return mvp * vec4f(pos, 1.0);
}
```

### Why it was rejected

**NoZ's render system is deferred.** `DrawElements` records a `DrawCommand` struct with a snapshot of the current `BatchState`. It does NOT execute the draw immediately. The actual GPU submission happens in `Graphics.EndFrame()` after all draw commands are sorted and batched.

By the time `EndFrame()` executes, all `DrawMesh` calls in the frame have completed. The per-draw UBO holds only the last value written by the last `DrawMesh` call. Every draw command in the batch uses that same final matrix — all objects transform identically to the last object drawn.

This is not a workaround-able timing issue. It is a fundamental property of NoZ's deferred batch architecture.

**Additionally:** `BindUniformBuffer` changes `BatchState`. Calling it N times per frame with the same buffer handle but different contents does NOT create N different batch states — the handle is the same. The batch system sees one UBO binding and does not snapshot the buffer's contents.

### Decision made

Use the **globals snapshot system**, which NoZ's batch system was designed to support. `GetOrAddGlobals(in Matrix4x4 projection)` de-duplicates matrices and assigns each a slot index. `DrawElements` records the current globals slot index in the `DrawCommand`. During batch execution, `RestoreGlobalsSnapshot(index)` re-uploads the correct matrix before each draw.

For Phase 1b: bake `MVP = worldMatrix × viewProjection` into the globals matrix. Each unique object transform gets a unique globals slot. Max 64 unique transforms per frame (globals buffer limit).

For Phase 2+: the same mechanism applies. `MaterialUniforms` (colors, metallic, roughness) use NoZ's uniform snapshot system — a parallel mechanism that snapshots `SetUniform` state per batch.

**Do not revisit** the per-draw UBO approach unless NoZ's batch architecture changes to support immediate-mode execution.

---

## DL-002 — Batch System Integration Replaced by Direct Driver Calls

**Date:** 2026-03-07 (Phase 6c refactor)
**Component:** `Graphics3D`, `IGraphicsDriver` extension strategy
**Status:** Rejected — replaced by direct `IGraphicsDriver3D` calls

### What was proposed

Continue routing all 3D rendering through NoZ's `Graphics` batch system (`Graphics.SetShader`, `Graphics.SetUniform`, `Graphics.DrawElements`). This was the Phase 1b–2 approach: YesZ used `InternalsVisibleTo("YesZ.Rendering")` to access internal `Graphics` methods, injected 3D projection via `SetPassProjection`, and created shaders via `Shader.CreateRaw()`.

The appeal: 3D draws participate in NoZ's sort/batch pipeline alongside 2D draws. A single `EndFrame()` call processes everything.

### Why it was rejected

**Fork surface area grew unsustainably.** To extend NoZ's batch system for 3D, YesZ needed:
- `InternalsVisibleTo("YesZ.Rendering")` in `Constants.cs`
- `SetPassProjection(in Matrix4x4)` / `GetPassProjection()` on `Graphics.State.cs`
- `Shader.CreateRaw()` factory on `Shader.cs`
- 5 default interface methods on `IGraphicsDriver`

Each of these is a fork modification to a file that upstream NoZ evolves actively. Every monthly merge risked conflicts in 4 separate files just to keep the batch-system integration working.

**The batch system's deferred architecture was fighting 3D rendering.** Phase 2's uniform snapshot patch (`DL-001`) was itself a symptom: the batch system was not designed for per-object GPU state. Extending it further for shadow maps (Phase 6), depth texture arrays (Phase 6c), and scene prepasses would have required increasingly invasive changes to `Graphics.cs` internals.

**Sorting 3D and 2D in the same queue was unnecessary.** 3D content renders first (before UI), always. There is no need to interleave 3D and 2D draw commands in a shared sort queue. The separation is semantic: all 3D draws happen in `Update()`, all 2D draws happen in `UpdateUI()`.

### Decision made

Extract all 3D-specific GPU operations behind `IGraphicsDriver3D`, a new interface implemented by `WebGPUGraphicsDriver` alongside `IGraphicsDriver`. `Graphics3D` calls `IGraphicsDriver3D` methods directly — bypassing NoZ's batch system entirely for 3D content.

This reduced fork surface area: removed `SetPassProjection`, `GetPassProjection`, `Shader.CreateRaw()`, `InternalsVisibleTo`, and 5 `IGraphicsDriver` default methods. Added 1 new interface file (`IGraphicsDriver3D.cs`) and 1 enum value (`DepthTexture2DArray`).

The transition point: `BeginScenePass3D` / `EndScenePass3D` handle the prepass flag so NoZ's subsequent 2D pass (`BeginScenePass`) preserves 3D content in the color buffer without clearing it.

**Do not revisit** batch-system integration for 3D rendering. The clean separation (3D via `IGraphicsDriver3D`, 2D via `Graphics`) is the stable architecture going forward.

---

## DL-003 — Monolithic Shader Replaced by Modular ShaderFlags Variants

**Date:** 2026-02-28 (Phase 1b) / 2026-03-06 (Phase 3b, 6b)
**Component:** Shader system, YesZ.Rendering shader loading
**Status:** Rejected — replaced by per-feature ShaderFlags + WGSL variant files

### What was proposed

Write a single "uber shader" covering all 3D surface types — unlit, lit, skinned, shadow-casting — with `#define`-style preprocessor conditionals. One WGSL file, one loaded shader handle, runtime flag parameters to activate code paths.

```wgsl
// Proposed uber shader (rejected)
@vertex fn vs_main(in: VertexInput) -> VertexOutput {
    var pos = in.position;
    #if SKINNED
    pos = apply_skinning(pos, in.bone_indices, in.bone_weights);
    #endif
    // ... rest of vertex shader
}
```

### Why it was rejected

**WGSL has no preprocessor.** The WebGPU Shading Language is compiled from source by the browser's GPU process at pipeline creation time. There is no `#define`, `#ifdef`, or macro system. A "conditional" uber shader would require runtime string manipulation to splice WGSL fragments — producing different source strings per variant — which is effectively the same as maintaining separate shader files, but harder to read and test.

**Vertex layout rigidity.** `MeshVertex3D` (4 attributes, 48 bytes) and `SkinnedMeshVertex3D` (6 attributes, 64 bytes) have different bind group layouts. A single shader cannot declare both `@location(4) bone_indices` and not declare it — the pipeline creation API rejects attribute declarations that do not match the bound vertex buffer's format descriptor.

**Pipeline cache separation is free.** NoZ's `VertexFormatHash` naturally assigns different pipeline cache keys to different vertex formats. Lit vs. unlit pipelines differ only in their `ShaderFlags` (`ShaderFlags.Lit`), which also participates in `PsoKey`. There is no performance cost to having separate shader files — the driver creates and caches one pipeline per unique `(vertexHash, shaderFlags)` pair.

**Shadow pass requires fragment = null.** The depth-only shadow pass pipeline has no fragment shader — `Fragment = null` in `CreateRenderPipeline`. An uber shader cannot suppress its own fragment stage at runtime.

### Decision made

One WGSL file per logical variant, loaded as embedded resources from `src/YesZ.Rendering/Shaders/`:

| File | ShaderFlags | Vertex type | Use case |
|------|-------------|-------------|----------|
| `unlit3d.wgsl` | `Depth \| DepthLess` | MeshVertex3D | Unlit opaque meshes |
| `lit3d.wgsl` | `Depth \| DepthLess \| Lit` | MeshVertex3D | Lit + shadowed meshes |
| `skinned_lit3d.wgsl` | `Depth \| DepthLess \| Lit \| Skinned` | SkinnedMeshVertex3D | Animated characters |
| `depth_only.wgsl` | `Depth \| DepthLess` (IsDepthOnly=true) | MeshVertex3D | Shadow map prepass |
| `depth_only_skinned.wgsl` | `Depth \| DepthLess` (IsDepthOnly=true) | SkinnedMeshVertex3D | Skinned shadow prepass |

`ShaderFlags.Lit` and `ShaderFlags.Skinned` were added to the `ShaderFlags` enum in the NoZ fork (`Shader.cs`) as Phase 3b and 5b fork changes respectively.

**Do not revisit** the uber shader approach. Separate files with `ShaderFlags` differentiation is the correct architecture for WebGPU's shader compilation model.
