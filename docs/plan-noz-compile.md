# noz-compile: Standalone Asset Compiler CLI

## Overview
New console project at `tools/noz-compile/` that compiles PNG textures and WGSL shaders to noz binary format without requiring the editor GUI. References only engine + CLI platform + ImageSharp.

## Command Interface
```
noz-compile texture input.png output [--filter linear|point] [--format rgba8|r8|rg8|rgb8] [--clamp clamp|repeat]
noz-compile shader input.wgsl output [--blend] [--depth] [--depth-less] [--premultiplied]
noz-compile import <project-dir> [--clean] [--verbose] [--dry-run]
```

## Binary Formats

### Asset Header (12 bytes)
- `uint` AssetSignature = 0x4E4F5A41 ("NOZA")
- `uint` AssetType FourCC (TEXR, SHDR)
- `ushort` version
- `ushort` flags

### Texture (after header)
- `byte` TextureFormat (RGBA8=0, RGB8=1, R8=2, RG8=3, RGBA32F=4, BGRA8=5)
- `byte` TextureFilter (Point=0, Linear=1)
- `byte` TextureClamp (Repeat=0, Clamp=1)
- `uint` width, `uint` height
- `byte[width*height*bpp]` pixel data

### Shader (after header)
- `uint` source length
- `byte[sourceLength]` UTF8 WGSL source
- `byte` ShaderFlags
- `byte` binding count
- Per binding: `byte` index, `byte` type, `string` name (length-prefixed)
- `uint` vertex format hash

## Phases

### Phase 1: Core + Texture Compiler
- `NozCompile.csproj` — references Engine, CLI platform, ImageSharp 3.1.12
- `Program.cs` — entry point using CommandLineApplication.Run
- `TextureCompiler.cs` — PNG → binary via Image.Load<Rgba32> + WriteAssetHeader
- Add `InternalsVisibleTo("noz-compile")` to engine/src/Constants.cs

### Phase 2: Shader Compiler
- `ShaderCompiler.cs` — WGSL → binary with binding parser regex + VertexFormatHash.Compute

### Phase 3: Batch Import
- `ImportCommand.cs` — reads editor.cfg, walks source dirs, compiles all assets
- Timestamp comparison (skip if output newer than source + meta)
- Meta files for per-asset options (filter, blend, etc.)

### Phase 4 (Future): Font Compilation
Deferred — requires glyph rasterization, atlas packing, SDF generation.

## Dependencies
| Dependency | Source | Reason |
|---|---|---|
| NoZ.csproj | ProjectReference | AssetType, WriteAssetHeader, TextureFormat, ShaderFlags |
| NoZ.CLI.csproj | ProjectReference | CommandLineApplication, NullPlatform |
| SixLabors.ImageSharp 3.1.12 | NuGet | PNG decoding |

## Files
| File | Purpose |
|---|---|
| tools/noz-compile/NozCompile.csproj | Project file |
| tools/noz-compile/Program.cs | Entry point |
| tools/noz-compile/TextureCompiler.cs | PNG to noz texture |
| tools/noz-compile/ShaderCompiler.cs | WGSL to noz shader |
| tools/noz-compile/ImportCommand.cs | Batch import |
| engine/src/Constants.cs | Add InternalsVisibleTo |
| noz.sln | Add project reference |

## Reference Files
- editor/src/document/TextureDocument.cs — texture binary format (lines 216-247)
- editor/src/document/ShaderDocument.cs — shader format, WGSL binding parser
- platform/cli/CommandLineApplication.cs — CLI framework
- engine/src/Constants.cs — InternalsVisibleTo
- editor/src/Importer.cs — timestamp skip logic
