//  YesZ - Transform3D Tests
//
//  Verifies 3D transform matrix computation.
//
//  Depends on: YesZ.Core (Transform3D)
//  Used by:    CI

using System.Numerics;
using Xunit;

namespace YesZ.Tests;

public class Transform3DTests
{
    [Fact]
    public void Identity_ProducesIdentityMatrix()
    {
        var transform = Transform3D.Identity;
        Assert.Equal(Matrix4x4.Identity, transform.LocalMatrix);
    }

    [Fact]
    public void Translation_AppliedCorrectly()
    {
        var transform = Transform3D.Identity;
        transform.Position = new Vector3(1, 2, 3);

        var matrix = transform.LocalMatrix;
        Assert.Equal(1.0f, matrix.M41, 0.001f);
        Assert.Equal(2.0f, matrix.M42, 0.001f);
        Assert.Equal(3.0f, matrix.M43, 0.001f);
    }

    [Fact]
    public void Scale_AppliedCorrectly()
    {
        var transform = Transform3D.Identity;
        transform.Scale = new Vector3(2, 3, 4);

        var matrix = transform.LocalMatrix;
        Assert.Equal(2.0f, matrix.M11, 0.001f);
        Assert.Equal(3.0f, matrix.M22, 0.001f);
        Assert.Equal(4.0f, matrix.M33, 0.001f);
    }

    [Fact]
    public void Rotation_90DegreesAroundY()
    {
        var transform = Transform3D.Identity;
        transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

        var matrix = transform.LocalMatrix;

        // After 90 degree Y rotation: X axis points toward -Z
        Assert.Equal(0.0f, matrix.M11, 0.001f);  // cos(90) ≈ 0
        Assert.Equal(-1.0f, matrix.M13, 0.001f);  // -sin(90) = -1
    }
}
