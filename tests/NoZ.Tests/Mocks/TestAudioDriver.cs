//  NoZ.Tests - Mock IAudioDriver
//
//  Returns incrementing nint handles for CreateSound.
//  Tracks Create/Destroy calls for test assertions.

using NoZ.Platform;

namespace NoZ.Tests.Mocks;

public class TestAudioDriver : IAudioDriver
{
    private nint _nextHandle = 1;

    public List<nint> CreatedSounds { get; } = [];
    public List<nint> DestroyedSounds { get; } = [];

    public float MasterVolume { get; set; } = 1.0f;
    public float SoundVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 1.0f;

    public void Init() { }
    public void Shutdown() { }

    public nint CreateSound(ReadOnlySpan<byte> pcmData, int sampleRate, int channels, int bitsPerSample)
    {
        var handle = _nextHandle++;
        CreatedSounds.Add(handle);
        return handle;
    }

    public void DestroySound(nint handle) => DestroyedSounds.Add(handle);

    public ulong Play(nint sound, float volume, float pitch, bool loop) => 0;
    public void Stop(ulong handle) { }
    public bool IsPlaying(ulong handle) => false;
    public void SetVolume(ulong handle, float volume) { }
    public void SetPitch(ulong handle, float pitch) { }
    public float GetVolume(ulong handle) => 1.0f;
    public float GetPitch(ulong handle) => 1.0f;
    public void PlayMusic(nint sound) { }
    public void StopMusic() { }
    public bool IsMusicPlaying() => false;

    public void Reset()
    {
        CreatedSounds.Clear();
        DestroyedSounds.Clear();
    }
}
