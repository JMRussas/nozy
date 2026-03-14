# YesZ — Upstream Maintenance

## Fork Details

| Field | Value |
|-------|-------|
| Upstream | https://github.com/nozgames/noz-cs |
| Fork | https://github.com/JMRussas/noz-cs |
| License | MIT (NoZ Games, LLC) |
| Repo layout | NoZ code at root (`engine/`, `platform/`, `generators/`); YesZ additions in `src/`, `samples/`, `tests/` |
| Last upstream merge | 2026-03-13 (upstream commit `fa7b55f`) |

## Merge Procedure

### Regular Merge (Monthly or on Notable Upstream Updates)

This repo is **not** a submodule setup — it's a direct fork with YesZ code added alongside NoZ.
The histories are disconnected (different root commits), so `--allow-unrelated-histories` is required.

```bash
git fetch upstream
git merge upstream/main --allow-unrelated-histories
# Resolve conflicts:
#   - CLAUDE.md, LICENSE, README.md → keep ours (YesZ versions)
#   - .gitignore → merge both sides (combine entries)
#   - Engine/platform files → unlikely conflict, but see Fork Changes below
dotnet build yesz.slnx    # Verify build
dotnet test yesz.slnx     # Verify tests
```

### Post-Merge Checks

1. **`Directory.Build.props`** — upstream may change global MSBuild properties. Check that `GenerateAssemblyInfo` isn't disabled globally (breaks `InternalsVisibleTo` in YesZ.Rendering.csproj which overrides it to `true`).
2. **UI API changes** — upstream is actively refactoring the UI system. Check `samples/HelloCube/HelloCubeApp.cs` compiles (it uses `UI.Text`, `ContainerStyle`, `LabelStyle`).
3. **NoZ.csproj** — if upstream changes `AssemblyName` or target framework, YesZ project references may break.

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
| 2026-03-06 | `platform/webgpu/WebGPUGraphicsDriver.cs` | Expanded `BoundDepthTexture` to 4 slots (`BoundDepthTexture0-3`) | Phase 6c: Support 4 cascade depth texture bindings |
| 2026-03-06 | `platform/webgpu/WebGPUGraphicsDriver.Resources.cs` | `BindDepthTextureForSampling` now uses `slot` param via switch to select `BoundDepthTexture0-3` | Phase 6c: Per-slot depth texture binding for cascaded shadows |
| 2026-03-06 | `platform/webgpu/WebGPUGraphicsDriver.State.cs` | Added `depthTextureSlot` counter in `UpdateBindGroupIfNeeded`; `DepthTexture2D` case maps successive bindings to slots 0-3 | Phase 6c: Multi-slot depth texture bind group creation |
| 2026-03-07 | `engine/src/platform/IGraphicsDriver3D.cs` | **New file** — 3D driver interface with 7 methods (depth texture arrays, 3D scene pass, depth-only layer pass) | Refactor: Extract 3D methods from IGraphicsDriver |
| 2026-03-07 | `engine/src/platform/IGraphicsDriver.cs` | **Removed** 5 depth texture default methods | Refactor: Moved to IGraphicsDriver3D |
| 2026-03-07 | `engine/src/graphics/Shader.cs` | Added `DepthTexture2DArray = 6` to `ShaderBindingType`; **removed** `CreateRaw()` | Refactor: Array binding + eliminate Shader.CreateRaw |
| 2026-03-07 | `engine/src/graphics/Graphics.State.cs` | **Removed** `SetPassProjection()`/`GetPassProjection()` | Refactor: Graphics3D no longer uses batch system |
| 2026-03-07 | `engine/src/Constants.cs` | **Removed** `InternalsVisibleTo("YesZ.Rendering")` | Refactor: No longer needs internal access |
| 2026-03-07 | `platform/webgpu/WebGPUGraphicsDriver.cs` | Implements `IGraphicsDriver3D`; `DepthTextureArrayInfo`, `_scenePrepassDone`; replaced `BoundDepthTexture0-3` with `BoundDepthTextureArray` | Refactor: Depth texture arrays + 3D scene prepass |
| 2026-03-07 | `platform/webgpu/WebGPUGraphicsDriver.Resources.cs` | `CreateDepthTextureArray`/`DestroyDepthTextureArray`/`BindDepthTextureArrayForSampling`; removed `BindDepthTextureForSampling` | Refactor: texture_depth_2d_array |
| 2026-03-07 | `platform/webgpu/WebGPUGraphicsDriver.RenderPass.cs` | `BeginScenePass3D`/`EndScenePass3D`/`BeginDepthOnlyPassLayer`; prepass flag in `BeginScenePass` | Refactor: 3D scene prepass + per-layer depth pass |
| 2026-03-07 | `platform/webgpu/WebGPUGraphicsDriver.Shaders.cs` | `DepthTexture2DArray` enum + bind group layout | Refactor: Layout for texture_depth_2d_array |
| 2026-03-07 | `platform/webgpu/WebGPUGraphicsDriver.State.cs` | `DepthTexture2DArray` case; deprecated `DepthTexture2D`; removed `depthTextureSlot` | Refactor: Array-based depth texture binding |

