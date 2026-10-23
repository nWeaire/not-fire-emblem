using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NotFireEmblem.Audio;
using NotFireEmblem.Core;
using NotFireEmblem.Units;

namespace NotFireEmblem.UI;

/// <summary>
/// The promotion scene (session 13, user request: "a Pokémon evolution sequence"). Over a darkened hub the unit's
/// battle sprite turns to a white silhouette, flickers between its old and new outfits faster and faster, flashes,
/// and is revealed in its new class with the stat changes. Confirm skips to the reveal, then closes.
/// </summary>
public partial class PromotionSequence : ColorRect
{
    // Timeline (seconds).
    private const double WhitenStart = 1.2, SwapStart = 2.0, SwapEnd = 6.0, FlashPeak = 6.4, RevealEnd = 7.4;

    private readonly string _name, _oldClassName, _newClassName;
    private readonly SpriteFrames? _oldFrames, _newFrames;
    private readonly List<(Stat Stat, int Change)> _changes;
    private readonly Action? _closed;

    private AnimatedSprite2D _sprite = null!;
    private ShaderMaterial _whiteness = null!;
    private TextureRect _glow = null!;
    private CpuParticles2D _sparkles = null!;
    private ColorRect _flash = null!;
    private Label _caption = null!;
    private Control _stats = null!;
    private Label _prompt = null!;
    private double _t;
    private bool _showingNew;
    private double _nextSwap = SwapStart;
    private bool _revealed, _closing;

    private const string WhiteShader = @"shader_type canvas_item;
uniform float white : hint_range(0.0, 1.0) = 0.0;
void fragment() {
    vec4 c = texture(TEXTURE, UV);
    COLOR = vec4(mix(c.rgb, vec3(1.0), white), c.a);
}";

    public PromotionSequence(ContentDatabase db, UnitState unit, string oldClassId, string newClassId, int crestTier, Action? closed = null)
    {
        _name = unit.Def.Name;
        _oldClassName = db.Class(oldClassId).Name;
        _newClassName = db.Class(newClassId).Name;
        bool crest = unit.Def.IsLord && crestTier > 0;
        _oldFrames = Frames(db, unit.Def.Id, oldClassId, crest);
        _newFrames = Frames(db, unit.Def.Id, newClassId, crest) ?? _oldFrames;
        _oldFrames ??= _newFrames;
        var before = db.Class(oldClassId).Bases;
        var after = db.Class(newClassId).Bases;
        _changes = StatBlock.All.Where(s => s != Stat.Cha && after[s] != before[s]).Select(s => (s, after[s] - before[s])).ToList();
        _closed = closed;
    }

    /// <summary>The unit's battle frames in a class's outfit, falling back like BattleUnit.SpriteCandidates.</summary>
    private static SpriteFrames? Frames(ContentDatabase db, string spriteId, string classId, bool crest)
    {
        var candidates = new List<string>();
        if (crest)
            candidates.AddRange(new[] { $"{spriteId}_{classId}_crest", $"{spriteId}_crest" });
        candidates.Add($"{spriteId}_{classId}");
        candidates.AddRange(Outfits.Earlier(db, classId).Select(c => $"{spriteId}_{c}"));
        candidates.Add(spriteId);
        return candidates.Select(SpriteLibrary.Battle).FirstOrDefault(f => f is not null);
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Color = new Color(0.02f, 0.02f, 0.04f, 0f);
        MouseFilter = MouseFilterEnum.Stop;

        var gradient = new Gradient { Colors = new[] { new Color(1f, 0.93f, 0.7f, 0.55f), new Color(1f, 0.93f, 0.7f, 0f) } };
        _glow = new TextureRect
        {
            Texture = new GradientTexture2D
            {
                Gradient = gradient, Fill = GradientTexture2D.FillEnum.Radial, Width = 256, Height = 256,
                FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f),
            },
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Modulate = new Color(1, 1, 1, 0),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_glow);

