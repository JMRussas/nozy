//  YesZ - MaterialUniforms Tests
//
//  Verifies GPU uniform buffer layout matches WGSL expectations.
//  WebGPU requires 16-byte alignment and exact field offsets.
//
//  Depends on: YesZ.Rendering (MaterialUniforms)
//  Used by:    CI

using System.Runtime.InteropServices;
using Xunit;
using YesZ.Rendering;

namespace YesZ.Rendering.Tests;

public class MaterialUniformsTests
{
    [Fact]
    public void SizeOf_Returns32Bytes()
    {
        Assert.Equal(32, Marshal.SizeOf<MaterialUniforms>());
    }

    [Fact]
    public void BaseColorFactorOffset_Is0()
    {
        Assert.Equal(0, Marshal.OffsetOf<MaterialUniforms>(nameof(MaterialUniforms.BaseColorFactor)).ToInt32());
    }

    [Fact]
    public void MetallicOffset_Is16()
    {
        Assert.Equal(16, Marshal.OffsetOf<MaterialUniforms>(nameof(MaterialUniforms.Metallic)).ToInt32());
    }

    [Fact]
    public void RoughnessOffset_Is20()
    {
        Assert.Equal(20, Marshal.OffsetOf<MaterialUniforms>(nameof(MaterialUniforms.Roughness)).ToInt32());
    }
}
