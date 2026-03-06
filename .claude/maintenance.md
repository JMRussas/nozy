# YesZ — Upstream Maintenance

## Fork Details

| Field | Value |
|-------|-------|
| Upstream | https://github.com/nozgames/noz-cs |
| Fork | https://github.com/JMRussas/noz-cs |
| License | MIT (NoZ Games, LLC) |
| Submodule path | engine/noz/ |

## Merge Procedure

### Regular Merge (Monthly or on Notable Upstream Updates)

```bash
cd engine/noz
git fetch upstream
git checkout main
git merge upstream/main
# Resolve conflicts (see Fork Changes below for likely conflict points)
git push origin main
cd ../..
git add engine/noz
git commit -m "chore: merge upstream NoZ changes"
dotnet build yesz.slnx    # Verify build
dotnet test yesz.slnx     # Verify tests
```

### Conflict Minimization Strategy

1. **Additive only** — new methods on interfaces, not modified methods
2. **Extension methods** — for non-interface additions, use C# extension methods
3. **Separate files** — if NoZ uses partial classes, put extensions in new files
4. **Document every change** — log below with rationale

### Breaking Change Response

If Bryan makes a breaking change to IGraphicsDriver or other extended types:
1. Pull the change into the fork
2. Update YesZ's usage of the changed interface
3. Verify all YesZ tests pass
4. Document the migration below

---

## Fork Changes Log

Track every modification to NoZ source here. This is the conflict prediction list.

| Date | File | Change | Rationale |
|------|------|--------|-----------|
| — | (none yet) | Phase 0: no fork changes | Submodule added as-is |
| 2026-02-28 | `engine/src/Constants.cs` | Added `[assembly: InternalsVisibleTo("YesZ.Rendering")]` | Phase 1b: Allow YesZ.Rendering to access internal Graphics/Shader APIs |
| 2026-02-28 | `engine/src/graphics/Graphics.State.cs` | Added `SetPassProjection(in Matrix4x4)` and `GetPassProjection()` internal methods | Phase 1b: Inject 3D perspective projection into batch system |
| 2026-02-28 | `engine/src/graphics/Shader.cs` | Added `Shader.CreateRaw()` internal factory method; added `SetShaderFlags` call in existing `Load()` factory | Phase 1b: Create Shader objects from WGSL source; ensure all loaded shaders propagate flags to driver |
| 2026-02-28 | `engine/src/platform/IGraphicsDriver.cs` | Added `SetShaderFlags(nuint, ShaderFlags)` default interface method | Phase 1b: Allow runtime depth/blend flag configuration per shader |
| 2026-02-28 | `platform/webgpu/WebGPUGraphicsDriver.cs` | Added `CreateDepthTexture()`, depth buffer fields, `SetShaderFlags()`, `ShaderFlags` in `ShaderInfo`, `HasDepthAttachment` in `PsoKey`/state | Phase 1a/1b: Depth buffer lifecycle, shader flag storage, pipeline key differentiation |
| 2026-02-28 | `platform/webgpu/WebGPUGraphicsDriver.RenderPass.cs` | Added depth attachment to `BeginScenePass`/`ResumeScenePass`, set `HasDepthAttachment` state | Phase 1a: Wire depth buffer through render passes |
| 2026-02-28 | `platform/webgpu/WebGPUGraphicsDriver.Shaders.cs` | Added `DepthStencilState` to `CreateRenderPipeline`, `HasDepthAttachment` to `PsoKey` | Phase 1a: Depth/stencil pipeline state derived from ShaderFlags |