### Phase 1a Note

Phase 1a (depth pipeline): `ShaderFlags.Depth`/`DepthLess` existed upstream but were not wired through to pipeline creation. Fork changes thread them through `ShaderInfo.Flags` → `PsoKey.HasDepthAttachment` → `CreateRenderPipeline()` → `DepthStencilState`, and add depth texture creation/attachment to render passes.

### Phase 2 Note

Phase 2 (uniform snapshots): `Graphics.SetUniform()` stored data in a driver-level dictionary that was NOT restored per-batch during deferred execution. This meant only the last value written was visible to all batches — making per-material uniforms impossible. Fix: mirror the `GlobalsSnapshot` pattern by snapshotting uniform data in `AddBatchState()` and restoring it per-batch in `ExecuteBatches()`. **No driver interface or implementation changes needed** — the fix is entirely in `Graphics.cs` and `Graphics.State.cs`. Intended as an upstream contribution to NoZ.

### IGraphicsDriver3D + Batch Decoupling Note

Major refactor that **reduces** fork surface area. Removes `SetPassProjection`, `GetPassProjection`, `Shader.CreateRaw()`, `InternalsVisibleTo`, and 5 `IGraphicsDriver` default methods. Adds 1 new interface file (`IGraphicsDriver3D.cs`) + 1 enum value (`DepthTexture2DArray`). Graphics3D now uses direct `IGraphicsDriver`/`IGraphicsDriver3D` calls instead of NoZ's batch system (`Graphics.SetShader`, `Graphics.SetUniform`, etc.). Scene rendering uses `BeginScenePass3D`/`EndScenePass3D` with a prepass flag so NoZ's subsequent 2D pass preserves 3D content.

---

## Upstream Merge Log

### 2026-03-13 — Merge upstream `fa7b55f` (~250 commits)

**Upstream range:** `3255fb7..fa7b55f`

**Major upstream changes:**
- UI system overhaul: `ElementTree` replaces `ElementData`, new `WidgetId` system, widget components (`UI.Button`, `UI.Container`, `UI.Toggle`, `UI.Slider`, etc.)
- `UI.Label()` renamed to `UI.Text()`
- `ContainerStyle.Border` replaced with `ContainerStyle.BorderRadius` (direct float, no `BorderStyle` wrapper)
- Editor refactoring: shape editor, gen style editor, asset browser, generation client
- CLI platform added: `NullGraphicsDriver`, `NullPlatform`, `CommandLineApplication`
- `AssetBundle` support, `NativeHashMap` collection
- `WidgetIdGenerator` replaces `ElementIdGenerator`
- `Directory.Build.props` adds `GenerateAssemblyInfo=false` (breaks `InternalsVisibleTo` — see fix below)

