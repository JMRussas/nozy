//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

using System.Text;
using System.Text.RegularExpressions;
using NoZ;

static class ShaderCompiler
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

        var flags = ShaderFlags.None;

        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--blend":
                    flags |= ShaderFlags.Blend;
                    break;
                case "--depth":
                    flags |= ShaderFlags.Depth;
                    break;
                case "--depth-less":
                    flags |= ShaderFlags.DepthLess;
                    break;
                case "--premultiplied":
                    flags |= ShaderFlags.PremultipliedAlpha;
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

        Compile(inputPath, outputPath, flags);
    }

    public static void Compile(string inputPath, string outputPath, ShaderFlags flags)
    {
        var wgslSource = File.ReadAllText(inputPath);
        var bindings = ParseWgslBindings(wgslSource);
        var vertexHash = ComputeVertexInputHash(wgslSource);

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var stream = File.Create(outputPath);
        using var writer = new BinaryWriter(stream);

        writer.WriteAssetHeader(AssetType.Shader, Shader.Version);

        var sourceBytes = Encoding.UTF8.GetBytes(wgslSource);
        writer.Write((uint)sourceBytes.Length);
        writer.Write(sourceBytes);
        writer.Write((byte)flags);
        writer.Write((byte)bindings.Count);
        foreach (var binding in bindings)
        {
            writer.Write((byte)binding.Binding);
            writer.Write((byte)binding.Type);
            writer.Write(binding.Name);
        }
        writer.Write(vertexHash);

        Console.WriteLine($"Compiled shader: {bindings.Count} bindings, vertexHash=0x{vertexHash:X8}, flags={flags} -> {outputPath}");
    }

    private static List<ShaderBinding> ParseWgslBindings(string wgslSource)
    {
        var bindingDict = new Dictionary<uint, ShaderBinding>();

        var bindingPattern = @"@group\s*\(\s*(\d+)\s*\)\s*@binding\s*\(\s*(\d+)\s*\)\s*var(?:<(\w+)>)?\s+(\w+)\s*:\s*([^;]+);";
        var matches = Regex.Matches(wgslSource, bindingPattern);

        foreach (Match match in matches)
        {
            var group = uint.Parse(match.Groups[1].Value);
            var binding = uint.Parse(match.Groups[2].Value);
            var storageClass = match.Groups[3].Value;
            var name = match.Groups[4].Value;
            var type = match.Groups[5].Value.Trim();

            ShaderBindingType bindingType;
            if (storageClass == "uniform" || type.Contains("uniform"))
                bindingType = ShaderBindingType.UniformBuffer;
            else if (type.Contains("texture_2d_array"))
                bindingType = ShaderBindingType.Texture2DArray;
            else if (type.Contains("texture_2d") || type.Contains("texture_cube"))
                bindingType = ShaderBindingType.Texture2D;
            else if (type.Contains("sampler"))
                bindingType = ShaderBindingType.Sampler;
            else
            {
                Console.Error.WriteLine($"Warning: Unknown WGSL binding type '{type}' for '{name}', assuming uniform buffer");
                bindingType = ShaderBindingType.UniformBuffer;
            }

            if (group == 0)
            {
                bindingDict[binding] = new ShaderBinding
                {
                    Binding = binding,
                    Type = bindingType,
                    Name = name
                };
            }
        }

        return bindingDict.Values.OrderBy(b => b.Binding).ToList();
    }

    private static uint ComputeVertexInputHash(string wgslSource)
    {
        var structMatch = Regex.Match(wgslSource, @"struct\s+VertexInput\s*\{([^}]+)\}");
        if (!structMatch.Success)
            return 0;

        var body = structMatch.Groups[1].Value;
        var locationPattern = @"@location\s*\(\s*(\d+)\s*\)\s+\w+\s*:\s*(\w+)";
        var matches = Regex.Matches(body, locationPattern);

        Span<(int location, int components, VertexAttribType type)> attrs =
            stackalloc (int, int, VertexAttribType)[matches.Count];

        for (int i = 0; i < matches.Count; i++)
        {
            var location = int.Parse(matches[i].Groups[1].Value);
            var wgslType = matches[i].Groups[2].Value;
            var (components, attribType) = MapWgslType(wgslType);
            attrs[i] = (location, components, attribType);
        }

        return VertexFormatHash.Compute(attrs);
    }

    private static (int components, VertexAttribType type) MapWgslType(string wgslType) => wgslType switch
    {
        "f32" => (1, VertexAttribType.Float),
        "i32" => (1, VertexAttribType.Int),
        "u32" => (1, VertexAttribType.Int),
        _ when wgslType.StartsWith("vec2") && wgslType.Contains("f32") => (2, VertexAttribType.Float),
        _ when wgslType.StartsWith("vec2") && wgslType.Contains("i32") => (2, VertexAttribType.Int),
        _ when wgslType.StartsWith("vec3") && wgslType.Contains("f32") => (3, VertexAttribType.Float),
        _ when wgslType.StartsWith("vec3") && wgslType.Contains("i32") => (3, VertexAttribType.Int),
        _ when wgslType.StartsWith("vec4") && wgslType.Contains("f32") => (4, VertexAttribType.Float),
        _ when wgslType.StartsWith("vec4") && wgslType.Contains("i32") => (4, VertexAttribType.Int),
        _ when wgslType.StartsWith("vec2") => (2, VertexAttribType.Float),
        _ when wgslType.StartsWith("vec3") => (3, VertexAttribType.Float),
        _ when wgslType.StartsWith("vec4") => (4, VertexAttribType.Float),
        _ => (1, VertexAttribType.Float),
    };

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: noz-compile shader <input.wgsl> <output> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --blend           Enable alpha blending");
        Console.WriteLine("  --depth           Enable depth testing");
        Console.WriteLine("  --depth-less      Enable depth-less comparison");
        Console.WriteLine("  --premultiplied   Enable premultiplied alpha");
    }
}
