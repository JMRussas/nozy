//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    PrintUsage();
    return 0;
}

var command = args[0];
var commandArgs = args[1..];

try
{
    switch (command.ToLowerInvariant())
    {
        case "texture":
            TextureCompiler.Run(commandArgs);
            break;
        case "shader":
            ShaderCompiler.Run(commandArgs);
            break;
        case "import":
            ImportCommand.Run(commandArgs);
            break;
        default:
            Console.Error.WriteLine($"Unknown command: {command}");
            PrintUsage();
            return 1;
    }
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("Usage: noz-compile <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  texture         Compile a PNG texture to noz binary format");
    Console.WriteLine("  shader          Compile a WGSL shader to noz binary format");
    Console.WriteLine("  import          Batch-compile all assets in a project directory");
    Console.WriteLine();
    Console.WriteLine("Run 'noz-compile <command> --help' for command-specific options.");
}
