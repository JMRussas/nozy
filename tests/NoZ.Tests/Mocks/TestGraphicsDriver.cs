//  NoZ.Tests - Mock IGraphicsDriver
//
//  Returns incrementing nuint handles for Create* methods.
//  Tracks all Create/Destroy calls in public lists for test assertions.

using System.Numerics;
using NoZ;
using NoZ.Platform;

namespace NoZ.Tests.Mocks;

public class TestGraphicsDriver : IGraphicsDriver
{
    private nuint _nextHandle = 1;

    public List<nuint> CreatedTextures { get; } = [];
    public List<nuint> DestroyedTextures { get; } = [];
    public List<nuint> CreatedShaders { get; } = [];
    public List<nuint> DestroyedShaders { get; } = [];
    public List<nuint> CreatedMeshes { get; } = [];
    public List<nuint> DestroyedMeshes { get; } = [];
    public List<nuint> CreatedBuffers { get; } = [];
    public List<nuint> DestroyedBuffers { get; } = [];
    public List<nuint> CreatedRenderTextures { get; } = [];
    public List<nuint> DestroyedRenderTextures { get; } = [];

    public string ShaderExtension => "";

    public void Init(GraphicsDriverConfig config) { }
    public void Shutdown() { }

    public bool BeginFrame() => true;
    public void EndFrame() { }

    public void SetViewport(in RectInt viewport) { }
    public void SetScissor(in RectInt scissor) { }
    public void ClearScissor() { }

    public nuint CreateMesh<T>(int maxVertices, int maxIndices, BufferUsage usage, string name = "") where T : IVertex
    {
        var handle = _nextHandle++;
        CreatedMeshes.Add(handle);
        return handle;
    }

    public void DestroyMesh(nuint handle) => DestroyedMeshes.Add(handle);
    public void BindMesh(nuint handle) { }
    public void UpdateMesh(nuint handle, ReadOnlySpan<byte> vertexData, ReadOnlySpan<ushort> indexData) { }

    public nuint CreateUniformBuffer(int sizeInBytes, BufferUsage usage, string name = "")
    {
        var handle = _nextHandle++;
        CreatedBuffers.Add(handle);
        return handle;
    }

    public void DestroyBuffer(nuint handle) => DestroyedBuffers.Add(handle);
    public void UpdateUniformBuffer(nuint buffer, int offsetBytes, ReadOnlySpan<byte> data) { }
    public void BindUniformBuffer(nuint buffer, int slot) { }

    public nuint CreateTexture(int width, int height, ReadOnlySpan<byte> data, TextureFormat format = TextureFormat.RGBA8, TextureFilter filter = TextureFilter.Linear, string? name = null)
    {
        var handle = _nextHandle++;
        CreatedTextures.Add(handle);
        return handle;
    }

    public void UpdateTexture(nuint handle, in Vector2Int size, ReadOnlySpan<byte> data) { }
    public void UpdateTextureRegion(nuint handle, in RectInt region, ReadOnlySpan<byte> data, int srcWidth = -1) { }
    public void DestroyTexture(nuint handle) => DestroyedTextures.Add(handle);
    public void BindTexture(nuint handle, int slot, TextureFilter filter = TextureFilter.Point) { }

    public nuint CreateTextureArray(int width, int height, int layers)
    {
        var handle = _nextHandle++;
        CreatedTextures.Add(handle);
        return handle;
    }

    public nuint CreateTextureArray(int width, int height, byte[][] layerData, TextureFormat format, TextureFilter filter, string? name = null)
    {
        var handle = _nextHandle++;
        CreatedTextures.Add(handle);
        return handle;
    }

    public void UpdateTextureLayer(nuint handle, int layer, ReadOnlySpan<byte> data) { }

    public nuint CreateShader(string name, string vertexSource, string fragmentSource, List<ShaderBinding> bindings)
    {
        var handle = _nextHandle++;
        CreatedShaders.Add(handle);
        return handle;
    }

    public void DestroyShader(nuint handle) => DestroyedShaders.Add(handle);
    public void BindShader(nuint handle) { }

    public void SetBlendMode(BlendMode mode) { }
    public void SetTextureFilter(TextureFilter filter) { }
    public void SetUniform(string name, ReadOnlySpan<byte> data) { }

    public void SetGlobalsCount(int count) { }
    public void SetGlobals(int index, ReadOnlySpan<byte> data) { }
    public void BindGlobals(int index) { }

    public void DrawElements(int firstIndex, int indexCount, int baseVertex = 0) { }
    public void SetVSync(bool vsync) { }

    public nuint CreateFence() => _nextHandle++;
    public void WaitFence(nuint fence) { }
    public void DeleteFence(nuint fence) { }

    public void BeginScenePass(Color clearColor) { }
    public void ResumeScenePass() { }
    public void EndScenePass() { }

    public nuint CreateRenderTexture(int width, int height, TextureFormat format = TextureFormat.BGRA8, int sampleCount = 1, string? name = null)
    {
        var handle = _nextHandle++;
        CreatedRenderTextures.Add(handle);
        return handle;
    }

    public void DestroyRenderTexture(nuint handle) => DestroyedRenderTextures.Add(handle);
    public void BeginRenderTexturePass(nuint renderTexture, Color clearColor) { }
    public void EndRenderTexturePass() { }
    public Task<byte[]> ReadRenderTexturePixelsAsync(nuint renderTexture) => Task.FromResult(Array.Empty<byte>());

    public void Reset()
    {
        CreatedTextures.Clear();
        DestroyedTextures.Clear();
        CreatedShaders.Clear();
        DestroyedShaders.Clear();
        CreatedMeshes.Clear();
        DestroyedMeshes.Clear();
        CreatedBuffers.Clear();
        DestroyedBuffers.Clear();
        CreatedRenderTextures.Clear();
        DestroyedRenderTextures.Clear();
    }
}
