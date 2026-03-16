//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

using NoZ;
using NoZ.Editor;
using NoZ.Editor.Msdf;

static class FontCompiler
{
    private const string DefaultCharacters = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    public static void Run(string[] args)
    {
        if (args.Length < 2 || args[0] is "-h" or "--help")
        {
            PrintUsage();
            return;
        }

        var inputPath = args[0];
        var outputPath = args[1];

        int fontSize = 48;
        string characters = DefaultCharacters;
        string ranges = "";
        float sdfRange = 4f;
        int padding = 1;
        bool symbol = false;

        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--size" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], out fontSize) || fontSize < 1)
                    {
                        Console.Error.WriteLine("Invalid font size");
                        return;
                    }
                    break;

                case "--charset" when i + 1 < args.Length:
                    var charset = args[++i].ToLowerInvariant();
                    characters = charset switch
                    {
                        "ascii" => DefaultCharacters,
                        "extended" => BuildExtendedCharset(),
                        "all" or "*" => "*",
                        _ => charset, // treat as literal character list
                    };
                    break;

                case "--ranges" when i + 1 < args.Length:
                    ranges = args[++i];
                    break;

                case "--sdf-range" when i + 1 < args.Length:
                    if (!float.TryParse(args[++i], out sdfRange) || sdfRange < 0)
                    {
                        Console.Error.WriteLine("Invalid SDF range");
                        return;
                    }
                    break;

                case "--padding" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], out padding) || padding < 0)
                    {
                        Console.Error.WriteLine("Invalid padding");
                        return;
                    }
                    break;

                case "--symbol":
                    symbol = true;
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

        if (!string.IsNullOrEmpty(ranges))
            characters = MergeCharacterRanges(ranges, characters);

        Compile(inputPath, outputPath, fontSize, characters, sdfRange, padding, symbol);
    }

    public static void Compile(
        string inputPath,
        string outputPath,
        int fontSize = 48,
        string? characters = null,
        float sdfRange = 4f,
        int padding = 1,
        bool symbol = false)
    {
        characters ??= DefaultCharacters;
        var importAll = characters == "*";

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var ttf = TrueTypeFont.Load(inputPath, fontSize, importAll ? null : characters);

        var glyphs = BuildGlyphList(ttf, characters, fontSize, sdfRange, padding, symbol);
        if (glyphs.Count == 0)
            throw new InvalidOperationException("No glyphs to compile");

        var (atlasSize, atlasUsage) = PackGlyphs(glyphs, fontSize, sdfRange, padding, symbol);

        unsafe
        {
            using var atlas = new PixelData<Color32>(atlasSize.X, atlasSize.Y);

            RenderGlyphs(glyphs, atlas, sdfRange, symbol, fontSize);

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            WriteFontData(outputPath, ttf, glyphs, atlas, atlasSize, fontSize, sdfRange, padding, symbol);
        }

        sw.Stop();
        var monoInfo = symbol ? $", symbol {fontSize}px" : "";
        Console.WriteLine($"Compiled font: {Path.GetFileName(inputPath)}: {glyphs.Count} glyphs, {atlasSize.X}x{atlasSize.Y} atlas ({atlasUsage:P0} used), size {fontSize}{monoInfo}, {sw.ElapsedMilliseconds}ms -> {outputPath}");
    }

    private struct ImportGlyph
    {
        public TrueTypeFont.Glyph Ttf;
        public Vector2Double Size;
        public Vector2Double Bearing;
        public double Advance;
        public double Scale;
        public Vector2Int PackedSize;
        public RectInt PackedRect;
        public char Codepoint;
    }

    private static List<ImportGlyph> BuildGlyphList(
        TrueTypeFont ttf, string characters, int fontSize, float sdfRange, int padding, bool symbol)
    {
        var glyphs = new List<ImportGlyph>();
        var importAll = characters == "*";
        double monoSize = symbol ? fontSize : 0;

        var source = importAll
            ? ttf.Glyphs
            : characters.Select(c => ttf.GetGlyph(c)).Where(g => g != null)!;

        foreach (var ttfGlyph in source)
        {
            if (ttfGlyph == null) continue;

            // Skip empty glyphs (space, etc.) for symbol fonts
            if (symbol && ttfGlyph!.contours is not { Length: > 0 })
                continue;

            if (symbol)
            {
                var maxDim = Math.Max(ttfGlyph.size.x, ttfGlyph.size.y);
                var scale = maxDim > 0 ? monoSize / maxDim : 1.0;

                var monoDim = (int)Math.Ceiling(monoSize + sdfRange * 2) + padding * 2;
                glyphs.Add(new ImportGlyph
                {
                    Codepoint = ttfGlyph.codepoint,
                    Ttf = ttfGlyph,
                    Scale = scale,
                    Size = new Vector2Double(monoSize + sdfRange * 2, monoSize + sdfRange * 2),
                    Bearing = new Vector2Double(-sdfRange, sdfRange),
                    Advance = monoSize,
                    PackedSize = new Vector2Int(monoDim, monoDim)
                });
            }
            else
            {
                glyphs.Add(new ImportGlyph
                {
                    Codepoint = ttfGlyph!.codepoint,
                    Ttf = ttfGlyph,
                    Scale = 1.0,
                    Size = ttfGlyph.size + new Vector2Double(sdfRange * 2, sdfRange * 2),
                    Bearing = new Vector2Double(
                        ttfGlyph.bearing.x - sdfRange,
                        ttfGlyph.size.y - ttfGlyph.bearing.y),
                    Advance = ttfGlyph.advance,
                    PackedSize = new Vector2Int(
                        (int)Math.Ceiling(ttfGlyph.size.x + sdfRange * 2 + padding * 2),
                        (int)Math.Ceiling(ttfGlyph.size.y + sdfRange * 2 + padding * 2)
                    )
                });
            }
        }

        return glyphs;
    }

    private static (Vector2Int size, float usage) PackGlyphs(
        List<ImportGlyph> glyphs, int fontSize, float sdfRange, int padding, bool symbol)
    {
        if (symbol)
            return PackGlyphsGrid(glyphs);

        var minHeight = (int)NextPowerOf2((uint)(fontSize + 2 + sdfRange * 2 + padding * 2));
        var packer = new RectPacker(minHeight, minHeight);

        while (packer.IsEmpty)
        {
            for (var i = 0; i < glyphs.Count; i++)
            {
                var glyph = glyphs[i];

                if (glyph.Ttf.contours == null || glyph.Ttf.contours.Length == 0)
                    continue;

                if (packer.Insert(glyph.PackedSize, out var packedRect) == -1)
                {
                    var size = packer.Size;
                    if (size.X <= size.Y)
                        packer.Resize(size.X * 2, size.Y);
                    else
                        packer.Resize(size.X, size.Y * 2);
                    break;
                }

                glyph.PackedRect = packedRect;
                glyphs[i] = glyph;
            }
        }

        var bounds = packer.UsedBounds;
        var trimmedSize = new Vector2Int(
            Math.Max(bounds.X, 1),
            Math.Max(bounds.Y, 1));

        long trimmedArea = (long)trimmedSize.X * trimmedSize.Y;
        long usedArea = 0;
        foreach (var glyph in glyphs)
        {
            if (glyph.Ttf.contours != null && glyph.Ttf.contours.Length > 0)
                usedArea += (long)glyph.PackedSize.X * glyph.PackedSize.Y;
        }
        float usage = trimmedArea > 0 ? (float)usedArea / trimmedArea : 0f;

        return (trimmedSize, usage);
    }

    private static (Vector2Int size, float usage) PackGlyphsGrid(List<ImportGlyph> glyphs)
    {
        var glyphCount = glyphs.Count(g => g.Ttf.contours is { Length: > 0 });
        if (glyphCount == 0)
            return (new Vector2Int(1, 1), 0f);

        var cellSize = glyphs.First(g => g.Ttf.contours is { Length: > 0 }).PackedSize;
        var cols = (int)Math.Ceiling(Math.Sqrt(glyphCount));
        var rows = (int)Math.Ceiling((double)glyphCount / cols);

        int col = 0, row = 0;
        for (var i = 0; i < glyphs.Count; i++)
        {
            var glyph = glyphs[i];
            if (glyph.Ttf.contours == null || glyph.Ttf.contours.Length == 0)
                continue;

            glyph.PackedRect = new RectInt(
                1 + col * cellSize.X,
                1 + row * cellSize.Y,
                cellSize.X,
                cellSize.Y);
            glyphs[i] = glyph;

            col++;
            if (col >= cols)
            {
                col = 0;
                row++;
            }
        }

        var atlasSize = new Vector2Int(
            2 + cols * cellSize.X,
            2 + rows * cellSize.Y);

        long totalArea = (long)atlasSize.X * atlasSize.Y;
        long usedArea = (long)glyphCount * cellSize.X * cellSize.Y;
        float usage = totalArea > 0 ? (float)usedArea / totalArea : 0f;

        return (atlasSize, usage);
    }

    private static void RenderGlyphs(
        List<ImportGlyph> glyphs, PixelData<Color32> atlas, float sdfRange, bool symbol, int fontSize)
    {
        var msdfAtlas = new MsdfBitmap(atlas.Width, atlas.Height);
        double monoSize = symbol ? fontSize : 0;

        foreach (var glyph in glyphs)
        {
            if (glyph.Ttf.contours == null || glyph.Ttf.contours.Length == 0)
                continue;

            var outputPosition = new Vector2Int(
                glyph.PackedRect.X + glyph.PackedSize.X - (glyph.PackedRect.Width),
                glyph.PackedRect.Y + glyph.PackedSize.Y - (glyph.PackedRect.Height)
            );

            // Undo padding to get actual render region
            var renderPos = new Vector2Int(
                glyph.PackedRect.X + (glyph.PackedSize.X - glyph.PackedRect.Width) / 2,
                glyph.PackedRect.Y + (glyph.PackedSize.Y - glyph.PackedRect.Height) / 2
            );

            // Use the packed rect position offset by padding for the render position
            outputPosition = new Vector2Int(
                glyph.PackedRect.X + (glyph.PackedSize.X > glyph.PackedRect.Width ? 1 : 0),
                glyph.PackedRect.Y + (glyph.PackedSize.Y > glyph.PackedRect.Height ? 1 : 0)
            );

            // Match the editor's render logic exactly
            var padding = (glyph.PackedSize.X - (int)Math.Ceiling(glyph.Size.x)) / 2;
            outputPosition = new Vector2Int(
                glyph.PackedRect.X + padding,
                glyph.PackedRect.Y + padding
            );

            var outputSize = new Vector2Int(
                glyph.PackedRect.Width - padding * 2,
                glyph.PackedRect.Height - padding * 2
            );

            var s = glyph.Scale;

            double centerX = 0, centerY = 0;
            if (symbol)
            {
                centerX = (monoSize - glyph.Ttf.size.x * s) / 2;
                centerY = (monoSize - glyph.Ttf.size.y * s) / 2;
            }

            var translate = new Vector2Double(
                -glyph.Ttf.bearing.x + (sdfRange + centerX) / s,
                -glyph.Ttf.bearing.y + glyph.Ttf.size.y + (sdfRange + centerY) / s
            );

            MsdfFont.RenderGlyph(
                glyph.Ttf,
                msdfAtlas,
                outputPosition,
                outputSize,
                sdfRange / (2.0 * s),
                new Vector2Double(s, s),
                translate
            );
        }

        // Convert float MSDF bitmap to Color32 atlas
        for (int y = 0; y < atlas.Height; y++)
        {
            for (int x = 0; x < atlas.Width; x++)
            {
                var px = msdfAtlas[x, y];
                atlas[x, y] = new Color32(
                    (byte)Math.Clamp(px[0] * 255f, 0f, 255f),
                    (byte)Math.Clamp(px[1] * 255f, 0f, 255f),
                    (byte)Math.Clamp(px[2] * 255f, 0f, 255f),
                    255);
            }
        }
    }

    private static void WriteFontData(
        string outputPath,
        TrueTypeFont ttf,
        List<ImportGlyph> glyphs,
        PixelData<Color32> atlas,
        Vector2Int atlasSize,
        int fontSize,
        float sdfRange,
        int padding,
        bool symbol)
    {
        var fontSizeInv = 1.0f / fontSize;

        using var writer = new BinaryWriter(File.Create(outputPath));

        writer.WriteAssetHeader(AssetType.Font, Font.Version);
        writer.Write((uint)fontSize);
        writer.Write((uint)atlasSize.X);
        writer.Write((uint)atlasSize.Y);

        if (symbol)
        {
            writer.Write(1.0f);  // ascent
            writer.Write(0.0f);  // descent
            writer.Write(1.0f);  // lineHeight
            writer.Write(1.0f);  // baseline
            writer.Write(0.0f);  // internalLeading
        }
        else
        {
            writer.Write((float)(ttf.Ascent * fontSizeInv));
            writer.Write((float)(ttf.Descent * fontSizeInv));
            writer.Write((float)(ttf.Height * fontSizeInv));
            writer.Write((float)(ttf.Ascent * fontSizeInv));
            writer.Write((float)(ttf.InternalLeading * fontSizeInv));
        }

        // Font family name
        writer.Write((ushort)ttf.FamilyName.Length);
        if (ttf.FamilyName.Length > 0)
            writer.Write(ttf.FamilyName.ToCharArray());

        // Write glyph count and data
        writer.Write((ushort)glyphs.Count);
        foreach (var glyph in glyphs)
        {
            writer.Write((uint)glyph.Codepoint);

            var hasContours = glyph.Ttf.contours != null && glyph.Ttf.contours.Length > 0;
            if (hasContours)
            {
                // UV coordinates (offset by Padding to exclude padding from sampling)
                writer.Write((float)(glyph.PackedRect.X + padding) / atlasSize.X);
                writer.Write((float)(glyph.PackedRect.Y + padding) / atlasSize.Y);
                writer.Write((float)(glyph.PackedRect.X + padding + glyph.Size.x) / atlasSize.X);
                writer.Write((float)(glyph.PackedRect.Y + padding + glyph.Size.y) / atlasSize.Y);

                // Size
                writer.Write((float)(glyph.Size.x * fontSizeInv));
                writer.Write((float)(glyph.Size.y * fontSizeInv));
            }
            else
            {
                writer.Write(0f);
                writer.Write(0f);
                writer.Write(0f);
                writer.Write(0f);
                writer.Write(0f);
                writer.Write(0f);
            }

            // Advance
            writer.Write((float)(glyph.Advance * fontSizeInv));

            // Bearing
            writer.Write((float)(glyph.Bearing.x * fontSizeInv));
            writer.Write((float)(glyph.Bearing.y * fontSizeInv));
        }

        // Write kerning data
        var kerning = ttf._kerning;
        var kerningCount = kerning?.Count ?? 0;
        writer.Write((ushort)kerningCount);

        if (kerning != null)
        {
            foreach (var (key, value) in kerning)
            {
                var left = (uint)(key >> 16);
                var right = (uint)(key & 0xFFFF);
                writer.Write(left);
                writer.Write(right);
                writer.Write(value * fontSizeInv);
            }
        }

        // Write atlas as RGBA32
        var atlasSpan = atlas.AsByteSpan();
        writer.Write(atlasSpan);

        // Build packed glyph name buffer using Unicode character names
        var nameBuffer = new System.Text.StringBuilder();
        var nameOffsets = new (ushort start, ushort length)[glyphs.Count];
        for (int i = 0; i < glyphs.Count; i++)
        {
            var name = UnicodeNames.GetName(glyphs[i].Codepoint) ?? glyphs[i].Ttf.name;
            if (!string.IsNullOrEmpty(name))
            {
                nameOffsets[i] = ((ushort)nameBuffer.Length, (ushort)name.Length);
                nameBuffer.Append(name);
            }
        }

        // Write glyph name section
        var nameChars = nameBuffer.ToString();
        writer.Write((ushort)nameChars.Length);
        if (nameChars.Length > 0)
            writer.Write(nameChars.ToCharArray());

        for (int i = 0; i < glyphs.Count; i++)
        {
            writer.Write(nameOffsets[i].start);
            writer.Write(nameOffsets[i].length);
        }
    }

    private static string MergeCharacterRanges(string rangesStr, string existingChars)
    {
        var chars = new HashSet<char>(existingChars);
        foreach (var range in rangesStr.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = range.Split('-', 2);
            if (parts.Length == 2)
            {
                var start = Convert.ToInt32(parts[0].Trim(), 16);
                var end = Convert.ToInt32(parts[1].Trim(), 16);
                for (int c = start; c <= end && c <= 0xFFFF; c++)
                    chars.Add((char)c);
            }
            else if (parts.Length == 1)
            {
                var c = Convert.ToInt32(parts[0].Trim(), 16);
                if (c <= 0xFFFF)
                    chars.Add((char)c);
            }
        }
        return new string(chars.OrderBy(c => c).ToArray());
    }

    private static string BuildExtendedCharset()
    {
        // ASCII + Latin-1 Supplement (covers Western European languages)
        var chars = new HashSet<char>();
        for (int c = 0x20; c <= 0x7E; c++) chars.Add((char)c);   // Basic ASCII
        for (int c = 0xA0; c <= 0xFF; c++) chars.Add((char)c);   // Latin-1 Supplement
        return new string(chars.OrderBy(c => c).ToArray());
    }

    private static uint NextPowerOf2(uint v)
    {
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v++;
        return v;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: noz-compile font <input.ttf|otf> <output> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --size <N>            Font size in pixels (default: 48)");
        Console.WriteLine("  --charset <set>       Character set: ascii, extended, all (default: ascii)");
        Console.WriteLine("  --ranges <hex-ranges> Unicode ranges, e.g. \"E000-E0FF,F000-F0FF\"");
        Console.WriteLine("  --sdf-range <N>       SDF range in pixels (default: 4)");
        Console.WriteLine("  --padding <N>         Atlas padding in pixels (default: 1)");
        Console.WriteLine("  --symbol              Symbol mode: uniform glyph cells, no metrics");
    }
}
