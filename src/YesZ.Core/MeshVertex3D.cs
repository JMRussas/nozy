//  YesZ - 3D Mesh Vertex
//
//  48-byte vertex format for 3D meshes: position, normal, UV, color.
//  Implements NoZ's IVertex for pipeline cache compatibility.
//
//  Depends on: NoZ (IVertex, VertexFormatDescriptor, VertexAttribute, VertexFormatHash, Color)
//  Used by:    Mesh3D, Mesh3DBuilder, Graphics3D, 3D shaders

using System.Numerics;
using System.Runtime.InteropServices;
using NoZ;
using NoZ.Platform;

namespace YesZ;

[StructLayout(LayoutKind.Sequential)]
public struct MeshVertex3D : IVertex
{
    public Vector3 Position;    // location 0 — 3× float32, offset 0
    public Vector3 Normal;      // location 1 — 3× float32, offset 12
    public Vector2 UV;          // location 2 — 2× float32, offset 24
    public Color   Color;       // location 3 — 4× float32, offset 32

    public static readonly int SizeInBytes = Marshal.SizeOf<MeshVertex3D>();  // 48 bytes
    public static readonly uint VertexHash = VertexFormatHash.Compute(GetFormatDescriptor().Attributes);

    public static VertexFormatDescriptor GetFormatDescriptor() => new()
    {
        Stride = SizeInBytes,
        Attributes =
        [
            new VertexAttribute(0, 3, VertexAttribType.Float, (int)Marshal.OffsetOf<MeshVertex3D>(nameof(Position))),
            new VertexAttribute(1, 3, VertexAttribType.Float, (int)Marshal.OffsetOf<MeshVertex3D>(nameof(Normal))),
            new VertexAttribute(2, 2, VertexAttribType.Float, (int)Marshal.OffsetOf<MeshVertex3D>(nameof(UV))),
            new VertexAttribute(3, 4, VertexAttribType.Float, (int)Marshal.OffsetOf<MeshVertex3D>(nameof(Color))),
        ]
    };
}
