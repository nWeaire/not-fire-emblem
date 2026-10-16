using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace NotFireEmblem.Units;

/// <summary>
/// Builds SpriteFrames from the PNG frame folders written by tools/art (assets/sprites/&lt;set&gt;/&lt;id&gt;/animations/&lt;anim&gt;/&lt;dir&gt;/).
/// Results are cached; a missing art set returns null so callers can fall back to placeholders.
/// </summary>
public static class SpriteLibrary
{
    public const string BattleRoot = "res://assets/sprites/battle";
    public const string MapRoot = "res://assets/sprites/units";

    /// <summary>Map directions, as named by the art folders.</summary>
    public static readonly string[] MapDirections = { "south", "east", "north", "west" };

    /// <summary>Map animations, their playback speed (fps) and whether they loop. Frames are named "&lt;anim&gt;_&lt;dir&gt;".</summary>
    public static readonly (string Name, double Fps, bool Loop)[] MapAnimations =
    {
        ("idle", 5, true),
        ("walk", 10, true),
        ("attack", 14, false),
        ("hit", 12, false),
    };

    /// <summary>Battle-screen animations, their playback speed (fps) and whether they loop.</summary>
    public static readonly (string Name, double Fps, bool Loop)[] BattleAnimations =
    {
        ("idle", 6, true),
        ("attack", 12, false),
        ("critical", 12, false),
        ("dodge", 12, false),
        ("hit", 10, false),
    };

    private static readonly Dictionary<string, SpriteFrames?> Cache = new();

    /// <summary>Battle frames for the unit's current outfit (see BattleUnit.SpriteCandidates), or null.</summary>
    public static SpriteFrames? Battle(BattleUnit unit) =>
        unit.SpriteCandidates().Select(Battle).FirstOrDefault(f => f is not null);

    /// <summary>
    /// Side-view battle frames (facing east) for a sprite id, or null if it has no attack art yet.
    /// Missing animations fall back: idle → the east rotation, critical → attack, dodge/hit → idle.
    /// </summary>
    public static SpriteFrames? Battle(string spriteId)
    {
        string key = $"battle:{spriteId}";
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        var loaded = new Dictionary<string, List<Texture2D>>();
        foreach (var (name, _, _) in BattleAnimations)
        {
            var textures = LoadFrames($"{BattleRoot}/{spriteId}/animations/{name}/east");
            if (textures.Count > 0)
                loaded[name] = textures;
        }
        // Without an idle animation, stand in the still east-facing rotation.
        string still = $"{BattleRoot}/{spriteId}/rotations/east.png";
        if (!loaded.ContainsKey("idle") && ResourceLoader.Exists(still))
            loaded["idle"] = new List<Texture2D> { ResourceLoader.Load<Texture2D>(still) };

        SpriteFrames? frames = null;
        if (loaded.ContainsKey("idle") && loaded.ContainsKey("attack"))
        {
            frames = new SpriteFrames();
            frames.RemoveAnimation("default");
            foreach (var (name, fps, loop) in BattleAnimations)
            {
                var textures = loaded.GetValueOrDefault(name)
                               ?? (name == "critical" ? loaded["attack"] : loaded["idle"]);
                if (name == "idle")
                    textures = BreathingLoop(textures);
                frames.AddAnimation(name);
                frames.SetAnimationSpeed(name, fps);
                frames.SetAnimationLoopMode(name, loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);
                foreach (var texture in textures)
                    frames.AddFrame(name, texture);
            }
        }
        Cache[key] = frames;
        return frames;
    }

    /// <summary>Map frames for the unit's current outfit (see BattleUnit.SpriteCandidates), or null.</summary>
    public static SpriteFrames? Map(BattleUnit unit) =>
        Grid.GridMetrics.Flat && unit.SpriteCandidates().Select(Field).FirstOrDefault(f => f is not null) is { } field
            ? field
            : unit.SpriteCandidates().Select(Map).FirstOrDefault(f => f is not null);

