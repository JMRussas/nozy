//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

using NoZ;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

static class TextureCompiler
{
    public static void Run(string[] args)
    {
        if (args.Length < 2 || args[0] is "-h" or "--help")
        {
            PrintUsage();
            return;
        }

        var inputPath = args[0];
        var outputPath = args[1];

        var filter = TextureFilter.Linear;
        var format = TextureFormat.RGBA8;
        var clamp = TextureClamp.Clamp;

        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--filter" when i + 1 < args.Length:
                    filter = args[++i].ToLowerInvariant() switch
                    {
                        "point" or "nearest" => TextureFilter.Point,
                        _ => TextureFilter.Linear,
                    };
                    break;

                case "--format" when i + 1 < args.Length:
                    format = args[++i].ToLowerInvariant() switch
                    {
                        "r8" => TextureFormat.R8,
                        "rg8" => TextureFormat.RG8,
                        "rgb8" => TextureFormat.RGB8,
                        _ => TextureFormat.RGBA8,
                    };
                    break;

                case "--clamp" when i + 1 < args.Length:
                    clamp = args[++i].ToLowerInvariant() switch
                    {
                        "repeat" => TextureClamp.Repeat,
                        _ => TextureClamp.Clamp,
                    };
                    break;

                default:
                    Console.Error.WriteLine($"Unknown option: {args[i]}");
                    return;
            }
        }

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Input file not found: {inputPath}");
            return;
        }

        Compile(inputPath, outputPath, format, filter, clamp);
    }

    public static void Compile(
        string inputPath,
        string outputPath,
        TextureFormat format = TextureFormat.RGBA8,
        TextureFilter filter = TextureFilter.Linear,
        TextureClamp clamp = TextureClamp.Clamp)
    {
        using var image = Image.Load<Rgba32>(inputPath);

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var stream = File.Create(outputPath);
        using var writer = new BinaryWriter(stream);

        writer.WriteAssetHeader(AssetType.Texture, Texture.Version);

        writer.Write((byte)format);
        writer.Write((byte)filter);
        writer.Write((byte)clamp);
        writer.Write((uint)image.Width);
        writer.Write((uint)image.Height);

        var bpp = GetBytesPerPixel(format);
        var dataLength = image.Width * image.Height * bpp;
        var pixels = new byte[dataLength];

        if (format == TextureFormat.RGBA8)
        {
            image.CopyPixelDataTo(pixels);
        }
        else
        {
            // For non-RGBA8 formats, load as RGBA8 and convert
            var rgba = new byte[image.Width * image.Height * 4];
            image.CopyPixelDataTo(rgba);
            ConvertPixels(rgba, pixels, image.Width * image.Height, format);
        }

        writer.Write(pixels);

        Console.WriteLine($"Compiled texture: {image.Width}x{image.Height} {format} {filter} {clamp} -> {outputPath}");
    }

    private static void ConvertPixels(byte[] rgba, byte[] output, int pixelCount, TextureFormat format)
    {
        for (int i = 0; i < pixelCount; i++)
        {
            var srcOffset = i * 4;
            switch (format)
            {
                case TextureFormat.R8:
                    output[i] = rgba[srcOffset];
                    break;

                case TextureFormat.RG8:
                    output[i * 2] = rgba[srcOffset];
                    output[i * 2 + 1] = rgba[srcOffset + 1];
                    break;

                case TextureFormat.RGB8:
                    output[i * 3] = rgba[srcOffset];
                    output[i * 3 + 1] = rgba[srcOffset + 1];
                    output[i * 3 + 2] = rgba[srcOffset + 2];
                    break;
            }
        }
    }

    private static int GetBytesPerPixel(TextureFormat format) => format switch
    {
        TextureFormat.RGBA8 => 4,
        TextureFormat.RGB8 => 3,
        TextureFormat.RG8 => 2,
        TextureFormat.R8 => 1,
        _ => 4,
    };

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: noz-compile texture <input.png> <output> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --filter <linear|point>       Texture filter mode (default: linear)");
        Console.WriteLine("  --format <rgba8|r8|rg8|rgb8>  Pixel format (default: rgba8)");
        Console.WriteLine("  --clamp <clamp|repeat>        Clamp mode (default: clamp)");
    }
}
