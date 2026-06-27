using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace PitLife.Core;

public sealed class AudioSystem : IDisposable
{
    private readonly Dictionary<string, SoundEffect> _sounds = new(StringComparer.Ordinal);
    private bool _audioEnabled = true;

    public bool Enabled
    {
        get => _audioEnabled;
        set { _audioEnabled = value; StopAll(); }
    }

    public void LoadTone(string key, float frequency, float duration, float volume = 0.3f)
    {
        ArgumentNullException.ThrowIfNull(key);
        var sampleRate = 44100;
        var samples = (int)(sampleRate * duration);
        var buffer = new byte[samples * 2]; // 16-bit mono

        for (var i = 0; i < samples; i++)
        {
            var t = (float)i / sampleRate;
            var envelope = 1f - (t / duration);
            var value = (short)(MathF.Sin(2f * MathF.PI * frequency * t) * envelope * volume * 32767f);
            buffer[i * 2] = (byte)(value & 0xFF);
            buffer[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        _sounds[key] = new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
    }

    public void LoadClick(string key, float volume = 0.4f)
    {
        ArgumentNullException.ThrowIfNull(key);
        var sampleRate = 44100;
        var samples = 800; // ~18ms click
        var buffer = new byte[samples * 2];

        for (var i = 0; i < samples; i++)
        {
            var t = (float)i / sampleRate;
            var envelope = MathF.Exp(-t * 200f);
            var noise = (float)new Random((int)(t * 1000000)).NextDouble() * 2f - 1f;
            var value = (short)(noise * envelope * volume * 32767f);
            buffer[i * 2] = (byte)(value & 0xFF);
            buffer[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        _sounds[key] = new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
    }

    public void Initialize()
    {
        // UI sounds
        LoadClick("click");
        LoadClick("hover");
        LoadTone("btn_tone", 800f, 0.05f, 0.2f);

        // Creature sounds
        LoadTone("birth", 600f, 0.15f, 0.3f);
        LoadTone("death", 200f, 0.3f, 0.4f);
        LoadTone("predation", 300f, 0.2f, 0.35f);

        // Cataclysm sounds
        LoadTone("asteroid", 50f, 0.5f, 0.5f);
        LoadTone("volcano", 80f, 0.4f, 0.45f);
        LoadTone("earthquake", 30f, 0.4f, 0.5f);
    }

    public void Play(string key, float volume = 1f)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!_audioEnabled || !_sounds.TryGetValue(key, out var effect)) return;
        try
        {
            var instance = effect.CreateInstance();
            instance.Volume = Math.Clamp(volume, 0f, 1f);
            instance.Play();
        }
        catch { /* audio device not available */ }
    }

    private void StopAll()
    {
        foreach (var sound in _sounds.Values)
            sound.Dispose();
        _sounds.Clear();
    }

    public void Dispose()
    {
        StopAll();
    }
}
