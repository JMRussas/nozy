//  YesZ - Camera3D Tests
//
//  Verifies perspective projection and view matrix computation.
//
//  Depends on: YesZ.Core (Camera3D)
//  Used by:    CI

using System.Numerics;
using Xunit;

namespace YesZ.Tests;

public class Camera3DTests
{
    [Fact]
    public void DefaultCamera_HasValidProjection()
    {
        var camera = new Camera3D();
        var proj = camera.ProjectionMatrix;

        // Projection matrix should not be identity
        Assert.NotEqual(Matrix4x4.Identity, proj);

        // Near plane > 0
        Assert.True(camera.NearPlane > 0);

        // Far plane > near plane
        Assert.True(camera.FarPlane > camera.NearPlane);
    }

    [Fact]
    public void Camera_AtOffset_TranslatesInView()
    {
        var camera = new Camera3D { Position = new Vector3(0, 0, 5) };
        var view = camera.ViewMatrix;

        // Camera at (0,0,5) looking down -Z: view matrix should translate by -5 on Z
        Assert.Equal(-5.0f, view.M43, 0.001f);
    }

    [Fact]
    public void ViewProjection_CombinesCorrectly()
    {
        var camera = new Camera3D();
        var vp = camera.ViewProjectionMatrix;
        var expected = camera.ViewMatrix * camera.ProjectionMatrix;

        Assert.Equal(expected, vp);
    }

    [Fact]
    public void AspectRatio_AffectsProjection()
    {
        var camera16x9 = new Camera3D { AspectRatio = 16.0f / 9.0f };
        var camera4x3 = new Camera3D { AspectRatio = 4.0f / 3.0f };

        Assert.NotEqual(camera16x9.ProjectionMatrix, camera4x3.ProjectionMatrix);
    }
}
