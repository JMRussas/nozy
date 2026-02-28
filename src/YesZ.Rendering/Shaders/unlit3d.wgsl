// YesZ — Unlit 3D shader
//
// Transforms vertices by MVP (stored in globals.projection).
// Outputs vertex color with no lighting.
//
// Flags: ShaderFlags.Depth | ShaderFlags.DepthLess

struct Globals {
    projection: mat4x4f,
    time: f32,
}
@group(0) @binding(0) var<uniform> globals: Globals;

struct VertexInput {
    @location(0) position: vec3f,
    @location(1) normal: vec3f,
    @location(2) uv: vec2f,
    @location(3) color: vec4f,
}

struct VertexOutput {
    @builtin(position) clip_position: vec4f,
    @location(0) color: vec4f,
}

@vertex fn vs_main(in: VertexInput) -> VertexOutput {
    var out: VertexOutput;
    out.clip_position = globals.projection * vec4f(in.position, 1.0);
    out.color = in.color;
    return out;
}

@fragment fn fs_main(in: VertexOutput) -> @location(0) vec4f {
    return in.color;
}
