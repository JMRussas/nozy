# YesZ Anti-Patterns

Named anti-patterns specific to this codebase. Query this before modifying any render-path or fork code. Each entry explains what the pattern looks like, why it fails at runtime, and the correct approach.

---

## antipattern-yesz-001 — Modifying NoZ Fork Behavior Instead of Extending

**Name:** Fork Behavior Override
**Trust zone violated:** engine/noz/ (L3)

### What it looks like

Changing the body of an existing method in `engine/noz/` to add YesZ-specific behavior — for example, altering `Graphics.DrawElements()` to detect 3D draws, or modifying `WebGPUGraphicsDriver.BeginScenePass()` to always attach a shadow texture.

```csharp
// WRONG — modifying existing NoZ method body
public void BeginScenePass(RenderPassDescriptor desc)
{
    // ... existing NoZ code ...
    if (YesZShadowMap != null)  // ← injected condition; breaks upstream mergeability
        desc.DepthStencilAttachment = YesZShadowMap.Attachment;
}
```

### Why it fails

- **Merge conflicts guaranteed.** Upstream NoZ evolves `BeginScenePass` independently. Any body modification produces a three-way conflict on the next merge, even if upstream's change is unrelated.
- **Invisible surface area.** The fork change log in `.claude/maintenance.md` must predict every conflict. Behavior changes inside existing methods are impossible to exhaustively document.
- **Test blindspot.** NoZ's own test suite runs against the unmodified methods. Behavior overrides can pass YesZ tests but break NoZ's assumptions when the fork is later reconciled.

### Correct approach

1. **New methods/interfaces only.** Add `IGraphicsDriver3D` alongside `IGraphicsDriver` — never alter `IGraphicsDriver`. Add `BeginScenePass3D` rather than changing `BeginScenePass`.
2. **Default interface methods for backward compatibility.** When an interface extension is unavoidable, use C# default interface method syntax so existing implementors do not break.
3. **Extension methods for utility.** If NoZ's static `Graphics` class needs a new query, use a `static class GraphicsExtensions` in YesZ.Rendering rather than touching `Graphics.cs`.
4. **Document every fork change** in the Fork Changes Log table in `.claude/maintenance.md` before the PR is opened.

---

## antipattern-yesz-002 — Per-Draw Uniform Updates Instead of Globals Snapshot Batching

**Name:** Per-Object Uniform Storm
**Performance budget violated:** N+1 shader uploads rule

### What it looks like

Calling `UpdateUniformBuffer` and `BindUniformBuffer` once per object inside a draw loop, where each call uploads the model matrix (or MVP) immediately before `DrawElements`.

```csharp
// WRONG — per-draw uniform upload
foreach (var obj in scene.Objects)
{
    var mvp = obj.WorldMatrix * camera.ViewProjectionMatrix;
    Graphics.Driver.UpdateUniformBuffer(_mvpUbo, AsBytes(mvp));   // ← upload per object
    Graphics.Driver.BindUniformBuffer(_mvpUbo, slot: 1);
    Graphics.DrawElements(obj.Mesh.IndexCount, 0, order: 0);
}
```

### Why it fails

- **NoZ's render system is deferred.** `DrawElements` records a `DrawCommand` with the current `BatchState`. By the time `EndFrame()` executes the batch, all objects share the same UBO — the last-written matrix overwrites every draw's model transform.
- **Driver state contention.** `BindUniformBuffer` is global driver state. Calling it per-object in a loop means every object draws with the final object's matrix (N draws, all showing object N's transform).
- **Performance.** Even if timing were correct, N synchronous `UpdateUniformBuffer` calls on a dynamic buffer stall the GPU pipeline. This is a GPU-CPU sync point per object.

### Correct approach

Use the **globals snapshot system**. Each unique view-projection matrix gets a globals slot (max 64 per frame). For per-object transforms, either:

