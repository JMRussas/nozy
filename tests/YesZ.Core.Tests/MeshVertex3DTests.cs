//  YesZ - MeshVertex3D Tests
//
//  Verifies vertex struct layout, attribute descriptors, and hash uniqueness.
//
//  Depends on: YesZ.Core (MeshVertex3D), NoZ (MeshVertex, VertexAttribType)
//  Used by:    CI

using NoZ;
using Xunit;

namespace YesZ.Tests;

public class MeshVertex3DTests
{
    [Fact]
    public void GetFormatDescriptor_Default_Returns48ByteStride()
    {
        var desc = MeshVertex3D.GetFormatDescriptor();
        Assert.Equal(48, desc.Stride);
        Assert.Equal(48, MeshVertex3D.SizeInBytes);
    }

    [Fact]
    public void GetFormatDescriptor_Default_Has4Attributes()
    {
        var desc = MeshVertex3D.GetFormatDescriptor();
        Assert.Equal(4, desc.Attributes.Length);
    }

    [Fact]
    public void GetFormatDescriptor_OffsetsMatchMarshal()
    {
        var desc = MeshVertex3D.GetFormatDescriptor();
        Assert.Equal(0, desc.Attributes[0].Offset);   // Position
        Assert.Equal(12, desc.Attributes[1].Offset);  // Normal
        Assert.Equal(24, desc.Attributes[2].Offset);  // UV
        Assert.Equal(32, desc.Attributes[3].Offset);  // Color
    }

    [Fact]
    public void GetFormatDescriptor_ComponentCounts()
    {
        var desc = MeshVertex3D.GetFormatDescriptor();
        Assert.Equal(3, desc.Attributes[0].Components); // Position — vec3
        Assert.Equal(3, desc.Attributes[1].Components); // Normal — vec3
        Assert.Equal(2, desc.Attributes[2].Components); // UV — vec2
        Assert.Equal(4, desc.Attributes[3].Components); // Color — vec4
    }

    [Fact]
    public void GetFormatDescriptor_AllAttributesAreFloat()
    {
        var desc = MeshVertex3D.GetFormatDescriptor();
        foreach (var attr in desc.Attributes)
        {
            Assert.Equal(VertexAttribType.Float, attr.Type);
        }
    }

    [Fact]
    public void GetFormatDescriptor_LocationsAre0Through3()
    {
        var desc = MeshVertex3D.GetFormatDescriptor();
        for (int i = 0; i < desc.Attributes.Length; i++)
        {
            Assert.Equal(i, desc.Attributes[i].Location);
        }
    }

    [Fact]
    public void VertexHash_DiffersFromNoZMeshVertex()
    {
        Assert.NotEqual(MeshVertex.VertexHash, MeshVertex3D.VertexHash);
    }
}
