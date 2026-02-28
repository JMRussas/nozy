//  YesZ - Texture Loading Utility
//
//  Loads images from files or streams via StbImageSharp and uploads
//  them as GPU textures through NoZ's IGraphicsDriver. Also provides
//  procedural texture generation for testing.
//
//  Depends on: StbImageSharp, NoZ (Graphics, TextureFormat, TextureFilter)
//  Used by:    Game code, samples

using NoZ;
using StbImageSharp;

namespace YesZ.Rendering;

public static class TextureLoader
{
    /// <summary>
    /// Load an image file (PNG, JPG, BMP, TGA) and create a GPU texture.
    /// </summary>
    public static nuint LoadFromFile(string path, TextureFilter filter = TextureFilter.Linear)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream, filter, Path.GetFileNameWithoutExtension(path));
    }

    /// <summary>
    /// Load an image from a stream and create a GPU texture.
    /// </summary>
    public static nuint LoadFromStream(Stream stream, TextureFilter filter = TextureFilter.Linear, string? name = null)
    {
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        return Graphics.Driver.CreateTexture(
            image.Width,
            image.Height,
            image.Data,
            TextureFormat.RGBA8,
            filter,
            name);
    }

    /// <summary>
    /// Generate a checkerboard texture. Useful for testing UV mapping.
    /// </summary>
    /// <param name="size">Texture width and height in pixels.</param>
    /// <param name="colorA">First checkerboard color.</param>
    /// <param name="colorB">Second checkerboard color.</param>
    /// <param name="cellSize">Size of each checker cell in pixels.</param>
    /// <param name="filter">Texture filtering mode.</param>
    public static nuint CreateCheckerboard(
        int size,
        Color colorA,
        Color colorB,
        int cellSize = 32,
        TextureFilter filter = TextureFilter.Linear)
    {
        var pixels = new byte[size * size * 4];
        var a = colorA.ToColor32();
        var b = colorB.ToColor32();

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isA = ((x / cellSize) + (y / cellSize)) % 2 == 0;
                var c = isA ? a : b;
                int i = (y * size + x) * 4;
                pixels[i + 0] = c.R;
                pixels[i + 1] = c.G;
                pixels[i + 2] = c.B;
                pixels[i + 3] = c.A;
            }
        }

        return Graphics.Driver.CreateTexture(size, size, pixels, TextureFormat.RGBA8, filter, "Checkerboard");
    }
}
