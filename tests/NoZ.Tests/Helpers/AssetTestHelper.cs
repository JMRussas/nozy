//  NoZ.Tests - Asset test helpers
//
//  Binary asset data builders and engine init/cleanup for tests.
//  Uses test mocks to avoid real GPU/audio/platform dependencies.

using System.Reflection;
using NoZ.Tests.Mocks;

namespace NoZ.Tests.Helpers;

public class AssetTestHelper : IDisposable
{
    public TestGraphicsDriver Graphics { get; }
    public TestPlatform Platform { get; }
    public TestAudioDriver Audio { get; }

    public AssetTestHelper()
    {
        Graphics = new TestGraphicsDriver();
        Platform = new TestPlatform();
        Audio = new TestAudioDriver();
    }

    public void InitEngine()
    {
        var config = new ApplicationConfig
        {
            Platform = Platform,
            AudioBackend = Audio,
            Graphics = new GraphicsConfig { Driver = Graphics },
            Vtable = new TestApplication(),
        };

        // Set Application.Platform so Graphics.ResetState() can read WindowSize
        typeof(Application)
            .GetProperty("Platform", BindingFlags.Public | BindingFlags.Static)!
            .SetValue(null, Platform);

        NoZ.Audio.Init(Audio);
        NoZ.Graphics.Init(config);
        Application.RegisterAssetTypes();
    }

    public void Dispose()
    {
        Asset.ClearRegistry();
        NoZ.Graphics.Shutdown();
        NoZ.Audio.Shutdown();
        Graphics.Reset();
        Audio.Reset();
    }

    /// Write a valid asset binary header to a stream.
    public static void WriteAssetHeader(BinaryWriter writer, AssetType type, ushort version, ushort flags = 0)
    {
        writer.Write(Constants.AssetSignature);
        writer.Write(type.Value);
        writer.Write(version);
        writer.Write(flags);
    }

    /// Create binary data for a Texture asset (header + format + filter + clamp + w + h + pixel data).
    public static byte[] CreateTextureBytes(int width = 4, int height = 4, TextureFormat format = TextureFormat.RGBA8, TextureFilter filter = TextureFilter.Linear)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        WriteAssetHeader(writer, AssetType.Texture, Texture.Version);
        writer.Write((byte)format);
        writer.Write((byte)filter);
        writer.Write((byte)TextureClamp.Clamp);
        writer.Write((uint)width);
        writer.Write((uint)height);
        var bpp = format switch
        {
            TextureFormat.RGBA8 => 4,
            TextureFormat.RGB8 => 3,
            TextureFormat.RG8 => 2,
            TextureFormat.R8 => 1,
            _ => 4
        };
        writer.Write(new byte[width * height * bpp]);
        return ms.ToArray();
    }

    /// Create binary data for a Shader asset.
    public static byte[] CreateShaderBytes(string source = "// test shader")
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        WriteAssetHeader(writer, AssetType.Shader, Shader.Version);
        var sourceBytes = System.Text.Encoding.UTF8.GetBytes(source);
        writer.Write((uint)sourceBytes.Length);
        writer.Write(sourceBytes);
        writer.Write((byte)ShaderFlags.Blend); // flags
        writer.Write((byte)0); // binding count
        writer.Write((uint)0); // vertex format hash
        return ms.ToArray();
    }

    /// Create binary data for a Sound asset.
    public static byte[] CreateSoundBytes(int sampleRate = 44100, int channels = 1, int bitsPerSample = 16, int dataSize = 100)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        WriteAssetHeader(writer, AssetType.Sound, Sound.Version);
        writer.Write(sampleRate);
        writer.Write(channels);
        writer.Write(bitsPerSample);
        writer.Write(dataSize);
        writer.Write(new byte[dataSize]);
        return ms.ToArray();
    }

    /// Create binary data for a minimal Font asset.
    public static byte[] CreateFontBytes(int fontSize = 16, int atlasWidth = 4, int atlasHeight = 4)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        WriteAssetHeader(writer, AssetType.Font, Font.Version);
        writer.Write((uint)fontSize);
        writer.Write((uint)atlasWidth);
        writer.Write((uint)atlasHeight);
        writer.Write(10.0f); // ascent
        writer.Write(-3.0f); // descent
        writer.Write(14.0f); // lineHeight
        writer.Write(10.0f); // baseline
        writer.Write(0.0f);  // internalLeading
        writer.Write((ushort)4); // familyName length
        writer.Write("Test".ToCharArray());
        writer.Write((ushort)0); // glyphCount
        writer.Write((ushort)0); // kerningCount
        writer.Write(new byte[atlasWidth * atlasHeight * 4]); // RGBA atlas data
        return ms.ToArray();
    }

    /// Register fake asset data on the test platform so Asset.Load can find it.
    public void RegisterAssetData(AssetType type, string name, byte[] data)
    {
        Platform.RegisterAssetData(type, name, data);
    }
}

internal class TestApplication : IApplication
{
    public void Update() { }
}
