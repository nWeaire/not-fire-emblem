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
