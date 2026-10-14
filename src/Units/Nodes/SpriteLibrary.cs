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
    /// Side-view battle frames (facing east) for a sprite id, or null if it has no idle and attack art yet.
    /// Missing optional animations fall back: critical → attack, dodge/hit → idle.
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

        SpriteFrames? frames = null;
        if (loaded.ContainsKey("idle") && loaded.ContainsKey("attack"))
        {
            frames = new SpriteFrames();
            frames.RemoveAnimation("default");
            foreach (var (name, fps, loop) in BattleAnimations)
            {
                var textures = loaded.GetValueOrDefault(name)
                               ?? (name == "critical" ? loaded["attack"] : loaded["idle"]);
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
        unit.SpriteCandidates().Select(Map).FirstOrDefault(f => f is not null);

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
