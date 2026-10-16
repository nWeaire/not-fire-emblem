using System.Collections.Generic;
using Godot;
using NotFireEmblem.Core;

namespace NotFireEmblem.Audio;

/// <summary>
/// Autoload that plays the game's music and sound effects by id: assets/audio/music/&lt;id&gt;.ogg (looping, with a
/// crossfade between tracks) and assets/audio/sfx/&lt;id&gt;.wav. Missing files are skipped quietly. Menu and cursor
/// sounds follow the confirm / cancel / cursor input actions, so screens don't each have to play them.
/// The audio is rendered by tools/audio/build.py.
/// </summary>
public partial class AudioDirector : Node
{
    public const string MusicRoot = "res://assets/audio/music";
    public const string SfxRoot = "res://assets/audio/sfx";
    private const float MusicDb = -9f;   // the tracks are mastered loud; sit them under the effects
    private const float SfxDb = -4f;
    private const float FadeSeconds = 0.6f;
    private const int SfxVoices = 8;

    public static AudioDirector? Instance { get; private set; }

    private readonly AudioStreamPlayer[] _music = new AudioStreamPlayer[2];
    private readonly List<AudioStreamPlayer> _sfx = new();
    private readonly Dictionary<string, AudioStream?> _cache = new();
    private int _current;
    private string? _musicId;
    private ulong _lastCursorMs;

    /// <summary>The track playing now (null when silent).</summary>
    public string? MusicId => _musicId;

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        // Release the cached streams, or Godot reports them as leaked at exit.
        foreach (var player in GetChildren())
            if (player is AudioStreamPlayer p)
            {
                p.Stop();
                p.Stream = null;
            }
        // C# holds its own handle on each cached stream until the garbage collector runs; release them now.
        foreach (var stream in _cache.Values)
            stream?.Dispose();
        _cache.Clear();
        if (Instance == this)
            Instance = null;
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        for (int i = 0; i < 2; i++)
        {
            _music[i] = new AudioStreamPlayer { VolumeDb = -80f };
            AddChild(_music[i]);
        }
        for (int i = 0; i < SfxVoices; i++)
        {
            var player = new AudioStreamPlayer { VolumeDb = SfxDb };
            AddChild(player);
            _sfx.Add(player);
        }
    }

    /// <summary>Plays a track (looping unless it's a jingle), crossfading from the current one. Same id: no restart.</summary>
    public static void PlayMusic(string id, bool loop = true) => Instance?.Music(id, loop);

    public static void StopMusic() => Instance?.Music(null, false);

    public static void PlaySfx(string id, float pitch = 1f) => Instance?.Sfx(id, pitch);

    private void Music(string? id, bool loop)
    {
        if (id == _musicId && id is not null && _music[_current].Playing)
            return;
        _musicId = id;
        var old = _music[_current];
        FadeTo(old, -80f, stopAfter: true);
        if (id is null || Load($"{MusicRoot}/{id}.ogg") is not AudioStreamOggVorbis stream)
            return;
        // Jingles must not loop; tracks must, whatever their import setting says. (Set on the shared, cached
        // stream: a duplicate per play left a C# handle Godot reported as leaked at exit.)
        stream.Loop = loop;
        _current = 1 - _current;
        var next = _music[_current];
        next.Stream = stream;
        next.VolumeDb = -40f;
        next.Play();
        FadeTo(next, MusicDb, stopAfter: false);
    }

    private void FadeTo(AudioStreamPlayer player, float db, bool stopAfter)
    {
        if (!player.Playing)
            return;
        var tween = CreateTween();
        tween.TweenProperty(player, "volume_db", db, FadeSeconds);
        if (stopAfter)
            tween.TweenCallback(Callable.From(player.Stop));
    }

    private void Sfx(string id, float pitch)
    {
        if (Load($"{SfxRoot}/{id}.wav") is not { } stream)
            return;
        var player = _sfx.Find(p => !p.Playing) ?? _sfx[0];
        player.Stream = stream;
        player.PitchScale = pitch;
        player.Play();
    }

    private AudioStream? Load(string path)
    {
        if (_cache.TryGetValue(path, out var cached))
            return cached;
        var stream = ResourceLoader.Exists(path) ? ResourceLoader.Load<AudioStream>(path) : null;
        if (stream is null)
            GD.Print($"Audio: no file for {path}");
        _cache[path] = stream;
        return stream;
    }

    public override void _Input(InputEvent e)
    {
        if (e.IsEcho())
            return;
        if (e.IsActionPressed(InputSetup.Confirm))
            Sfx("confirm", 1f);
        else if (e.IsActionPressed(InputSetup.Cancel))
            Sfx("cancel", 1f);
        else if (e.IsActionPressed(InputSetup.CursorUp) || e.IsActionPressed(InputSetup.CursorDown)
                 || e.IsActionPressed(InputSetup.CursorLeft) || e.IsActionPressed(InputSetup.CursorRight))
        {
            // Held keys repeat quickly; keep the tick from buzzing.
            ulong now = Time.GetTicksMsec();
            if (now - _lastCursorMs > 60)
                Sfx("cursor", 1f);
            _lastCursorMs = now;
        }
    }
}
