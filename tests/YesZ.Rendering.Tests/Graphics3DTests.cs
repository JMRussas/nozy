//  YesZ - Graphics3D Tests
//
//  Unit tests for 3D rendering logic that doesn't require GPU initialization.
//  Full integration testing requires running HelloCube visually.
//
//  Depends on: YesZ.Rendering (Graphics3D), YesZ.Core (Camera3D, MeshVertex3D)
//  Used by:    CI

using Xunit;

namespace YesZ.Rendering.Tests;

public class Graphics3DTests
{
    [Fact]
    public void EmbeddedShader_Exists()
    {
        // Verify the WGSL shader is embedded in the assembly
        var assembly = typeof(Graphics3D).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();
        Assert.Contains("YesZ.Rendering.Shaders.unlit3d.wgsl", resourceNames);
    }

    [Fact]
    public void EmbeddedShader_HasContent()
    {
        var assembly = typeof(Graphics3D).Assembly;
        using var stream = assembly.GetManifestResourceStream("YesZ.Rendering.Shaders.unlit3d.wgsl");
        Assert.NotNull(stream);
        Assert.True(stream!.Length > 0);

        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        Assert.Contains("vs_main", content);
        Assert.Contains("fs_main", content);
        Assert.Contains("globals", content);
    }
}
