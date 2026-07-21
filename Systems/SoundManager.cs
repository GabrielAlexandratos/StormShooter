using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace StormShooter;

public static class SoundManager
{
    private static readonly Dictionary<string, SoundEffect> _sounds = new();
    private static readonly Dictionary<string, Song> _songs = new();
    private static readonly Random _rng = new();

    public static void Load(ContentManager content)
    {
        TryLoad(content, "pistolshot", "Sounds/pistolshot");
        TryLoad(content, "akreload", "Sounds/akreload");
        TryLoad(content, "dry_fire", "Sounds/dry_fire");
        TryLoad(content, "humanhit1", "Sounds/humanhit1");
        TryLoad(content, "unload", "Sounds/unload");
        TryLoad(content, "equip", "Sounds/equip");
        TryLoad(content, "snowstep1", "Sounds/snowstep1");
        TryLoad(content, "snowstep2", "Sounds/snowstep2");
        TryLoad(content, "snowstep3", "Sounds/snowstep3");
        TryLoad(content, "snowimpact1", "Sounds/snowimpact1");
        TryLoad(content, "snowimpact2", "Sounds/snowimpact2");

        TryLoadSong(content, "wind_ambience", "Sounds/wind_ambience");
        TryLoadSong(content, "light_rain", "Sounds/light_rain");
        TryLoadSong(content, "heavy_rain", "Sounds/heavy_rain");
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

    public static void PlayAmbience(string name, float volume = 1f)
    {
        if (!_songs.TryGetValue(name, out var song) || song == null) return;
        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = volume - 0.25f;
        if (MediaPlayer.Queue.ActiveSong == song && MediaPlayer.State == MediaState.Playing) return;
        MediaPlayer.Play(song);
    }

    public static void StopAmbience()
    {
        if (MediaPlayer.State != MediaState.Stopped)
            MediaPlayer.Stop();
    }

    private static void TryLoad(ContentManager content, string key, string path)
    {
        try { _sounds[key] = content.Load<SoundEffect>(path); }
        catch { }
    }

    private static void TryLoadSong(ContentManager content, string key, string path)
    {
        try { _songs[key] = content.Load<Song>(path); }
        catch { }
    }
}