1. **Bake MVP into globals** (current Phase 1b–2 approach): `MVP = worldMatrix × viewProjection`. Each unique MVP gets its own globals slot. `Graphics3D.DrawMesh` calls `GetOrAddGlobals(mvp)` and `SetCurrentGlobalsIndex(slot)` before `DrawElements`. The batch system naturally assigns different globals indices to different objects.
2. **Structured buffer for large counts** (Phase 9+ instancing): Pre-upload all world matrices into a structured buffer once per frame. Index into it via `instance_index` in the WGSL vertex shader. Zero per-draw uploads after the initial bulk update.

---

## antipattern-yesz-003 — Depth Texture Binding Without Prepass Guard

**Name:** Shadow Sample Without Prepass
**Trust zone:** src/YesZ.Rendering/ (L2)

### What it looks like

Binding a shadow depth texture for sampling in the main scene pass without verifying that the shadow prepass completed for the current frame.

```csharp
// WRONG — unconditional bind; crashes or produces garbage on first frame / when disabled
Graphics.Driver.BindDepthTextureForSampling(_shadowMap, slot: 0);
Graphics.SetShader(_litShadowShader);
Graphics3D.DrawMesh(mesh, worldMatrix);
```

### Why it fails

- **Frame 0 / warmup**: The depth-only prepass runs in `Update()`. If `DrawMesh` is called before the prepass has rendered (e.g., during asset loading, or when shadow rendering is toggled off at runtime), the depth texture contains undefined data from the previous frame or from the driver's initial clear.
- **WebGPU validation error**: Binding a texture that is still attached as a depth-stencil output of an in-flight render pass triggers a validation layer error. The prepass must fully complete (`EndDepthOnlyPassLayer`) before the texture transitions to `TextureBinding` usage.
- **PCF reads garbage**: A depth texture that was never cleared to 1.0 will cause the PCF kernel to return 0.0 (fully shadowed) or 1.0 (fully lit) unpredictably. Visual corruption is delayed — it may only appear on scene camera movement.

### Correct approach

