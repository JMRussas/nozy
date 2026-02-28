//  YesZ - 3D Mesh
//
//  Immutable mesh container: vertex/index data uploaded to GPU on creation.
//  Holds a RenderMesh handle for use with Graphics.SetMesh().
//
//  Depends on: YesZ.Core (MeshVertex3D), NoZ (Graphics, RenderMesh, BufferUsage)
//  Used by:    Graphics3D, game code

using System.Runtime.InteropServices;
using NoZ;
using NoZ.Platform;

namespace YesZ;

public class Mesh3D : IDisposable
{
    public RenderMesh RenderMesh { get; }
    public int IndexCount { get; }

    private bool _disposed;

    private Mesh3D(RenderMesh renderMesh, int indexCount)
    {
        RenderMesh = renderMesh;
        IndexCount = indexCount;
    }

    /// <summary>
    /// Creates an immutable GPU mesh from vertex and index data.
    /// Must be called after Graphics is initialized (i.e., during or after LoadAssets).
    /// </summary>
    public static Mesh3D Create(MeshVertex3D[] vertices, ushort[] indices)
    {
        var renderMesh = Graphics.CreateMesh<MeshVertex3D>(vertices.Length, indices.Length, BufferUsage.Static, "Mesh3D");
        Graphics.Driver.UpdateMesh(
            renderMesh.Handle,
            MemoryMarshal.AsBytes<MeshVertex3D>(vertices),
            indices
        );
        return new Mesh3D(renderMesh, indices.Length);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (RenderMesh.Handle != nuint.Zero)
            Graphics.Driver.DestroyMesh(RenderMesh.Handle);

        GC.SuppressFinalize(this);
    }
}
