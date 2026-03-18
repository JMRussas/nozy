# YesZ Testing Policy

## Test-Before-Implement Policy

**For every phase that adds non-trivial logic, write the failing unit test first, then implement.**

This is not optional. The test is the behavioral specification. Implementation is correct when the test passes for the right reason — not merely when the code compiles.

### Why this matters for YesZ

YesZ has two categories of code: GPU-dependent (render pipeline, shaders, draw calls) and GPU-independent (math, parsers, data structures, animation). GPU-dependent code cannot be unit-tested without a real device. GPU-independent code — which includes all of `YesZ.Core` and significant parts of the parsers — is 100% testable.

The test-before-implement gate applies to the GPU-independent category. GPU-dependent code is verified via HelloCube visual regression checks documented in each phase's roadmap entry.

### Gate rule

No `<approach>` item in a phase plan may begin implementation until:
1. The unit test for that item is written
2. The test is failing **for the right reason** — the assertion fails because the feature is not yet implemented, not because of a compile error or import problem

"Failing for the right reason" requires a comment in the plan: `// Fails: method not yet implemented` or `// Fails: returns 0 instead of expected 42`.

---

## Current Test Baseline

As of Phase 6c (complete): **9 passing tests**

```
dotnet test yesz.slnx
```

| Test class | Count | What's covered |
|-----------|-------|----------------|
| `Transform3DTests` | ~3 | Local/world transform composition, matrix output |
| `Camera3DTests` | ~3 | View-projection construction, FOV, near/far plane mapping |
| `MeshVertex3DTests` | ~3 | Stride, attribute layout, vertex hash differentiation from NoZ 2D |

(Exact breakdown: run `dotnet test yesz.slnx --verbosity normal` to list individual test names.)

---

## Test Categories and Boundary Conditions

### Category 1: Math and Transform Tests (`YesZ.Core.Tests`)

**Scope:** `Transform3D`, `Camera3D`, `MeshVertex3D`, `Mesh3DBuilder`

**Boundary conditions required for each test class:**

| Boundary | Description | Why it matters |
|----------|-------------|----------------|
| Identity transform | `Transform3D` at default position/rotation/scale | Baseline — composition with identity must be identity |
| Zero scale | `Transform3D` with `Scale = Vector3.Zero` | Degenerate matrix; division by zero in decompose |
| NaN input | `Camera3D` with NaN FOV or near/far | WebGPU uploads NaN silently; produces black frames |
| Near == Far | Degenerate frustum | Division by zero in projection matrix formula |
| FOV at extremes | FOV = 0° and FOV = 180° | Tangent blows up at 90°; useful error message needed |
| Behind-camera point | Point at `z < camera near` | Should have `w < 0` or `z > 1` in clip space |
| Negative near plane | `nearPlane < 0` | WebGPU depth range is [0, 1]; negative near is invalid |
| 24 vs 36 count | `Mesh3DBuilder.CreateCube()` | 24 vertices (4/face), 36 indices (6/face) is exact spec |
| Winding order | CCW from outside for all 6 cube faces | Back-face culling will hide forward-facing triangles if wrong |
| Normal unit length | All `MeshVertex3D.Normal` in cube | Denormalized normals corrupt Blinn-Phong lighting |

### Category 2: glTF Parser Tests (`YesZ.Core.Tests` or `YesZ.Rendering.Tests`)

**Scope:** `GlbReader`, glTF buffer/accessor/mesh extraction

**Boundary conditions required for each test class:**

| Boundary | Description | Why it matters |
|----------|-------------|----------------|
| Empty .glb | 12-byte header only, no JSON chunk | Should throw `GlbFormatException`, not index out of range |
| Header magic wrong | First 4 bytes != `0x46546C67` | Detect non-glTF files early with a clear error |
| JSON chunk length = 0 | Valid header, zero-length JSON | Edge case in chunk iterator |
| No BIN chunk | JSON-only .glb (no binary buffer) | Valid for embedded-data-URI models; BIN is optional |
| Multiple primitives | Mesh with 3+ primitives | Multi-primitive extraction must produce multiple `Mesh3D` |
| Missing accessor | Accessor index in primitive references nonexistent accessor | Should throw, not silently return empty mesh |
| Sparse accessor | glTF sparse accessor (compressed deltas) | Not supported in Phase 4a; must throw `NotSupportedException` |
| Skinned with no weights | Mesh has joints attribute but no weights | Must treat as equal weights or throw — not produce NaN |
| Zero-joint skeleton | `skin.joints` is empty array | Bone matrix upload loop must not execute |
| Joint count > 256 | Skeleton exceeds max supported joints | Must assert/throw at load time, not corrupt silently |

### Category 3: Animation Tests (`YesZ.Core.Tests`)

**Scope:** `AnimationPlayer3D`, `Skeleton`, keyframe sampling

**Boundary conditions required for each test class:**

