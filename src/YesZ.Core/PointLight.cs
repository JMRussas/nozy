//  YesZ - Point Light
//
//  Omnidirectional light at a position with color, intensity, and range.
//  Range must be positive; attenuation reaches zero at the range boundary.
//
//  Depends on: System.Numerics
//  Used by:    YesZ.Core (LightEnvironment), YesZ.Rendering (Graphics3D)

using System.Numerics;

namespace YesZ;

public struct PointLight
{
    private float _range;

    /// <summary>World-space position.</summary>
    public Vector3 Position { get; set; }

    /// <summary>Linear RGB color.</summary>
    public Vector3 Color { get; set; }

    /// <summary>Intensity multiplier.</summary>
    public float Intensity { get; set; }

    /// <summary>
    /// Maximum influence distance. Attenuation is zero beyond this.
    /// Must be positive; values ≤ 0 are clamped to <see cref="DefaultRange"/>.
    /// </summary>
    public float Range
    {
        readonly get => _range;
        set => _range = value > 0f ? value : DefaultRange;
    }

    /// <summary>Pre-multiplied color × intensity.</summary>
    public readonly Vector3 EffectiveColor => Color * Intensity;

    public const float DefaultRange = 10f;

    /// <summary>Create a point light at the origin with default values.</summary>
    public static PointLight Default => new()
    {
        Position = Vector3.Zero,
        Color = Vector3.One,
        Intensity = 1.0f,
        Range = DefaultRange,
    };
}