        _sparkles = new CpuParticles2D
        {
            Emitting = false, Amount = 48, Lifetime = 1.4,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Sphere, EmissionSphereRadius = 150,
            Direction = Vector2.Up, Spread = 30, InitialVelocityMin = 30, InitialVelocityMax = 80,
            Gravity = new Vector2(0, -40), ScaleAmountMin = 3, ScaleAmountMax = 6,
            Color = new Color(1f, 0.95f, 0.75f),
            ColorRamp = new Gradient { Colors = new[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0) } },
        };
        AddChild(_sparkles);

        _whiteness = new ShaderMaterial { Shader = new Shader { Code = WhiteShader } };
        _sprite = new AnimatedSprite2D { Material = _whiteness, TextureFilter = TextureFilterEnum.Nearest };
        if (_oldFrames is not null)
        {
            _sprite.SpriteFrames = _oldFrames;
            _sprite.Play("idle");
        }
        AddChild(_sprite);

        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 10);
        column.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        column.GrowVertical = GrowDirection.Begin;
        column.OffsetBottom = -28;
        AddChild(column);
        _stats = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, Modulate = new Color(1, 1, 1, 0) };
        ((HBoxContainer)_stats).AddThemeConstantOverride("separation", 22);
        foreach (var (stat, change) in _changes)
            _stats.AddChild(UiKit.Outlined(UiKit.Label($"{stat} {change:+0;-0}", 22, change > 0 ? UiKit.Good : UiKit.Bad)));
        column.AddChild(_stats);
        // Silent while the outfits change; the caption fades in with the reveal.
        _caption = UiKit.Outlined(UiKit.Label("", 30, UiKit.Text, HorizontalAlignment.Center));
        column.AddChild(_caption);
        _prompt = UiKit.Label("Confirm ▶", 16, UiKit.Muted, HorizontalAlignment.Center);
        _prompt.Modulate = new Color(1, 1, 1, 0);
        column.AddChild(_prompt);

        _flash = new ColorRect { Color = new Color(1, 1, 1, 0), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(UiKit.Fill(_flash));

        Layout();
        Resized += Layout;
        AudioDirector.PlaySfx("crest_rise");
    }

    /// <summary>Centres the sprite a little above the middle and scales it to about 40% of the screen's height.</summary>
    private void Layout()
    {
        var center = new Vector2(Size.X / 2f, Size.Y * 0.42f);
        var frames = _showingNew ? _newFrames : _oldFrames;
        if (frames is not null)
        {
            var texture = frames.GetFrameTexture("idle", 0);
            var used = texture.GetImage().GetUsedRect();
            float scale = Mathf.Max(1f, Mathf.Floor(Size.Y * 0.4f / Mathf.Max(1, used.Size.Y)));
            _sprite.Scale = new Vector2(scale, scale);
            // Line up the drawn pixels' centre, not the padded frame's (the player's side faces left, so flip).
            var offset = texture.GetSize() / 2f - ((Vector2)used.Position + (Vector2)used.Size / 2f);
            _sprite.FlipH = true;
            _sprite.Offset = offset with { X = -offset.X };
        }
        _sprite.Position = center;
        float glow = Size.Y * 0.9f;
        _glow.Size = new Vector2(glow, glow);
        _glow.Position = center - _glow.Size / 2f;
        _sparkles.Position = center;
    }

    private void Show(bool newOutfit)
    {
        if (_showingNew == newOutfit)
            return;
        _showingNew = newOutfit;
        var frames = newOutfit ? _newFrames : _oldFrames;
        if (frames is null)
            return;
        _sprite.SpriteFrames = frames;
        _sprite.Play("idle");
        Layout();
    }

    public override void _Process(double delta)
    {
        if (_closing)
            return;
        _t += delta;
        Color = Color with { A = (float)Math.Min(0.94, _t / 0.6 * 0.94) };
        if (_t < SwapStart)
        {
            _whiteness.SetShaderParameter("white", (float)Math.Clamp((_t - WhitenStart) / (SwapStart - WhitenStart), 0, 1));
            _glow.Modulate = _glow.Modulate with { A = (float)Math.Clamp((_t - WhitenStart) / 0.8, 0, 1) * 0.5f };
            _sparkles.Emitting = _t >= WhitenStart;
            return;
        }
        if (_t < SwapEnd)
        {
            // Old and new outfits trade places, faster and faster (0.45 s down to 0.04 s).
            if (_t >= _nextSwap)
            {
                Show(!_showingNew);
                double progress = (_t - SwapStart) / (SwapEnd - SwapStart);
                _nextSwap = _t + 0.45 * Math.Pow(0.04 / 0.45, progress);
                if (_nextSwap - _t > 0.09)
                    AudioDirector.PlaySfx("cursor", 0.8f + (float)progress * 0.8f);
            }
            _glow.Modulate = _glow.Modulate with { A = 0.5f + 0.5f * (float)((_t - SwapStart) / (SwapEnd - SwapStart)) };
            return;
        }
        if (_t < FlashPeak)
        {
            Show(true);
            _sparkles.Emitting = false;
            _flash.Color = _flash.Color with { A = (float)((_t - SwapEnd) / (FlashPeak - SwapEnd)) };
            return;
        }
        if (!_revealed)
            Reveal();
        float k = (float)Math.Clamp((_t - FlashPeak) / (RevealEnd - FlashPeak), 0, 1);
        _flash.Color = _flash.Color with { A = 1 - k };
        _whiteness.SetShaderParameter("white", 1 - k);
        _glow.Modulate = _glow.Modulate with { A = 1 - 0.6f * k };
        _stats.Modulate = _stats.Modulate with { A = k };
        _caption.Modulate = _caption.Modulate with { A = k };
        _prompt.Modulate = _prompt.Modulate with { A = k >= 1 ? 0.6f + 0.4f * Mathf.Sin((float)_t * 4f) : 0 };
    }

    private void Reveal()
    {
        _revealed = true;
        Show(true);
        _sparkles.Emitting = false;
        string article = "AEIOU".Contains(char.ToUpperInvariant(_newClassName[0])) ? "an" : "a";
        _caption.Text = $"{_name} became {article} {_newClassName}!";
        AudioDirector.PlaySfx("level_up");
    }

    /// <summary>Every key, button and click stops here while the scene is up; Confirm skips ahead, then closes.</summary>
    public override void _Input(InputEvent e)
    {
        if (_closing || e is InputEventMouseMotion)
            return;
        GetViewport().SetInputAsHandled();
        bool confirm = e.IsActionPressed("ui_accept") || e.IsActionPressed("confirm")
                       || e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left };
        if (!confirm)
            return;
        if (_t < FlashPeak)
        {
            _t = FlashPeak;
            Reveal();
        }
        else if (_t >= RevealEnd)
            Close();
    }

    private void Close()
    {
        _closing = true;
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0f, 0.25);
        tween.TweenCallback(Callable.From(() =>
        {
            _closed?.Invoke();
            QueueFree();
        }));
    }
}
