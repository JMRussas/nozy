//  YesZ - LightEnvironment Tests
//
//  Verifies point light add/clear/overflow and default light values.
//
//  Depends on: YesZ.Core (LightEnvironment, PointLight, AmbientLight, DirectionalLight)
//  Used by:    CI

using System;
using System.Numerics;
using Xunit;

namespace YesZ.Tests;

public class LightEnvironmentTests
{
    [Fact]
    public void AddPointLight_UnderMax_Succeeds()
    {
        var env = new LightEnvironment();

        for (int i = 0; i < LightEnvironment.MaxPointLights; i++)
        {
            env.AddPointLight(new PointLight { Position = new Vector3(i, 0, 0), Range = 5f });
        }

        Assert.Equal(LightEnvironment.MaxPointLights, env.PointLightCount);
    }

    [Fact]
    public void AddPointLight_OverMax_Throws()
    {
        var env = new LightEnvironment();

        for (int i = 0; i < LightEnvironment.MaxPointLights; i++)
        {
            env.AddPointLight(new PointLight { Range = 5f });
        }

        Assert.Throws<InvalidOperationException>(() =>
            env.AddPointLight(new PointLight { Range = 5f }));
    }

    [Fact]
    public void ClearPointLights_ResetsCount()
    {
        var env = new LightEnvironment();
        env.AddPointLight(new PointLight { Range = 5f });
        env.AddPointLight(new PointLight { Range = 5f });

        env.ClearPointLights();

        Assert.Equal(0, env.PointLightCount);
        Assert.True(env.PointLights.IsEmpty);
    }

    [Fact]
    public void Default_HasReasonableAmbient()
    {
        var env = new LightEnvironment();

        Assert.Equal(Vector3.One, env.Ambient.Color);
        Assert.InRange(env.Ambient.Intensity, 0.05f, 0.5f);
    }

    [Fact]
    public void Default_HasReasonableDirectional()
    {
        var env = new LightEnvironment();

        var dirLen = env.Directional.Direction.Length();
        Assert.InRange(dirLen, 0.999f, 1.001f);
        Assert.Equal(Vector3.One, env.Directional.Color);
        Assert.InRange(env.Directional.Intensity, 0.5f, 5.0f);
    }

    [Fact]
    public void ClearPointLights_ZerosArrayData()
    {
        var env = new LightEnvironment();
        env.AddPointLight(new PointLight { Position = new Vector3(99, 99, 99), Range = 50f });
        env.ClearPointLights();

        // Re-add one light — the old data at index 0 should be gone
        env.AddPointLight(new PointLight { Position = new Vector3(1, 2, 3), Range = 5f });

        Assert.Equal(1, env.PointLightCount);
        Assert.Equal(new Vector3(1, 2, 3), env.PointLights[0].Position);
    }

    [Fact]
    public void PointLights_ReturnsCorrectSpan()
    {
        var env = new LightEnvironment();

        var p0 = new PointLight { Position = new Vector3(1, 0, 0), Color = Vector3.UnitX, Intensity = 1f, Range = 5f };
        var p1 = new PointLight { Position = new Vector3(0, 2, 0), Color = Vector3.UnitY, Intensity = 2f, Range = 10f };
        var p2 = new PointLight { Position = new Vector3(0, 0, 3), Color = Vector3.UnitZ, Intensity = 3f, Range = 15f };

        env.AddPointLight(p0);
        env.AddPointLight(p1);
        env.AddPointLight(p2);

        var span = env.PointLights;
        Assert.Equal(3, span.Length);
        Assert.Equal(new Vector3(1, 0, 0), span[0].Position);
        Assert.Equal(new Vector3(0, 2, 0), span[1].Position);
        Assert.Equal(new Vector3(0, 0, 3), span[2].Position);
    }
}
