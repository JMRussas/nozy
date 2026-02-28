//  YesZ - Material Uniform Buffer Layout
//
//  GPU-compatible struct for material parameters. Uploaded via
//  Graphics.SetUniform<MaterialUniforms>("material", ...) and read
//  by the textured3d.wgsl shader at @binding(1).
//
//  PBR parameters (Metallic, Roughness) are stored but unused until
//  Phase 3b when the lit shader reads them.
//
//  Depends on: System.Numerics, System.Runtime.InteropServices
//  Used by:    Graphics3D (uniform upload), MaterialUniformsTests

using System.Numerics;
using System.Runtime.InteropServices;

namespace YesZ.Rendering;

[StructLayout(LayoutKind.Sequential)]
internal struct MaterialUniforms
{
    public Vector4 BaseColorFactor;  // 16 bytes — RGBA color multiplier
    public float Metallic;           //  4 bytes — 0 = dielectric, 1 = metal
    public float Roughness;          //  4 bytes — 0 = mirror, 1 = diffuse
    public float _pad0;              //  4 bytes — align to 16-byte boundary
    public float _pad1;              //  4 bytes
}
// Total: 32 bytes (WebGPU requires uniform buffer size to be multiple of 16)
