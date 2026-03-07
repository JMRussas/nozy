//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

namespace NoZ.Platform;

public interface IGraphicsDriver3D
{
    // Depth texture arrays (for cascaded shadow maps)
    nuint CreateDepthTextureArray(int width, int height, int layers, string? name = null);
    void DestroyDepthTextureArray(nuint handle);
    void BeginDepthOnlyPassLayer(nuint handle, int layer, int width, int height);
    void EndDepthOnlyPass();
    void BindDepthTextureArrayForSampling(nuint handle);

    // 3D scene pass (renders before NoZ's 2D pass, preserves depth+color)
    void BeginScenePass3D(Color clearColor);
    void EndScenePass3D();
}
