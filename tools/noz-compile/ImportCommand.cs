//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

using NoZ;

static class ImportCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 1 || args[0] is "-h" or "--help")
        {
            PrintUsage();
            return;
        }

        var projectDir = Path.GetFullPath(args[0]);
        var clean = false;
        var verbose = false;
        var dryRun = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--clean":
                    clean = true;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                default:
                    Console.Error.WriteLine($"Unknown option: {args[i]}");
                    return;
            }
        }

        if (!Directory.Exists(projectDir))
        {
            Console.Error.WriteLine($"Project directory not found: {projectDir}");
            return;
        }

        // Resolve paths: project assets/, project library/, and engine/assets/shader/
        var assetsDir = Path.Combine(projectDir, "assets");
        var outputDir = Path.Combine(projectDir, "library");
        var engineShaderDir = FindEngineShaderDir(projectDir);

        // If editor.cfg exists at or above the project dir, use its config for
        // source paths and output path instead of the defaults
        var configDir = FindConfigDir(projectDir);
        if (configDir != null)
        {
            var props = PropertySet.LoadFile(Path.Combine(configDir, "editor.cfg"));
            if (props != null)
            {
                outputDir = ResolvePath(configDir, props.GetString("editor", "output_path", "./library"));

                // Build source list from editor.cfg, resolving relative to config dir
                var configSources = props.GetKeys("source")
                    .Select(p => ResolvePath(configDir, p))
                    .ToList();

                // Always include the project's own assets/ dir if not already covered
                var projectAssetsNorm = Path.GetFullPath(assetsDir);
                if (!configSources.Any(s => Path.GetFullPath(s).Equals(projectAssetsNorm, StringComparison.OrdinalIgnoreCase)))
                    configSources.Insert(0, assetsDir);

                // Find engine shader dir from config sources
                if (engineShaderDir == null)
                {
                    foreach (var src in configSources)
                    {
                        var shaderDir = Path.Combine(src, "shader");
                        if (Directory.Exists(shaderDir))
                        {
                            engineShaderDir = src;
                            break;
                        }
                    }
                }

                assetsDir = projectAssetsNorm;
            }
        }

        // Collect all source directories to scan
        var sourceDirs = new List<string>();

        if (Directory.Exists(assetsDir))
            sourceDirs.Add(assetsDir);

        // Add engine/assets/ for shaders (the whole dir, shader walking filters to .wgsl)
        if (engineShaderDir != null && Directory.Exists(engineShaderDir))
        {
            var norm = Path.GetFullPath(engineShaderDir);
            if (!sourceDirs.Any(s => Path.GetFullPath(s).Equals(norm, StringComparison.OrdinalIgnoreCase)))
                sourceDirs.Add(norm);
        }

        if (sourceDirs.Count == 0)
        {
            Console.Error.WriteLine($"No asset directories found for: {projectDir}");
            return;
        }

        if (verbose)
        {
            Console.WriteLine($"Project: {projectDir}");
            Console.WriteLine($"Output:  {outputDir}");
            foreach (var sp in sourceDirs)
                Console.WriteLine($"Source:  {sp}");
            Console.WriteLine();
        }

        var stats = new ImportStats();

        foreach (var sourcePath in sourceDirs)
        {
            // Walk for .png textures
            foreach (var file in Directory.EnumerateFiles(sourcePath, "*.png", SearchOption.AllDirectories))
            {
                var name = MakeCanonicalName(file);
                var targetPath = Path.Combine(outputDir, "texture", name);
                ImportTexture(file, targetPath, clean, verbose, dryRun, ref stats);
            }

            // Walk for .wgsl shaders
            foreach (var file in Directory.EnumerateFiles(sourcePath, "*.wgsl", SearchOption.AllDirectories))
            {
                var name = MakeCanonicalName(file);
                var targetPath = Path.Combine(outputDir, "shader", name);
                ImportShader(file, targetPath, clean, verbose, dryRun, ref stats);
            }

            // Walk for .ttf fonts
            foreach (var file in Directory.EnumerateFiles(sourcePath, "*.ttf", SearchOption.AllDirectories))
            {
                var name = MakeCanonicalName(file);
                var targetPath = Path.Combine(outputDir, "font", name);
                ImportFont(file, targetPath, clean, verbose, dryRun, ref stats);
            }

            // Walk for .otf fonts
            foreach (var file in Directory.EnumerateFiles(sourcePath, "*.otf", SearchOption.AllDirectories))
            {
                var name = MakeCanonicalName(file);
                var targetPath = Path.Combine(outputDir, "font", name);
                ImportFont(file, targetPath, clean, verbose, dryRun, ref stats);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Import complete: {stats.Compiled} compiled, {stats.Skipped} up-to-date, {stats.Failed} failed");
    }

    private static void ImportTexture(
        string sourcePath, string targetPath, bool clean, bool verbose, bool dryRun, ref ImportStats stats)
    {
        if (!clean && IsUpToDate(sourcePath, targetPath))
        {
            stats.Skipped++;
            if (verbose)
                Console.WriteLine($"  skip texture  {Path.GetFileName(sourcePath)}");
            return;
        }

        if (verbose || dryRun)
            Console.WriteLine($"  {(dryRun ? "would compile" : "compile")} texture  {Path.GetFileName(sourcePath)} -> {targetPath}");

        if (dryRun)
        {
            stats.Compiled++;
            return;
        }

        try
        {
            // Read .meta file for per-asset options
            var meta = LoadMeta(sourcePath);
            var filterStr = meta?.GetString("texture", "filter", "linear") ?? "linear";
            var clampStr = meta?.GetString("texture", "clamp", "clamp") ?? "clamp";
            var formatStr = meta?.GetString("texture", "format", "rgba8") ?? "rgba8";

            var filter = filterStr is "point" or "nearest" ? TextureFilter.Point : TextureFilter.Linear;
            var clamp = clampStr == "repeat" ? TextureClamp.Repeat : TextureClamp.Clamp;
            var format = formatStr switch
            {
                "r8" => TextureFormat.R8,
                "rg8" => TextureFormat.RG8,
                "rgb8" => TextureFormat.RGB8,
                _ => TextureFormat.RGBA8,
            };

            TextureCompiler.Compile(sourcePath, targetPath, format, filter, clamp);
            stats.Compiled++;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  FAILED texture {Path.GetFileName(sourcePath)}: {ex.Message}");
            stats.Failed++;
        }
    }

    private static void ImportShader(
        string sourcePath, string targetPath, bool clean, bool verbose, bool dryRun, ref ImportStats stats)
    {
        if (!clean && IsUpToDate(sourcePath, targetPath))
        {
            stats.Skipped++;
            if (verbose)
                Console.WriteLine($"  skip shader   {Path.GetFileName(sourcePath)}");
            return;
        }

        if (verbose || dryRun)
            Console.WriteLine($"  {(dryRun ? "would compile" : "compile")} shader   {Path.GetFileName(sourcePath)} -> {targetPath}");

        if (dryRun)
        {
            stats.Compiled++;
            return;
        }

        try
        {
            // Read .meta file for shader flags
            var meta = LoadMeta(sourcePath);
            var flags = ShaderFlags.None;
            if (meta != null)
            {
                if (meta.GetBool("shader", "blend", false)) flags |= ShaderFlags.Blend;
                if (meta.GetBool("shader", "depth", false)) flags |= ShaderFlags.Depth;
                if (meta.GetBool("shader", "depth_less", false)) flags |= ShaderFlags.DepthLess;
                if (meta.GetBool("shader", "premultiplied", false)) flags |= ShaderFlags.PremultipliedAlpha;
            }

            ShaderCompiler.Compile(sourcePath, targetPath, flags);
            stats.Compiled++;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  FAILED shader {Path.GetFileName(sourcePath)}: {ex.Message}");
            stats.Failed++;
        }
    }

    private static void ImportFont(
        string sourcePath, string targetPath, bool clean, bool verbose, bool dryRun, ref ImportStats stats)
    {
        if (!clean && IsUpToDate(sourcePath, targetPath))
        {
            stats.Skipped++;
            if (verbose)
                Console.WriteLine($"  skip font     {Path.GetFileName(sourcePath)}");
            return;
        }

        if (verbose || dryRun)
            Console.WriteLine($"  {(dryRun ? "would compile" : "compile")} font     {Path.GetFileName(sourcePath)} -> {targetPath}");

        if (dryRun)
        {
            stats.Compiled++;
            return;
        }

        try
        {
            // Read .meta file for font options
            var meta = LoadMeta(sourcePath);
            var fontSize = meta?.GetInt("font", "size", 48) ?? 48;
            var characters = meta?.GetString("font", "characters", "");
            var ranges = meta?.GetString("font", "ranges", "");
            var sdfRange = meta?.GetFloat("sdf", "range", 4f) ?? 4f;
            var padding = meta?.GetInt("font", "padding", 1) ?? 1;
            var symbol = meta?.GetBool("font", "symbol", false) ?? false;

            // Merge ranges into characters if specified
            string? finalCharacters = string.IsNullOrEmpty(characters) ? null : characters;
            if (!string.IsNullOrEmpty(ranges))
            {
                var baseChars = finalCharacters ?? " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
                finalCharacters = MergeRanges(ranges, baseChars);
            }

            FontCompiler.Compile(sourcePath, targetPath, fontSize, finalCharacters, sdfRange, padding, symbol);
            stats.Compiled++;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  FAILED font {Path.GetFileName(sourcePath)}: {ex.Message}");
            stats.Failed++;
        }
    }

    private static string MergeRanges(string rangesStr, string existingChars)
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

    private static bool IsUpToDate(string sourcePath, string targetPath)
    {
        if (!File.Exists(targetPath))
            return false;

        var targetTime = File.GetLastWriteTimeUtc(targetPath);
        var sourceTime = File.GetLastWriteTimeUtc(sourcePath);

        if (sourceTime > targetTime)
            return false;

        // Also check .meta file timestamp
        var metaPath = sourcePath + ".meta";
        if (File.Exists(metaPath))
        {
            var metaTime = File.GetLastWriteTimeUtc(metaPath);
            if (metaTime > targetTime)
                return false;
        }

        return true;
    }

    private static PropertySet? LoadMeta(string sourcePath)
    {
        var metaPath = sourcePath + ".meta";
        return PropertySet.LoadFile(metaPath);
    }

    private static string MakeCanonicalName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return name.ToLowerInvariant()
            .Replace('/', '_')
            .Replace('\\', '_')
            .Replace(' ', '_');
    }

    /// <summary>
    /// Find engine/assets/shader/ by walking up from the project directory
    /// looking for an engine/ directory that contains assets/shader/.
    /// </summary>
    private static string? FindEngineShaderDir(string startDir)
    {
        var dir = startDir;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "engine", "assets");
            if (Directory.Exists(Path.Combine(candidate, "shader")))
                return candidate;

            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }

    private static string? FindConfigDir(string startDir)
    {
        var dir = startDir;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "editor.cfg")))
                return dir;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }

    private static string ResolvePath(string baseDir, string path)
    {
        if (Path.IsPathRooted(path))
            return path;
        return Path.GetFullPath(Path.Combine(baseDir, path));
    }

    private struct ImportStats
    {
        public int Compiled;
        public int Skipped;
        public int Failed;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: noz-compile import <project-dir> [options]");
        Console.WriteLine();
        Console.WriteLine("Batch-compiles all assets in the project's source directories.");
        Console.WriteLine("Walks <project-dir>/assets/ for .png, .wgsl, .ttf, and .otf files, and");
        Console.WriteLine("engine/assets/shader/ for engine shaders. Output goes to");
        Console.WriteLine("<project-dir>/library/. If editor.cfg is found, uses its");
        Console.WriteLine("source paths and output path configuration.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --clean      Recompile everything regardless of timestamps");
        Console.WriteLine("  --verbose    Print each file being compiled or skipped");
        Console.WriteLine("  --dry-run    Show what would be compiled without writing files");
    }
}
