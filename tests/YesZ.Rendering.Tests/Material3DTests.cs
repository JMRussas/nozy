//  YesZ - Material3D Tests
//
//  Verifies default material property values.
//
//  Depends on: YesZ.Rendering (Material3D)
//  Used by:    CI

using System.Numerics;
using Xunit;
using YesZ.Rendering;

namespace YesZ.Rendering.Tests;

public class Material3DTests
{
    private static Material3D CreateTestMaterial() => new(shaderHandle: 0, defaultTexture: 0);

    [Fact]
    public void DefaultBaseColorFactor_IsWhite()
    {
        var mat = CreateTestMaterial();
        Assert.Equal(Vector4.One, mat.BaseColorFactor);
    }

    [Fact]
    public void DefaultMetallic_IsZero()
    {
        var mat = CreateTestMaterial();
        Assert.Equal(0.0f, mat.Metallic);
    }

    [Fact]
    public void DefaultRoughness_IsHalf()
    {
        var mat = CreateTestMaterial();
        Assert.Equal(0.5f, mat.Roughness);
    }
}
