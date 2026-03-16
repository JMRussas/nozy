//
//  Subset of Msdf.ShapeClipper for font compilation (Union only).
//  The full ShapeClipper in editor/src/msdf/ also handles sprite paths
//  which depend on NoZ.Editor.Shape — not available in noz-compile.
//

using System;
using Clipper2Lib;

namespace NoZ.Editor.Msdf;

internal static class ShapeClipper
{
    const int DefaultStepsPerCurve = 8;
    const int ClipperPrecision = 6;

    // Boolean-union all contours, producing non-overlapping linear contours.
    public static Shape Union(Shape shape, int stepsPerCurve = DefaultStepsPerCurve)
    {
        if (shape.contours.Count == 0)
            return shape;

        var paths = ShapeToPaths(shape, stepsPerCurve);
        if (paths.Count == 0)
            return shape;

        var tree = new PolyTreeD();
        Clipper.BooleanOp(ClipType.Union, paths, null, tree, FillRule.NonZero, ClipperPrecision);

        return TreeToShape(tree, shape) ?? shape;
    }

    internal static PathsD ShapeToPaths(Shape shape, int stepsPerCurve)
    {
        var paths = new PathsD();
        foreach (var contour in shape.contours)
        {
            var path = ContourToPath(contour, stepsPerCurve);
            if (path.Count >= 3)
                paths.Add(path);
        }
        return paths;
    }

    private static Shape? TreeToShape(PolyTreeD tree, Shape reference)
    {
        var result = new Shape();
        result.inverseYAxis = reference.inverseYAxis;
        CollectContours(tree, result);

        if (result.contours.Count == 0)
            return null;

        foreach (var contour in result.contours)
            contour.Reverse();

        return result;
    }

    private static PathD ContourToPath(Contour contour, int stepsPerCurve)
    {
        var path = new PathD();
        foreach (var edge in contour.edges)
        {
            switch (edge)
            {
                case LinearSegment lin:
                    path.Add(new PointD(lin.p[0].x, lin.p[0].y));
                    break;

                case QuadraticSegment quad:
                    for (int i = 0; i < stepsPerCurve; i++)
                    {
                        double t = (double)i / stepsPerCurve;
                        var p = quad.Point(t);
                        path.Add(new PointD(p.x, p.y));
                    }
                    break;

                case CubicSegment cub:
                    for (int i = 0; i < stepsPerCurve; i++)
                    {
                        double t = (double)i / stepsPerCurve;
                        var p = cub.Point(t);
                        path.Add(new PointD(p.x, p.y));
                    }
                    break;
            }
        }
        return path;
    }

    private static void CollectContours(PolyPathD node, Shape shape)
    {
        if (node.Polygon != null && node.Polygon.Count >= 3)
        {
            var contour = shape.AddContour();
            var poly = node.Polygon;
            for (int i = 0; i < poly.Count; i++)
            {
                int next = (i + 1) % poly.Count;
                contour.AddEdge(new LinearSegment(
                    new Vector2Double(poly[i].x, poly[i].y),
                    new Vector2Double(poly[next].x, poly[next].y)));
            }
        }

        for (int i = 0; i < node.Count; i++)
            CollectContours(node[i], shape);
    }
}
