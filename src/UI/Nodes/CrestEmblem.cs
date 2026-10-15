using System.Collections.Generic;
using Godot;

namespace NotFireEmblem.UI;

/// <summary>
/// A crest (assets/crests/&lt;motif&gt;/t&lt;tier&gt;_&lt;radiant|shrouded&gt;.png) with its glow: an additive, slightly larger copy
/// that pulses. A crest keeps its shape and takes its colour from the bearer's heart (STORY §4.1): Rowan's is radiant
/// white, the Binders' are shrouded purple. See docs/ART_DIRECTION.md §5.
/// </summary>
public partial class CrestEmblem : Control
{
    /// <summary>Crest motifs of the named bearers (ART_DIRECTION §5), keyed by a word in the unit's name.</summary>
    public static readonly IReadOnlyDictionary<string, (string Motif, int Tier)> Bearers = new Dictionary<string, (string, int)>
    {
        ["binder"] = ("serpent", 1),
        ["sennet"] = ("thorns", 1),
        ["varga"] = ("tree", 1),
        ["rusk"] = ("claws", 1),
        ["halvric"] = ("sword", 2),
        ["castellan"] = ("rose", 3),
        ["vossk"] = ("skull", 3),
        ["ilse"] = ("flames", 3),
        ["morvane"] = ("eye", 4),
    };

    private readonly TextureRect _glow;
    private readonly TextureRect _crest;
    private Tween? _pulse;

    public CrestEmblem(string motif, int tier, bool radiant, float size)
    {
        CustomMinimumSize = new Vector2(size, size);
        MouseFilter = MouseFilterEnum.Ignore;
        var texture = Texture(motif, tier, radiant);
        // The Lord's tier of Rowan's line renders a little dimmer than the others (ART_DIRECTION §5).
        var boost = motif == "rowan" && tier == 3 ? new Color(1.25f, 1.25f, 1.25f) : Colors.White;
        var glowColor = radiant ? new Color(0.85f, 0.92f, 1f, 0.5f) : new Color(0.75f, 0.35f, 1f, 0.55f);

        _glow = new TextureRect
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
            Modulate = glowColor,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_glow);
        _glow.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _glow.OffsetLeft = _glow.OffsetTop = -size * 0.08f;
        _glow.OffsetRight = _glow.OffsetBottom = size * 0.08f;

        _crest = new TextureRect
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            Modulate = boost,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_crest);
        _crest.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public override void _Ready()
    {
        _pulse = CreateTween().SetLoops();
        _pulse.TweenProperty(_glow, "modulate:a", _glow.Modulate.A * 0.35f, 1.1f).SetTrans(Tween.TransitionType.Sine);
        _pulse.TweenProperty(_glow, "modulate:a", _glow.Modulate.A, 1.1f).SetTrans(Tween.TransitionType.Sine);
    }

    /// <summary>The crest image, or null if it doesn't exist. Tiers are clamped to 1–4.</summary>
    public static Texture2D? Texture(string motif, int tier, bool radiant)
    {
        string path = $"res://assets/crests/{motif}/t{Mathf.Clamp(tier, 1, 4)}_{(radiant ? "radiant" : "shrouded")}.png";
        return ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
    }

    /// <summary>The crest a named bearer carries, matched on a word in their name ("Binder Sennet" → thorns).</summary>
    public static (string Motif, int Tier)? BearerCrest(string unitName)
    {
        (string, int)? fallback = null;
        foreach (var word in unitName.ToLowerInvariant().Split(' '))
        {
            if (Bearers.TryGetValue(word, out var crest))
            {
                if (word != "binder")
                    return crest;
                fallback = crest; // "Binder" alone means the Prologue's unnamed Binder
            }
        }
        return fallback;
    }
}
