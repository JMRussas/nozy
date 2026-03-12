//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//
//  Mesh preview for generated sprites: tessellates mask shapes as white
//  regions and composites the generated image underneath.
//

using Clipper2Lib;
using LibTessDotNet;
using NoZ.Editor.Msdf;

namespace NoZ.Editor;

public partial class GenSpriteEditor
{
    private const int MaxMeshVertices = Shape.MaxAnchors * 16;
    private const int MaxMeshIndices = MaxMeshVertices * 3;

    private readonly MeshVertex[] _meshVertices = new MeshVertex[MaxMeshVertices];
    private readonly ushort[] _meshIndices = new ushort[MaxMeshIndices];
    private int _meshVersion = -1;

    private int _meshVertexCount;
    private int _meshIndexCount;

    private void UpdateMesh()
    {
        if (_meshVersion == Document.Version) return;
        _meshVersion = Document.Version;

        var vertexOffset = 0;
        var indexOffset = 0;

        // Tessellate all layers' mask shapes as white
        foreach (var layer in Document.Layers)
        {
            var shape = layer.Shape;

            var positivePaths = new PathsD();
            var negativePaths = new PathsD();

            for (ushort pi = 0; pi < shape.PathCount; pi++)
            {
                ref readonly var path = ref shape.GetPath(pi);
                if (path.AnchorCount < 3) continue;

                var pathShape = new Msdf.Shape();
                ShapeClipper.AppendContour(pathShape, shape, pi);
                pathShape = ShapeClipper.Union(pathShape);
                var contours = ShapeClipper.ShapeToPaths(pathShape, 8);
                if (contours.Count == 0) continue;

                if (path.IsSubtract)
                    negativePaths.AddRange(contours);
                else
                    positivePaths.AddRange(contours);
            }

            if (positivePaths.Count == 0) continue;

            PathsD maskPaths;
            if (negativePaths.Count > 0)
            {
                maskPaths = Clipper.BooleanOp(ClipType.Difference,
                    positivePaths, negativePaths, FillRule.NonZero, precision: 6);
            }
            else
            {
                maskPaths = positivePaths;
            }

            if (maskPaths.Count > 0)
                TessellatePaths(maskPaths, ref vertexOffset, ref indexOffset);
        }

        _meshVertexCount = vertexOffset;
        _meshIndexCount = indexOffset;
    }

    private void TessellatePaths(PathsD paths, ref int vertexOffset, ref int indexOffset)
    {
        var tess = new Tess();
        foreach (var path in paths)
        {
            if (path.Count < 3) continue;
            var verts = new ContourVertex[path.Count];
            for (int j = 0; j < path.Count; j++)
                verts[j].Position = new Vec3((float)path[j].x, (float)path[j].y, 0);
            tess.AddContour(verts);
        }

        tess.Tessellate(WindingRule.NonZero, LibTessDotNet.ElementType.Polygons, 3);

        if (tess.ElementCount == 0) return;

        var vertCount = tess.VertexCount;
        var idxCount = tess.ElementCount * 3;

        if (vertexOffset + vertCount > MaxMeshVertices ||
            indexOffset + idxCount > MaxMeshIndices)
            return;

        for (int v = 0; v < vertCount; v++)
        {
            ref var tv = ref tess.Vertices[v];
            _meshVertices[vertexOffset + v] = new MeshVertex(
                tv.Position.X, tv.Position.Y, 0, 0, Color.White);
        }

        for (int e = 0; e < tess.ElementCount; e++)
        {
            _meshIndices[indexOffset + e * 3 + 0] = (ushort)(tess.Elements[e * 3 + 0] + vertexOffset);
            _meshIndices[indexOffset + e * 3 + 1] = (ushort)(tess.Elements[e * 3 + 1] + vertexOffset);
            _meshIndices[indexOffset + e * 3 + 2] = (ushort)(tess.Elements[e * 3 + 2] + vertexOffset);
        }

        vertexOffset += vertCount;
        indexOffset += idxCount;
    }

    private void DrawMaskMesh()
    {
        if (_meshVertexCount == 0) return;

        using (Graphics.PushState())
        {
            Graphics.SetSortGroup(3);
            Graphics.SetLayer(EditorLayer.DocumentEditor);
            Graphics.SetTransform(Document.Transform);
            Graphics.SetTexture(Graphics.WhiteTexture);
            Graphics.SetShader(EditorAssets.Shaders.Texture);
            Graphics.SetColor(new Color(1f, 1f, 1f, 0.3f * Workspace.XrayAlpha));
            Graphics.Draw(
                _meshVertices.AsSpan(0, _meshVertexCount),
                _meshIndices.AsSpan(0, _meshIndexCount));
        }
    }

    private void DrawGeneratedImage()
    {
        var texture = Document.Generation.Texture;
        if (texture == null) return;

        var ppu = EditorApplication.Config.PixelsPerUnitInv;
        var cs = Document.ConstrainedSize;

        var rect = new Rect(
            cs.X * ppu * -0.5f,
            cs.Y * ppu * -0.5f,
            cs.X * ppu,
            cs.Y * ppu);

        using (Graphics.PushState())
        {
            Graphics.SetSortGroup(2);
            Graphics.SetLayer(EditorLayer.DocumentEditor);
            Graphics.SetTransform(Document.Transform);
            Graphics.SetTexture(texture);
            Graphics.SetShader(EditorAssets.Shaders.Texture);
            Graphics.SetColor(Color.White.WithAlpha(Workspace.XrayAlpha));
            Graphics.Draw(rect);
        }
    }
}