Guard all depth texture binds behind `_scenePrepassDone` (the flag set by `IGraphicsDriver3D.EndScenePass3D`'s prepass path):

```csharp
// In ShadowRenderer.BindForSampling():
if (!_prepassCompleted)
    return;  // Lit shader falls back to no-shadow variant

Graphics.Driver.BindDepthTextureArrayForSampling(_shadowArray);
```

Additionally, always clear the shadow depth texture to 1.0 at the start of each prepass via `LoadOp.Clear` in `BeginDepthOnlyPassLayer`. Never assume texture contents carry over from the previous frame.

---

## antipattern-yesz-004 — Skinned Meshes With Wrong Bone Count Assumptions

**Name:** Bone Count Mismatch
**Trust zone:** src/YesZ.Core/ (L3), src/YesZ.Rendering/ (L2)

### What it looks like

Hardcoding `MAX_BONES = 64` in the skinning shader while creating a uniform buffer sized for that count, then loading a glTF model with a different skeleton joint count.

```wgsl
// WRONG — hardcoded constant in shader; fails silently for models with ≠ 64 joints
const MAX_BONES: u32 = 64u;
struct SkinUniforms { joint_matrices: array<mat4x4f, 64> }
```

```csharp
// WRONG — assumes every skinned model has ≤ 64 joints
var skinUbo = Graphics.Driver.CreateUniformBuffer(64 * 64, BufferUsage.Dynamic, "Skin");
```

### Why it fails

- **glTF models have variable joint counts.** A character model may have 120+ joints (spine, hands, facial rig). Uploading 120 matrices to a 64-matrix buffer silently truncates joint data. Joints 65–120 use identity matrices, causing mesh sections to collapse to the origin.
- **WGSL array bounds are fixed at compile time.** The shader cannot dynamically resize the `joint_matrices` array based on the loaded skeleton. Under-sized arrays produce GPU validation errors in debug mode; in release mode, out-of-bounds reads return zeros.
- **Buffer size mismatch detection is delayed.** `UpdateUniformBuffer` accepts a byte span — it will write whatever you give it without checking against the buffer's declared size at the driver level. The corruption manifests visually, not as an exception.

### Correct approach

1. **Query joint count from the loaded skeleton before creating the buffer**: `int jointCount = skeleton.Joints.Length;`
2. **Size the skinning UBO dynamically**: `CreateUniformBuffer(jointCount * 64, BufferUsage.Dynamic, "Skin_" + modelName)`
3. **Pass joint count as a push constant or uniform**: The shader uses `jointCount` for loop bounds rather than a compile-time constant. If WGSL requires a fixed array, use the largest supported count (e.g., 256) and loop only to `jointCount`.
4. **Assert at load time**: `Debug.Assert(skeleton.Joints.Length <= MaxSupportedJoints, $"Skeleton has {skeleton.Joints.Length} joints, max is {MaxSupportedJoints}");`

---

## antipattern-yesz-005 — Render Passes Without Proper Depth Attachment

**Name:** Depth-Free Pass
**Trust zone:** engine/noz/ (L3), src/YesZ.Rendering/ (L2)

### What it looks like

Beginning a 3D scene render pass without the depth attachment, typically by calling `BeginScenePass` (the 2D NoZ variant) instead of `BeginScenePass3D` for 3D content.

```csharp
// WRONG — uses 2D pass; no depth texture attached
Graphics.Driver.BeginScenePass();  // ← attaches only color; no depth buffer
Graphics3D.DrawMesh(mesh, worldMatrix);
// Result: depth test is Always (no depth buffer → all fragments pass)
```

Similarly wrong: calling `BeginDepthOnlyPassLayer` without first allocating the depth texture array via `CreateDepthTextureArray`.

```csharp
// WRONG — depth texture array not yet created
Graphics3D.ShadowRenderer.BeginDepthOnlyPassLayer(cascadeIndex: 0);
// Result: null texture reference → WebGPU validation error
```

### Why it fails

- **No depth attachment = no depth testing.** Without a `DepthStencilAttachment` in the render pass descriptor, the GPU hardware skips the depth test entirely regardless of the `DepthCompare` value in the pipeline. Back faces appear through front faces; far objects appear on top of near objects.
- **`BeginScenePass` vs `BeginScenePass3D` distinction.** `BeginScenePass` is the NoZ 2D pass — it does not set the `_scenePrepassDone` flag and does not attach the depth texture array. Calling it before `DrawMesh` bypasses the entire 3D depth infrastructure built in Phases 1a and 6.
- **Uninitialized depth texture array.** `BeginDepthOnlyPassLayer` reads from `_depthTextureArrays[cascadeIndex]`. If `CreateDepthTextureArray` was not called first (e.g., because shadow map init was deferred to the first shadow-casting light), the slot is null. The WebGPU C API will dereference a null handle — behavior is undefined: crash, black shadow, or silent corruption.

### Correct approach

1. **Always use `BeginScenePass3D` for 3D content.** Check `IGraphicsDriver3D` method availability at `Graphics3D.Init()` and assert the driver implements it.
2. **Allocate depth texture arrays at `Graphics3D.Init()`, not on first use.** Shadow maps have known maximum counts (4 cascades × max lights). Pre-allocate the full array at startup.
3. **Verify depth attachment in debug builds.** Add a `Debug.Assert(_scenePrepassDone, "DrawMesh called before BeginScenePass3D")` in `Graphics3D.DrawMesh()` when `DEBUG` is defined.
4. **Match pass variant to content type.** 2D overlay content uses `BeginScenePass` / `ResumeScenePass`. 3D scene content uses `BeginScenePass3D`. Shadow depth content uses `BeginDepthOnlyPassLayer`. Never mix variants within a single frame's 3D render sequence.
