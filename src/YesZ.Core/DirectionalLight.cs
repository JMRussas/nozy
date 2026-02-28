//  YesZ - Directional Light
//
//  Infinite-distance light with a direction, color, and intensity.
//  Direction is always normalized; setting a zero vector falls back to default.
//
//  Depends on: System.Numerics
//  Used by:    YesZ.Core (LightEnvironment), YesZ.Rendering (Graphics3D)

using System.Numerics;

namespace YesZ;

public struct DirectionalLight
{
    private Vector3 _direction;

    /// <summary>
    /// Direction the light travels (toward the scene). Always normalized.
    /// Setting a zero-length vector falls back to the default direction.
    /// </summary>
    public Vector3 Direction
    {
        readonly get => _direction;
        set
        {
            var len = value.Length();
            _direction = len > 1e-6f ? value / len : DefaultDirection;
        }
    }

    /// <summary>Linear RGB color (no alpha — meaningless for lights).</summary>
    public Vector3 Color { get; set; }

    /// <summary>Intensity multiplier (typically 1.0–5.0).</summary>
    public float Intensity { get; set; }

    /// <summary>Pre-multiplied color × intensity, ready for shader upload.</summary>
    public readonly Vector3 EffectiveColor => Color * Intensity;

    /// <summary>Default direction: downward-diagonal.</summary>
    public static readonly Vector3 DefaultDirection = Vector3.Normalize(new(-0.5f, -1f, -0.5f));

    /// <summary>Create a directional light with default values (white, intensity 1, downward).</summary>
    public static DirectionalLight Default => new()
    {
        Direction = DefaultDirection,
        Color = Vector3.One,
        Intensity = 1.0f,
    };
}
