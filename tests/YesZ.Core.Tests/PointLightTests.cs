//  YesZ - PointLight Tests
//
//  Verifies range validation and default values.
//
//  Depends on: YesZ.Core (PointLight)
//  Used by:    CI

using System.Numerics;
using Xunit;

namespace YesZ.Tests;

public class PointLightTests
{
    [Fact]
    public void Range_ZeroOrNegative_ClampsToDefault()
    {
        var light = new PointLight { Range = 0f };
        Assert.Equal(PointLight.DefaultRange, light.Range);

        light.Range = -5f;
        Assert.Equal(PointLight.DefaultRange, light.Range);
    }

    [Fact]
    public void Range_Positive_IsAccepted()
    {
        var light = new PointLight { Range = 25f };
        Assert.Equal(25f, light.Range);
    }

    [Fact]
    public void Default_HasPositiveRange()
    {
        var light = PointLight.Default;
        Assert.True(light.Range > 0f);
    }

    [Fact]
    public void EffectiveColor_IsColorTimesIntensity()
    {
        var light = new PointLight
        {
            Color = new Vector3(0, 1, 0),
            Intensity = 3.0f,
            Range = 10f,
        };

        Assert.Equal(new Vector3(0, 3, 0), light.EffectiveColor);
    }

    [Fact]
    public void DefaultConstructed_Range_FallsBackToDefault()
    {
        // struct default: _range is 0f — getter must return DefaultRange
        var light = new PointLight();

        Assert.Equal(PointLight.DefaultRange, light.Range);
    }
}
