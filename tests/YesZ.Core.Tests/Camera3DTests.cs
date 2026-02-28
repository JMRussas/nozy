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

    [Fact]
    public void ViewProjection_OriginAtNearPlane_MapsToClipCenter()
    {
        var camera = new Camera3D
        {
            Position = Vector3.Zero,
            Rotation = Quaternion.Identity,
            NearPlane = 0.1f,
            FarPlane = 100f,
        };

        // Point on camera's forward axis at near plane distance
        var worldPoint = new Vector4(0, 0, -camera.NearPlane, 1);
        var vp = camera.ViewProjectionMatrix;
        var clip = Vector4.Transform(worldPoint, vp);

        // After perspective divide, X and Y should be ~0 (center of screen)
        Assert.Equal(0f, clip.X / clip.W, 0.01f);
        Assert.Equal(0f, clip.Y / clip.W, 0.01f);
        // Z/W should be near 0 (near plane maps to 0 in WebGPU [0,1] depth)
        Assert.InRange(clip.Z / clip.W, -0.01f, 0.1f);
    }

    [Fact]
    public void ViewProjection_BehindCamera_HasNegativeW()
    {
        var camera = new Camera3D
        {
            Position = Vector3.Zero,
            Rotation = Quaternion.Identity,
        };

        // Point behind camera (+Z when camera looks down -Z)
        var worldPoint = new Vector4(0, 0, 5, 1);
        var vp = camera.ViewProjectionMatrix;
        var clip = Vector4.Transform(worldPoint, vp);

        // Point behind camera should have negative W (or z > 1 after divide)
        Assert.True(clip.W < 0 || clip.Z / clip.W > 1,
            $"Point behind camera should not be in clip space: W={clip.W}, Z/W={clip.Z / clip.W}");
    }

    [Fact]
    public void ProjectionMatrix_FOVChange_AffectsHorizontalExtent()
    {
        var narrowFov = new Camera3D { FieldOfView = 30f, AspectRatio = 1f };
        var wideFov = new Camera3D { FieldOfView = 90f, AspectRatio = 1f };

        // With wider FOV, a point at (1, 0, -1) should map closer to center in clip X
        // (wider FOV means more world space fits in [-1, 1] clip range)
        var testPoint = new Vector4(1, 0, -1, 1);

        var narrowClip = Vector4.Transform(testPoint, narrowFov.ViewProjectionMatrix);
        var wideClip = Vector4.Transform(testPoint, wideFov.ViewProjectionMatrix);

        var narrowNdcX = MathF.Abs(narrowClip.X / narrowClip.W);
        var wideNdcX = MathF.Abs(wideClip.X / wideClip.W);

        // Wider FOV should produce smaller NDC X (point is "closer to center")
        Assert.True(wideNdcX < narrowNdcX,
            $"Wide FOV NDC X ({wideNdcX}) should be less than narrow FOV NDC X ({narrowNdcX})");
    }
}
