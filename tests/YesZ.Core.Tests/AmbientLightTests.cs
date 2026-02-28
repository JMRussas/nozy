//  YesZ - AmbientLight Tests
//
//  Verifies default values and effective color computation.
//
//  Depends on: YesZ.Core (AmbientLight)
//  Used by:    CI

using System.Numerics;
using Xunit;

namespace YesZ.Tests;

public class AmbientLightTests
{
    [Fact]
    public void Default_HasWhiteColorLowIntensity()
    {
        var light = AmbientLight.Default;

        Assert.Equal(Vector3.One, light.Color);
        Assert.Equal(0.1f, light.Intensity);
    }

    [Fact]
    public void EffectiveColor_IsColorTimesIntensity()
    {
        var light = new AmbientLight
        {
            Color = new Vector3(0.5f, 0.5f, 0.5f),
            Intensity = 0.2f,
        };

        var expected = new Vector3(0.1f, 0.1f, 0.1f);
        Assert.Equal(expected, light.EffectiveColor);
    }
}