**Conflicts resolved:**
- `.gitignore` — merged both sides (added NoZ-specific entries to YesZ's ignore file)
- `CLAUDE.md`, `LICENSE`, `README.md` — kept YesZ versions
- `Graphics.cs` — auto-merged cleanly (upstream added 1 line, fork changes in different region)

**Build fixes required:**
1. **Project path restructuring** — solution and csproj files referenced `engine/noz/...` (submodule paths). Updated to root-level paths (`engine/`, `platform/`, `generators/`) since repo is a direct fork, not a submodule setup.
2. **`GenerateAssemblyInfo=false`** — upstream's `Directory.Build.props` disables assembly info generation globally, which prevents the SDK from emitting `InternalsVisibleTo` attributes from csproj `<InternalsVisibleTo>` items. Fixed by overriding `<GenerateAssemblyInfo>true</GenerateAssemblyInfo>` in `src/YesZ.Rendering/YesZ.Rendering.csproj`.
3. **HelloCube UI API migration** — `UI.Label()` → `UI.Text()`, `Border = new BorderStyle { Radius = 8 }` → `BorderRadius = 8`.

**Verification:** Build succeeded (0 errors, 0 warnings), 214 tests pass (163 core + 51 rendering).

---

## Gotchas & Pitfalls

- NoZ uses `.slnx` (XML solution format), not `.sln` — use `dotnet build yesz.slnx`
- NoZ targets `net10.0` — requires .NET 10 SDK preview
- `ShaderFlags.Depth` and `ShaderFlags.DepthLess` are fully wired through `ShaderInfo.Flags` → `CreateRenderPipeline()` → `DepthStencilState`. Depth write and compare function are derived from these flags automatically.
- WebGPU backend conditionally sets `DepthStencilState` based on `hasDepthAttachment` and shader flags — null for no depth, configured for scene pass
- Depth/cull are pipeline state in WebGPU, not per-draw toggles — no `SetDepthTest()` or `SetCullMode()` methods needed on `IGraphicsDriver`
- NoZ's sprite shader hardcodes `Z = 0.0` in vertex output — 3D shaders use actual Z values
- `ExampleAssets.cs` in NoZ examples is auto-generated by the NoZ Editor — don't depend on it
- **Matrix transpose convention**: NoZ's `UploadGlobals()` transposes the projection matrix before uploading to the GPU. NoZ's 2D `SetCamera` constructs matrices pre-transposed. Graphics3D now bypasses NoZ's batch system entirely — all 3D matrices are uploaded via direct `driver.SetUniform()` with NO transpose (C# row-major bytes map naturally to WGSL column-major). The old `SetPassProjection` path (which needed pre-transpose) has been removed.
- **Depth texture not created at init**: `CreateSwapChain()` sets `_surfaceWidth`/`_surfaceHeight` but `BeginFrame()` only creates the depth texture when size changes. Since the size matches on the first frame, the depth texture was never created, causing "invalid texture view for depth stencil attachment". Fixed by calling `CreateDepthTexture()` at the end of `CreateSwapChain()`.
- **`IGraphicsDriver.SetShaderFlags` is a default interface method** — highest merge conflict risk if upstream adds the same method name. This is the only fork change to a public interface.
- **NoZ built-in assets must be explicitly loaded**: Non-editor apps must call `Asset.Load()` for built-in shaders (sprite, text, ui, texture) and fonts (seguisb) during `LoadAssets()`. The `AssetPath` must point to `editor/library/` where pre-built binary assets live. `Asset.Get()` only does registry lookup — no lazy loading.
- **`Directory.Build.props` has `GenerateAssemblyInfo=false`**: This upstream setting breaks `<InternalsVisibleTo>` in csproj files. Any YesZ project that needs `InternalsVisibleTo` must override with `<GenerateAssemblyInfo>true</GenerateAssemblyInfo>` in its own csproj.
- **NoZ UI API is unstable**: Upstream is actively refactoring the UI system. `UI.Label` → `UI.Text`, `ContainerStyle.Border` → `ContainerStyle.BorderRadius`, etc. Check HelloCube compiles after any upstream merge.
- **Repo is a direct fork, not a submodule**: The `.gitmodules` file references `engine/noz/` but this is a leftover — NoZ code lives at root level. Project references use paths like `..\..\engine\NoZ.csproj`, `..\..\platform\webgpu\NoZ.WebGPU.csproj`.
- **`--allow-unrelated-histories` required for upstream merges**: The fork was bootstrapped as a new repo, so `git merge upstream/main` fails without this flag.
