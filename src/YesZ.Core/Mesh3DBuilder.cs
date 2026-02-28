//  YesZ - 3D Mesh Builder
//
//  Static methods for creating procedural meshes.
//  Returns raw vertex/index arrays — Mesh3D handles GPU upload.
//
//  Depends on: YesZ.Core (MeshVertex3D), NoZ (Color)
//  Used by:    Game code, samples

using System.Numerics;
using NoZ;

namespace YesZ;

public static class Mesh3DBuilder
{
    /// <summary>
    /// Creates a unit cube centered at origin (positions in [-0.5, 0.5]).
    /// 24 vertices (4 per face for flat normals), 36 indices (CCW winding).
    /// Each face has a distinct color.
    /// </summary>
    public static (MeshVertex3D[] Vertices, ushort[] Indices) CreateCube()
    {
        var vertices = new MeshVertex3D[24];
        var indices = new ushort[36];

        // Face definitions: normal, tangent-right, tangent-up, color
        ReadOnlySpan<(Vector3 Normal, Vector3 Right, Vector3 Up, Color Color)> faces =
        [
            (Vector3.UnitZ,  Vector3.UnitX,  Vector3.UnitY, new Color(1f, 0.2f, 0.2f, 1f)),   // +Z front  — red
            (-Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitY, new Color(0.2f, 0.8f, 0.2f, 1f)), // -Z back   — green
            (Vector3.UnitX,  -Vector3.UnitZ, Vector3.UnitY, new Color(0.2f, 0.4f, 1f, 1f)),   // +X right  — blue
            (-Vector3.UnitX, Vector3.UnitZ,  Vector3.UnitY, new Color(1f, 1f, 0.2f, 1f)),     // -X left   — yellow
            (Vector3.UnitY,  Vector3.UnitX,  -Vector3.UnitZ, new Color(0.2f, 1f, 1f, 1f)),    // +Y top    — cyan
            (-Vector3.UnitY, Vector3.UnitX,  Vector3.UnitZ, new Color(1f, 0.2f, 1f, 1f)),     // -Y bottom — magenta
        ];

        for (int face = 0; face < 6; face++)
        {
            var (normal, right, up, color) = faces[face];
            var center = normal * 0.5f;
            int vi = face * 4;
            int ii = face * 6;

            // 4 corners: center + combinations of ±right*0.5 ± up*0.5
            var halfR = right * 0.5f;
            var halfU = up * 0.5f;

            vertices[vi + 0] = new MeshVertex3D
            {
                Position = center - halfR - halfU,
                Normal = normal,
                UV = new Vector2(0, 1),
                Color = color,
            };
            vertices[vi + 1] = new MeshVertex3D
            {
                Position = center + halfR - halfU,
                Normal = normal,
                UV = new Vector2(1, 1),
                Color = color,
            };
            vertices[vi + 2] = new MeshVertex3D
            {
                Position = center + halfR + halfU,
                Normal = normal,
                UV = new Vector2(1, 0),
                Color = color,
            };
            vertices[vi + 3] = new MeshVertex3D
            {
                Position = center - halfR + halfU,
                Normal = normal,
                UV = new Vector2(0, 0),
                Color = color,
            };

            // Two triangles per face (CCW winding when viewed from outside)
            indices[ii + 0] = (ushort)(vi + 0);
            indices[ii + 1] = (ushort)(vi + 1);
            indices[ii + 2] = (ushort)(vi + 2);
            indices[ii + 3] = (ushort)(vi + 0);
            indices[ii + 4] = (ushort)(vi + 2);
            indices[ii + 5] = (ushort)(vi + 3);
        }

        return (vertices, indices);
    }
}
