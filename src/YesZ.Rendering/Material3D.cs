//  YesZ - 3D Material
//
//  Holds shader, texture, and PBR parameters for 3D rendering.
//  Created via Graphics3D.CreateMaterial() which provides the
//  textured shader and default white texture.
//
//  Depends on: System.Numerics
//  Used by:    Graphics3D (SetMaterial), game code

using System.Numerics;

namespace YesZ.Rendering;

public class Material3D
{
    /// <summary>Driver shader handle for this material's rendering program.</summary>
    internal nuint ShaderHandle { get; }

    /// <summary>Driver texture handle for the base color map. Defaults to 1x1 white.</summary>
    public nuint BaseColorTexture { get; set; }

    /// <summary>RGBA color multiplier applied to texture × vertex color.</summary>
    public Vector4 BaseColorFactor { get; set; } = Vector4.One;

    /// <summary>Metalness: 0 = dielectric, 1 = metal. Used in Phase 3b.</summary>
    public float Metallic { get; set; } = 0.0f;

    /// <summary>Roughness: 0 = mirror, 1 = fully diffuse. Used in Phase 3b.</summary>
    public float Roughness { get; set; } = 0.5f;

    internal Material3D(nuint shaderHandle, nuint defaultTexture)
    {
        ShaderHandle = shaderHandle;
        BaseColorTexture = defaultTexture;
    }
}
