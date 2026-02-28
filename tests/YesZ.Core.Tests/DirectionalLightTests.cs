//  YesZ - DirectionalLight Tests
//
//  Verifies direction normalization, zero-vector fallback, and effective color computation.
//
//  Depends on: YesZ.Core (DirectionalLight)
//  Used by:    CI

using System.Numerics;
using Xunit;

namespace YesZ.Tests;

public class DirectionalLightTests
{
    [Fact]
    public void Direction_SetNonUnit_IsNormalized()
    {
        var light = new DirectionalLight { Direction = new Vector3(1, 2, 3) };

        var len = light.Direction.Length();
        Assert.InRange(len, 0.999f, 1.001f);
    }

    [Fact]
    public void Direction_SetZero_FallsBackToDefault()
    {
        var light = new DirectionalLight { Direction = Vector3.Zero };

        Assert.Equal(DirectionalLight.DefaultDirection, light.Direction);
    }

    [Fact]
    public void EffectiveColor_IsColorTimesIntensity()
    {
        var light = new DirectionalLight
        {
            Direction = -Vector3.UnitY,
            Color = new Vector3(1, 0, 0),
            Intensity = 2.0f,
        };

        Assert.Equal(new Vector3(2, 0, 0), light.EffectiveColor);
    }

    [Fact]
    public void EffectiveColor_ZeroIntensity_IsBlack()
    {
        var light = new DirectionalLight
        {
            Direction = -Vector3.UnitY,
            Color = Vector3.One,
            Intensity = 0f,
        };

        Assert.Equal(Vector3.Zero, light.EffectiveColor);
    }

    [Fact]
    public void Default_HasNormalizedDirection()
    {
        var light = DirectionalLight.Default;

        var len = light.Direction.Length();
        Assert.InRange(len, 0.999f, 1.001f);
    }
}