| 2026-02-28 | `engine/src/graphics/Graphics.State.cs` | `SetUniform()` now keeps copy in `_currentUniforms` dictionary for per-batch snapshotting | Phase 2: Per-batch uniform snapshots — fix `SetUniform` being global state |
| 2026-02-28 | `engine/src/graphics/Graphics.cs` | Added `UniformSnapshotIndex` to `BatchState`, `GetOrAddUniformSnapshot()`, `UniformSnapshotMatches()`, `RestoreUniformSnapshot()`, snapshot clear in frame reset | Phase 2: Per-batch uniform snapshots — mirrors `GlobalsSnapshot` pattern |
| 2026-03-06 | `engine/src/platform/IGraphicsDriver.cs` | Added 5 default interface methods: `CreateDepthTexture`, `DestroyDepthTexture`, `BeginDepthOnlyPass`, `EndDepthOnlyPass`, `BindDepthTextureForSampling` | Phase 6a: Shadow map depth texture and depth-only pass infrastructure |
| 2026-03-06 | `platform/webgpu/WebGPUGraphicsDriver.cs` | Added `IsDepthOnly` to `CachedState` and `PsoKey`; added `DepthTextureInfo` struct and `_depthTextures` array | Phase 6a: Depth-only pipeline state differentiation and standalone depth texture tracking |
| 2026-03-06 | `platform/webgpu/WebGPUGraphicsDriver.Resources.cs` | Implemented `CreateDepthTexture`/`DestroyDepthTexture` with Depth24Plus + RenderAttachment/TextureBinding, separate render/sample views, comparison sampler | Phase 6a: Shadow map depth texture with PCF-ready comparison sampler |
| 2026-03-06 | `platform/webgpu/WebGPUGraphicsDriver.RenderPass.cs` | Added `BeginDepthOnlyPass`/`EndDepthOnlyPass` — no color attachment, depth-only with Clear/Store | Phase 6a: Depth-only render pass variant for shadow mapping |
| 2026-03-06 | `platform/webgpu/WebGPUGraphicsDriver.Shaders.cs` | Extended `CreateRenderPipeline` with `isDepthOnly` param — `Fragment=null`, no color targets, forced depth write + Less compare; `GetOrCreatePipeline` passes `IsDepthOnly` to PsoKey | Phase 6a: Depth-only pipeline without fragment shader |
| 2026-03-06 | `engine/src/graphics/Shader.cs` | Added `DepthTexture2D = 5` to `ShaderBindingType` enum | Phase 6b: Bind group layout support for `texture_depth_2d` WGSL bindings |
| 2026-03-06 | `platform/webgpu/WebGPUGraphicsDriver.Shaders.cs` | Added `DepthTexture2D` to internal `BindingType` enum; added mapping and bind group layout entry with `TextureSampleType.Depth` | Phase 6b: Depth texture bind group layout for shadow map sampling |
| 2026-03-06 | `platform/webgpu/WebGPUGraphicsDriver.State.cs` | Added `DepthTexture2D` case in `UpdateBindGroupIfNeeded` — reads from `_depthTextures[]` via `BoundDepthTexture` | Phase 6b: Bind depth texture sample views in bind group entries |
| 2026-03-06 | `platform/webgpu/WebGPUGraphicsDriver.Resources.cs` | Implemented `BindDepthTextureForSampling` — sets `BoundDepthTexture` state and marks bind group dirty | Phase 6b: Wire depth texture binding for shader sampling |
| 2026-03-06 | `platform/webgpu/WebGPUGraphicsDriver.cs` | Added `BoundDepthTexture` field to `CachedState` struct | Phase 6b: Track bound depth texture for bind group creation |

### Phase 1a Note

Phase 1a (depth pipeline): `ShaderFlags.Depth`/`DepthLess` existed upstream but were not wired through to pipeline creation. Fork changes thread them through `ShaderInfo.Flags` → `PsoKey.HasDepthAttachment` → `CreateRenderPipeline()` → `DepthStencilState`, and add depth texture creation/attachment to render passes.

### Phase 2 Note

Phase 2 (uniform snapshots): `Graphics.SetUniform()` stored data in a driver-level dictionary that was NOT restored per-batch during deferred execution. This meant only the last value written was visible to all batches — making per-material uniforms impossible. Fix: mirror the `GlobalsSnapshot` pattern by snapshotting uniform data in `AddBatchState()` and restoring it per-batch in `ExecuteBatches()`. **No driver interface or implementation changes needed** — the fix is entirely in `Graphics.cs` and `Graphics.State.cs`. Intended as an upstream contribution to NoZ.

## Gotchas & Pitfalls

- NoZ uses `.slnx` (XML solution format), not `.sln` — use `dotnet build yesz.slnx`
- NoZ targets `net10.0` — requires .NET 10 SDK preview
- `ShaderFlags.Depth` and `ShaderFlags.DepthLess` are fully wired through `ShaderInfo.Flags` → `CreateRenderPipeline()` → `DepthStencilState`. Depth write and compare function are derived from these flags automatically.
- WebGPU backend conditionally sets `DepthStencilState` based on `hasDepthAttachment` and shader flags — null for no depth, configured for scene pass
- Depth/cull are pipeline state in WebGPU, not per-draw toggles — no `SetDepthTest()` or `SetCullMode()` methods needed on `IGraphicsDriver`
- NoZ's sprite shader hardcodes `Z = 0.0` in vertex output — 3D shaders use actual Z values
- `ExampleAssets.cs` in NoZ examples is auto-generated by the NoZ Editor — don't depend on it
- **Matrix transpose convention**: NoZ's `UploadGlobals()` transposes the projection matrix before uploading to the GPU. NoZ's 2D `SetCamera` constructs matrices pre-transposed (translation in M14/M24, column convention), so this works out for `M * v` in WGSL. But `System.Numerics` helper functions (`CreateLookAt`, `CreatePerspectiveFieldOfView`) produce row-vector convention matrices (translation in M41/M42/M43). **Any 3D matrix passed to `SetPassProjection` must be pre-transposed** with `Matrix4x4.Transpose()` so that NoZ's transpose cancels it out and the GPU receives correct bytes for WGSL's column-major `M * v`.
- **Depth texture not created at init**: `CreateSwapChain()` sets `_surfaceWidth`/`_surfaceHeight` but `BeginFrame()` only creates the depth texture when size changes. Since the size matches on the first frame, the depth texture was never created, causing "invalid texture view for depth stencil attachment". Fixed by calling `CreateDepthTexture()` at the end of `CreateSwapChain()`.
- **`IGraphicsDriver.SetShaderFlags` is a default interface method** — highest merge conflict risk if upstream adds the same method name. This is the only fork change to a public interface.
- **NoZ built-in assets must be explicitly loaded**: Non-editor apps must call `Asset.Load()` for built-in shaders (sprite, text, ui, texture) and fonts (seguisb) during `LoadAssets()`. The `AssetPath` must point to `engine/noz/editor/library/` where pre-built binary assets live. `Asset.Get()` only does registry lookup — no lazy loading.
