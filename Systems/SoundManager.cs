using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace StormShooter;

public static class SoundManager
{
    private static readonly Dictionary<string, SoundEffect> _sounds = new();
    private static readonly Random _rng = new();

    public static void Load(ContentManager content)
    {
        TryLoad(content, "pistolshot", "Sounds/pistolshot");
        TryLoad(content, "akreload", "Sounds/akreload");
        TryLoad(content, "dry_fire", "Sounds/dry_fire");
        TryLoad(content, "humanhit1", "Sounds/humanhit1");
        TryLoad(content, "unload", "Sounds/unload");
        TryLoad(content, "snowstep1", "Sounds/snowstep1");
        TryLoad(content, "snowstep2", "Sounds/snowstep2");
        TryLoad(content, "snowstep3", "Sounds/snowstep3");
    }

    public static void Play(string name, float volume = 1f, float pitch = 0f, float pan = 0f)
    {
        if (_sounds.TryGetValue(name, out var sfx))
            sfx.Play(volume, pitch, pan);
    }

    public static void PlayRandom(float volume = 1f, float pitch = 0f, params string[] names)
    {
        if (names.Length == 0) return;
        Play(names[_rng.Next(names.Length)], volume, pitch);
    }

    private static void TryLoad(ContentManager content, string key, string path)
    {
        try { _sounds[key] = content.Load<SoundEffect>(path); }
        catch { }
    }
}
