//  YesZ - Light Environment
//
//  Container for all lights in a frame: one ambient, one directional,
//  up to MaxPointLights point lights. Point lights are cleared each frame
//  via ClearPointLights() (called by Graphics3D.Begin()).
//
//  Depends on: YesZ.Core (DirectionalLight, PointLight, AmbientLight)
//  Used by:    YesZ.Rendering (Graphics3D)

using System;

namespace YesZ;

public class LightEnvironment
{
    public const int MaxPointLights = 8;

    private readonly PointLight[] _pointLights = new PointLight[MaxPointLights];
    private int _pointLightCount;

    public AmbientLight Ambient { get; set; } = AmbientLight.Default;
    public DirectionalLight Directional { get; set; } = DirectionalLight.Default;

    public ReadOnlySpan<PointLight> PointLights => _pointLights.AsSpan(0, _pointLightCount);
    public int PointLightCount => _pointLightCount;

    public void AddPointLight(in PointLight light)
    {
        if (_pointLightCount >= MaxPointLights)
            throw new InvalidOperationException(
                $"Cannot add more than {MaxPointLights} point lights per frame.");

        _pointLights[_pointLightCount++] = light;
    }

    public void ClearPointLights()
    {
        _pointLightCount = 0;
    }
}