| Boundary | Description | Why it matters |
|----------|-------------|----------------|
| Time = 0 | Sample at animation start | Must return first keyframe exactly, not interpolate toward it |
| Time = duration | Sample at exact animation end | Must return last keyframe, not extrapolate beyond |
| Time > duration (loop) | Sample past end with loop enabled | Must wrap via `time % duration`, not clamp or throw |
| Time > duration (clamp) | Sample past end with loop disabled | Must clamp to last keyframe |
| Single keyframe | Animation with exactly 1 keyframe per joint | No interpolation needed; must return the single value |
| Missing joint channel | A joint has no animation channel in a clip | Must return bind-pose matrix for that joint |
| Non-uniform keyframe spacing | Uneven time intervals between keyframes | Lerp parameter `t` must be computed per-interval, not globally |
| Quaternion shortest-path | Two keyframes with quaternions on opposite hemisphere | Must use `Quaternion.Slerp` with dot-product sign check to avoid 360° spin |
| Root motion extraction | Root joint has translation channel | Root motion is applied to `Transform3D`, not baked into bone matrix |
| Simultaneous playback | Two `AnimationPlayer3D` instances on same skeleton | State must not be shared (each player holds its own time and weights) |

### Category 4: Shadow / Lighting Tests (`YesZ.Core.Tests`)

**Scope:** Light types, shadow frustum computation, cascade split math

**Boundary conditions required for each test class:**

| Boundary | Description | Why it matters |
|----------|-------------|----------------|
| Zero lights | Scene with no lights | Must render as ambient-only, not throw |
| Point light at origin | `PointLight` at `Vector3.Zero` | Distance = 0 → attenuation formula divides by zero |
| Directional light perpendicular to view | Shadow frustum ortho projection edge case | Degenerate frustum if light direction == camera forward |
| Cascade split at max distance | Split distance = camera far plane | Last cascade must cover to exactly far plane |
| Shadow bias = 0 | No depth bias applied | Every surface self-shadows (acne); must show test output as known bad |
| PCF kernel = 1×1 | Minimal PCF sampling | Valid; shadow is binary (aliased but correct) |
| Texel-perfect shadow map | Object exactly at shadow map resolution boundary | Shadow edge must not flicker between frames |

---

## Test Naming Convention

```
{ClassName}Tests.{MethodOrProperty}_{Scenario}_{Expected}
```

Examples:
- `Camera3DTests.ProjectionMatrix_ZeroFOV_ThrowsArgumentException`
- `GlbReaderTests.Parse_NoBinChunk_ReturnsMeshWithEmbeddedUri`
- `AnimationPlayer3DTests.Sample_TimeExceedsDurationWithLoop_WrapsCorrectly`
- `Mesh3DBuilderTests.CreateCube_AllNormals_AreUnitLength`

---

## Upcoming Phase Test Requirements

### Phase 7a — Render-to-Texture

**Test-first items** (GPU-independent logic only):

| Test | Failing reason before impl |
|------|---------------------------|
| `RenderTextureTests.Descriptor_NegativeWidth_ThrowsArgumentException` | Constructor not yet validated |
| `RenderTextureTests.Descriptor_WidthLargerThanMaxWebGPU_ThrowsArgumentException` | Limit check not yet enforced |
| `ToneMapperTests.Reinhard_ZeroLuminance_ReturnsBlack` | Math not yet implemented |
| `ToneMapperTests.Reinhard_MaxFloat_DoesNotReturnNaN` | Clamp not yet applied |

**Boundary conditions for 7a:**
```xml
<boundary_conditions>
  - RenderTexture width/height = 0: must throw ArgumentException (not crash GPU)
  - RenderTexture larger than swap chain: valid (offscreen); driver must accept
  - Blit to screen with null source texture: must throw before any GPU call
  - Two simultaneous render texture passes: second Begin before first End → throw or undefined?
  - Resize during render-to-texture pass: must not reallocate mid-pass
</boundary_conditions>
```

### Phase 8a — Scene Graph

**Test-first items:**

| Test | Failing reason before impl |
|------|---------------------------|
| `SceneNodeTests.AddChild_NullChild_ThrowsArgumentNullException` | Null guard not yet written |
| `SceneNodeTests.WorldTransform_NestedThreeLevels_ComposesCorrectly` | WorldTransform not yet implemented |
| `SceneNodeTests.RemoveChild_NotAttached_ThrowsInvalidOperationException` | Orphan check not yet written |
| `SceneNodeTests.SetParent_CircularReference_ThrowsInvalidOperationException` | Cycle detection not yet written |

**Boundary conditions for 8a:**
```xml
<boundary_conditions>
  - Node added to itself as child: circular reference → must throw
  - Node reparented while dirty flag pending: world transform must re-evaluate
  - Node removed during traversal: iterator invalidation → document safe pattern
  - Empty scene (root only): frustum cull must return empty, not throw
  - 10,000+ nodes: world transform update must not be O(n²)
</boundary_conditions>
```

---

## Running Tests

```bash
# Run all tests
dotnet test yesz.slnx

# Run with names listed (to verify baseline count)
dotnet test yesz.slnx --verbosity normal

# Run a specific test class
dotnet test yesz.slnx --filter "ClassName=Camera3DTests"

# Run and capture output (useful after a phase completes)
dotnet test yesz.slnx --verbosity normal 2>&1 | tail -20
```

After each phase, run `dotnet test yesz.slnx` and confirm:
1. The test count increased (new tests added)
2. All tests pass (no regressions)
3. Update the baseline count in this doc under "Current Test Baseline"
