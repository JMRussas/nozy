// YesZ — Textured 3D shader
//
// Transforms vertices by MVP (stored in globals.projection).
// Samples base color texture, multiplies by vertex color and material color factor.
//
// Flags: ShaderFlags.Depth | ShaderFlags.DepthLess

// Layout must match NoZ's Globals buffer (see Graphics.UploadGlobals).
// Do not reorder or remove fields.
struct Globals {
    projection: mat4x4f,
    time: f32,             // Unused here — required for layout compatibility with NoZ
}
@group(0) @binding(0) var<uniform> globals: Globals;

struct Material {
    base_color_factor: vec4f,
    metallic: f32,         // Unused until Phase 3b (lit shader)
    roughness: f32,        // Unused until Phase 3b (lit shader)
}
@group(0) @binding(1) var<uniform> material: Material;
@group(0) @binding(2) var base_color_texture: texture_2d<f32>;
@group(0) @binding(3) var base_color_sampler: sampler;

struct VertexInput {
    @location(0) position: vec3f,
    @location(1) normal: vec3f,
    @location(2) uv: vec2f,
    @location(3) color: vec4f,
}

struct VertexOutput {
    @builtin(position) clip_position: vec4f,
    @location(0) color: vec4f,
    @location(1) uv: vec2f,
}

@vertex fn vs_main(in: VertexInput) -> VertexOutput {
    var out: VertexOutput;
    out.clip_position = globals.projection * vec4f(in.position, 1.0);
    out.color = in.color;
    out.uv = in.uv;
    return out;
}

@fragment fn fs_main(in: VertexOutput) -> @location(0) vec4f {
    let tex_color = textureSample(base_color_texture, base_color_sampler, in.uv);
    return tex_color * in.color * material.base_color_factor;
}
