//  YesZ - Ambient Light
//
//  Uniform environmental illumination applied to all surfaces equally.
//
//  Depends on: System.Numerics
//  Used by:    YesZ.Core (LightEnvironment), YesZ.Rendering (Graphics3D)

using System.Numerics;

namespace YesZ;

public struct AmbientLight
{
    /// <summary>Linear RGB color.</summary>
    public Vector3 Color { get; set; }

    /// <summary>Intensity multiplier (typically 0.1–0.3).</summary>
    public float Intensity { get; set; }

    /// <summary>Pre-multiplied color × intensity.</summary>
    public readonly Vector3 EffectiveColor => Color * Intensity;

    /// <summary>Create an ambient light with default values (white, low intensity).</summary>
    public static AmbientLight Default => new()
    {
        Color = Vector3.One,
        Intensity = 0.1f,
    };
}