    public const string FieldRoot = "res://assets/sprites/field";

    /// <summary>
    /// The flat map's SNES field sprite (assets/sprites/field/&lt;id&gt;.png): one front-facing still, used for
    /// every map animation and direction until walk frames exist. Null if there's no field art.
    /// </summary>
    public static SpriteFrames? Field(string spriteId)
    {
        string key = $"field:{spriteId}";
        if (Cache.TryGetValue(key, out var cached))
            return cached;
        string path = $"{FieldRoot}/{spriteId}.png";
        SpriteFrames? frames = null;
        if (ResourceLoader.Exists(path))
        {
            var texture = ResourceLoader.Load<Texture2D>(path);
            frames = new SpriteFrames();
            frames.RemoveAnimation("default");
            foreach (var (name, fps, loop) in MapAnimations)
                foreach (var dir in MapDirections)
                {
                    string anim = $"{name}_{dir}";
                    frames.AddAnimation(anim);
                    frames.SetAnimationSpeed(anim, fps);
                    frames.SetAnimationLoopMode(anim, loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);
                    frames.AddFrame(anim, texture);
                }
        }
        Cache[key] = frames;
        return frames;
    }

    /// <summary>
    /// Four-direction map frames for a sprite id, or null if any direction lacks both an idle and a rotation image.
    /// Missing directions of an animation fall back: idle → the static rotation, walk/attack/hit → idle.
    /// </summary>
    public static SpriteFrames? Map(string spriteId)
    {
        string key = $"map:{spriteId}";
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        string root = $"{MapRoot}/{spriteId}";
        var idle = new Dictionary<string, List<Texture2D>>();
        foreach (var dir in MapDirections)
        {
            var textures = LoadFrames($"{root}/animations/idle/{dir}");
            if (textures.Count == 0 && ResourceLoader.Exists($"{root}/rotations/{dir}.png"))
                textures.Add(ResourceLoader.Load<Texture2D>($"{root}/rotations/{dir}.png"));
            if (textures.Count == 0)
            {
                Cache[key] = null;
                return null;
            }
            idle[dir] = textures;
        }

        var frames = new SpriteFrames();
        frames.RemoveAnimation("default");
        foreach (var (name, fps, loop) in MapAnimations)
        {
            foreach (var dir in MapDirections)
            {
                var textures = name == "idle" ? idle[dir] : LoadFrames($"{root}/animations/{name}/{dir}");
                if (textures.Count == 0)
                    textures = idle[dir];
                string anim = $"{name}_{dir}";
                frames.AddAnimation(anim);
                frames.SetAnimationSpeed(anim, fps);
                frames.SetAnimationLoopMode(anim, loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);
                foreach (var texture in textures)
                    frames.AddFrame(anim, texture);
            }
        }
        Cache[key] = frames;
        return frames;
    }

    /// <summary>
    /// Generated idle clips drift (the weapon ends somewhere else than it started), so looping the whole clip pops.
    /// The first few frames stay close to the stance; play them forward and back instead.
    /// </summary>
    private static List<Texture2D> BreathingLoop(List<Texture2D> textures)
    {
        if (textures.Count <= 2)
            return textures;
        var head = textures.Take(3).ToList();
        return head.Concat(head.Skip(1).Take(1)).ToList();
    }

    private static List<Texture2D> LoadFrames(string dir)
    {
        if (!DirAccess.DirExistsAbsolute(dir))
            return new List<Texture2D>();
        // Exported builds list "x.png.import"/"x.png.remap" instead of "x.png".
        var files = DirAccess.GetFilesAt(dir)
            .Select(f => f.Replace(".import", "", StringComparison.Ordinal).Replace(".remap", "", StringComparison.Ordinal))
            .Where(f => f.EndsWith(".png", StringComparison.Ordinal))
            .Distinct()
            .OrderBy(f => f, StringComparer.Ordinal);
        return files.Select(f => ResourceLoader.Load<Texture2D>($"{dir}/{f}")).Where(t => t is not null).ToList();
    }
}
