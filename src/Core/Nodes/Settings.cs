using System;
using Godot;

namespace NotFireEmblem.Core;

public enum TextSpeed { Slow, Normal, Fast, Instant }

/// <summary>
/// Player preferences, saved to user://settings.cfg (separate from save games). Loaded on first use; Changed fires
/// after every change so the audio and screens can apply it at once.
/// </summary>
public static class Settings
{
    private const string Path = "user://settings.cfg";
    private static bool _loaded;
    private static int _music = 80;
    private static int _sfx = 80;
    private static TextSpeed _textSpeed = TextSpeed.Normal;
    private static bool _battleScenes = true;

    public static event Action? Changed;

    /// <summary>0–100.</summary>
    public static int MusicVolume { get { Load(); return _music; } set => Set(ref _music, Math.Clamp(value, 0, 100)); }

    /// <summary>0–100.</summary>
    public static int SfxVolume { get { Load(); return _sfx; } set => Set(ref _sfx, Math.Clamp(value, 0, 100)); }

    public static TextSpeed TextSpeed { get { Load(); return _textSpeed; } set => Set(ref _textSpeed, value); }

    /// <summary>Show the side-view battle screen for fights (the B key toggles it in battle too).</summary>
    public static bool BattleScenes { get { Load(); return _battleScenes; } set => Set(ref _battleScenes, value); }

    /// <summary>Characters per second for the dialogue typewriter.</summary>
    public static float CharsPerSecond => TextSpeed switch
    {
        TextSpeed.Slow => 30f,
        TextSpeed.Fast => 110f,
        TextSpeed.Instant => 100000f,
        _ => 55f,
    };

    private static void Set<T>(ref T field, T value)
    {
        Load();
        if (Equals(field, value))
            return;
        field = value;
        Save();
        Changed?.Invoke();
    }

    private static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;
        var file = new ConfigFile();
        if (file.Load(Path) != Error.Ok)
            return;
        _music = Math.Clamp((int)file.GetValue("audio", "music", _music), 0, 100);
        _sfx = Math.Clamp((int)file.GetValue("audio", "sfx", _sfx), 0, 100);
        _textSpeed = (TextSpeed)Math.Clamp((int)file.GetValue("game", "text_speed", (int)_textSpeed), 0, 3);
        _battleScenes = (bool)file.GetValue("game", "battle_scenes", _battleScenes);
    }

    private static void Save()
    {
        var file = new ConfigFile();
        file.SetValue("audio", "music", _music);
        file.SetValue("audio", "sfx", _sfx);
        file.SetValue("game", "text_speed", (int)_textSpeed);
        file.SetValue("game", "battle_scenes", _battleScenes);
        file.Save(Path);
    }
}
