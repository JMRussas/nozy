//  YesZ - Mesh3DBuilder Tests
//
//  Verifies procedural cube geometry: vertex count, index count,
//  normals, winding order, bounds, and UV range.
//
//  Depends on: YesZ.Core (Mesh3DBuilder, MeshVertex3D)
//  Used by:    CI

using System.Numerics;
using Xunit;

namespace YesZ.Tests;

public class Mesh3DBuilderTests
{
    [Fact]
    public void CreateCube_Returns24Vertices()
    {
        var (vertices, _) = Mesh3DBuilder.CreateCube();
        Assert.Equal(24, vertices.Length); // 4 per face × 6 faces
    }

    [Fact]
    public void CreateCube_Returns36Indices()
    {
        var (_, indices) = Mesh3DBuilder.CreateCube();
        Assert.Equal(36, indices.Length); // 6 per face × 6 faces
    }

    [Fact]
    public void CreateCube_AllNormalsAreUnitLength()
    {
        var (vertices, _) = Mesh3DBuilder.CreateCube();
        foreach (var v in vertices)
        {
            Assert.Equal(1.0f, v.Normal.Length(), 0.001f);
        }
    }

    [Fact]
    public void CreateCube_FaceNormalsPointOutward()
    {
        var (vertices, indices) = Mesh3DBuilder.CreateCube();

        // Expected outward normals per face (2 triangles each)
        Vector3[] expectedFaceNormals =
        [
            Vector3.UnitZ,   // +Z front
            -Vector3.UnitZ,  // -Z back
            Vector3.UnitX,   // +X right
            -Vector3.UnitX,  // -X left
            Vector3.UnitY,   // +Y top
            -Vector3.UnitY,  // -Y bottom
        ];

        for (int i = 0; i < indices.Length; i += 3)
        {
            var v0 = vertices[indices[i]].Position;
            var v1 = vertices[indices[i + 1]].Position;
            var v2 = vertices[indices[i + 2]].Position;
            var cross = Vector3.Cross(v1 - v0, v2 - v0);
            var faceNormal = expectedFaceNormals[i / 6]; // 2 triangles per quad face
            Assert.True(Vector3.Dot(cross, faceNormal) > 0,
                $"Triangle {i / 3} cross product does not point along face normal {faceNormal}");
        }
    }

    [Fact]
    public void CreateCube_PositionsWithinUnitCube()
    {
        var (vertices, _) = Mesh3DBuilder.CreateCube();
        foreach (var v in vertices)
        {
            Assert.InRange(v.Position.X, -0.5f, 0.5f);
            Assert.InRange(v.Position.Y, -0.5f, 0.5f);
            Assert.InRange(v.Position.Z, -0.5f, 0.5f);
        }
    }

    [Fact]
    public void CreateCube_UVsWithinZeroOne()
    {
        var (vertices, _) = Mesh3DBuilder.CreateCube();
        foreach (var v in vertices)
        {
            Assert.InRange(v.UV.X, 0f, 1f);
            Assert.InRange(v.UV.Y, 0f, 1f);
        }
    }

    [Fact]
    public void CreateCube_WindingOrderCorrect()
    {
        var (vertices, indices) = Mesh3DBuilder.CreateCube();

        // Every triangle's cross product should point in the same direction
        // as the face normal (CCW winding when viewed from outside)
        for (int i = 0; i < indices.Length; i += 3)
        {
            var v0 = vertices[indices[i]].Position;
            var v1 = vertices[indices[i + 1]].Position;
            var v2 = vertices[indices[i + 2]].Position;
            var cross = Vector3.Cross(v1 - v0, v2 - v0);
            var vertexNormal = vertices[indices[i]].Normal;
            Assert.True(Vector3.Dot(cross, vertexNormal) > 0,
                $"Triangle {i / 3} has incorrect winding order");
        }
    }

    [Fact]
    public void CreateCube_IndicesInRange()
    {
        var (vertices, indices) = Mesh3DBuilder.CreateCube();
        foreach (var idx in indices)
        {
            Assert.InRange(idx, 0, vertices.Length - 1);
        }
    }
}
