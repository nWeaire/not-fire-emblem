using System;
using System.Threading.Tasks;
using Godot;
using NotFireEmblem.Battle;
using NotFireEmblem.Combat;
using NotFireEmblem.Units;

namespace NotFireEmblem.UI;

/// <summary>
/// The Fire Emblem style battle cut-in: the screen switches to a side-on view with both units at full size,
/// the player's side on the right and the enemy on the left, and plays each strike of a CombatResolved event
/// with attack / critical / hit / dodge animations, damage numbers and HP bars. Purely presentational.
/// </summary>
public partial class BattleSceneView : CanvasLayer
{
    private const float SpriteScale = 2.6f;
    private const float GroundY = 470f;
    private const float LeftX = 360f;
    private const float RightX = 792f;
    private const float LungeDistance = 250f;

    private Control _root = null!;
    private ColorRect _ground = null!;
    private Node2D _stage = null!;
    private AnimatedSprite2D _left = null!;
    private AnimatedSprite2D _right = null!;
    private Panel _leftPlatform = null!;
    private Panel _rightPlatform = null!;
    private SideHud _leftHud = null!;
    private SideHud _rightHud = null!;
    private ColorRect _flash = null!;
    private ColorRect _fade = null!;

    public override void _Ready()
    {
        Layer = 10;
        Visible = false;

        _root = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var sky = new TextureRect
        {
            Texture = new GradientTexture2D
            {
                Gradient = new Gradient { Colors = new[] { new Color(0.32f, 0.5f, 0.82f), new Color(0.78f, 0.86f, 0.95f) }, Offsets = new[] { 0f, 1f } },
                FillFrom = new Vector2(0, 0),
                FillTo = new Vector2(0, 1),
                Width = 8,
                Height = 64,
            },
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _root.AddChild(sky);
        sky.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _ground = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(_ground);
        _ground.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _ground.OffsetTop = GroundY - 60f;

        _leftPlatform = Platform();
        _rightPlatform = Platform();

        _stage = new Node2D();
        _root.AddChild(_stage);
        _left = Fighter(LeftX, flip: false);
        _right = Fighter(RightX, flip: true);

        _leftHud = new SideHud(_root, left: true);
        _rightHud = new SideHud(_root, left: false);

        _flash = new ColorRect { Color = new Color(1, 1, 1, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(_flash);
        _flash.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _fade = new ColorRect { Color = new Color(0, 0, 0, 1), MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(_fade);
        _fade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    private Panel Platform()
    {
        var panel = new Panel { Size = new Vector2(300, 46), MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.55f, 0.5f, 0.38f),
            CornerRadiusTopLeft = 150, CornerRadiusTopRight = 150, CornerRadiusBottomLeft = 150, CornerRadiusBottomRight = 150,
            BorderColor = new Color(0.2f, 0.18f, 0.12f), BorderWidthBottom = 4,
        });
        _root.AddChild(panel);
        return panel;
    }

    private AnimatedSprite2D Fighter(float x, bool flip)
    {
        var sprite = new AnimatedSprite2D
        {
            Position = new Vector2(x, GroundY),
            Scale = Vector2.One * SpriteScale,
            FlipH = flip,
            Centered = true,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
        _stage.AddChild(sprite);
        return sprite;
    }

    /// <summary>True when both units have battle art; otherwise the caller plays the on-map animation instead.</summary>
    public static bool CanShow(BattleUnit a, BattleUnit b) =>
        SpriteLibrary.Battle(a) is not null && SpriteLibrary.Battle(b) is not null;

    public async Task Play(BattleState s, CombatResolved c)
    {
        var attacker = s.Unit(c.AttackerId);
        var defender = s.Unit(c.DefenderId);
        // The player's side always stands on the right, as in the GBA games.
        bool attackerOnRight = attacker.Faction != Faction.Enemy || defender.Faction == Faction.Enemy;
        var rightUnit = attackerOnRight ? attacker : defender;
        var leftUnit = attackerOnRight ? defender : attacker;
        var rightForecast = attackerOnRight ? c.Forecast.Attacker : c.Forecast.Defender;
        var leftForecast = attackerOnRight ? c.Forecast.Defender : c.Forecast.Attacker;

        // Stage dressing from the defender's terrain.
        var terrain = s.Grid[defender.Pos].Terrain;
        _ground.Color = Color.FromHtml(terrain.ColorHex).Darkened(0.1f);
        PlacePlatform(_leftPlatform, LeftX, s.Grid[leftUnit.Pos].Terrain.ColorHex);
        PlacePlatform(_rightPlatform, RightX, s.Grid[rightUnit.Pos].Terrain.ColorHex);

        Setup(_left, leftUnit, LeftX);
        Setup(_right, rightUnit, RightX);
        _leftHud.Show(leftForecast, UnitView.ColorFor(leftUnit.Faction));
        _rightHud.Show(rightForecast, UnitView.ColorFor(rightUnit.Faction));

        Visible = true;
        await TweenAlpha(_fade, 0f, 0.25f);
        await Wait(0.35f);

        bool melee = c.Forecast.Distance <= 1;
        foreach (var strike in c.Result.Strikes)
        {
            bool strikerIsAttacker = strike.ByAttacker;
            bool strikerOnRight = strikerIsAttacker == attackerOnRight;
            var striker = strikerOnRight ? _right : _left;
            var target = strikerOnRight ? _left : _right;
            var targetHud = strikerOnRight ? _leftHud : _rightHud;
            await PlayStrike(striker, target, targetHud, strike, melee, strikerOnRight);
            await Wait(0.25f);
        }

        // Defeated units flicker out, as in the GBA games.
        if (c.Result.AttackerDied || c.Result.DefenderDied)
        {
            bool rightDied = attackerOnRight ? c.Result.AttackerDied : c.Result.DefenderDied;
            bool leftDied = attackerOnRight ? c.Result.DefenderDied : c.Result.AttackerDied;
            var tween = CreateTween().SetParallel();
            if (rightDied) tween.TweenProperty(_right, "modulate", new Color(1, 1, 1, 0), 0.8f).SetTrans(Tween.TransitionType.Bounce);
            if (leftDied) tween.TweenProperty(_left, "modulate", new Color(1, 1, 1, 0), 0.8f).SetTrans(Tween.TransitionType.Bounce);
            await ToSignal(tween, Tween.SignalName.Finished);
        }

        await Wait(0.45f);
        await TweenAlpha(_fade, 1f, 0.25f);
        Visible = false;
        _fade.Color = _fade.Color with { A = 1f };
    }

    private void PlacePlatform(Panel platform, float x, string hex)
    {
        platform.Position = new Vector2(x - platform.Size.X / 2f, GroundY - 26f);
        if (platform.GetThemeStylebox("panel") is StyleBoxFlat style)
            style.BgColor = Color.FromHtml(hex).Lightened(0.15f);
    }

    private static void Setup(AnimatedSprite2D sprite, BattleUnit unit, float x)
    {
        // Play() only runs after CanShow() confirmed both units have battle art.
        sprite.SpriteFrames = SpriteLibrary.Battle(unit)!;
        // Stand the unit's feet (lowest opaque pixel of its idle pose) on the platform, whatever the canvas padding.
        var image = sprite.SpriteFrames.GetFrameTexture("idle", 0).GetImage();
        var used = image.GetUsedRect();
        sprite.Offset = new Vector2(0, image.GetHeight() / 2f - used.End.Y);
        sprite.Position = new Vector2(x, GroundY);
        sprite.Modulate = Colors.White;
        sprite.Play("idle");
    }

    private async Task PlayStrike(AnimatedSprite2D striker, AnimatedSprite2D target, SideHud targetHud, Strike strike, bool melee, bool strikerOnRight)
    {
        string anim = strike.Crit ? "critical" : "attack";
        if (strike.Crit)
        {
            // The pre-critical glint.
            striker.Modulate = new Color(1.8f, 1.8f, 1.8f);
            await Wait(0.12f);
            striker.Modulate = Colors.White;
        }

        int frameCount = striker.SpriteFrames.GetFrameCount(anim);
        double fps = striker.SpriteFrames.GetAnimationSpeed(anim);
        float duration = (float)(frameCount / fps);
        float impactAt = duration * 0.55f;

        var home = striker.Position;
        if (melee)
        {
            // Close the gap for melee blows, then return.
            float direction = strikerOnRight ? -1f : 1f;
            var lunge = CreateTween();
            lunge.TweenProperty(striker, "position:x", home.X + direction * LungeDistance, impactAt * 0.8f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            lunge.TweenInterval(duration - impactAt * 0.8f - 0.05f);
            lunge.TweenProperty(striker, "position:x", home.X, 0.2f);
        }

        striker.Play(anim);
        await Wait(impactAt);

        if (strike.Hit)
        {
            target.Play("hit");
            if (strike.Crit)
            {
                _flash.Color = new Color(1, 1, 1, 0.85f);
                CreateTween().TweenProperty(_flash, "color:a", 0f, 0.35f);
            }
            Shake(strike.Crit ? 18f : 8f);
            FlashSprite(target);
            Popup(target.Position, strike.Crit ? $"{strike.Damage}!" : $"{strike.Damage}", strike.Crit ? new Color(1f, 0.85f, 0.2f) : Colors.White, strike.Crit);
            targetHud.AnimateHp(strike.TargetHpAfter);
        }
        else
        {
            target.Play("dodge");
            Popup(target.Position, "Miss", new Color(0.75f, 0.85f, 1f), false);
        }

        await Wait(Math.Max(0.35f, duration - impactAt));
        if (striker.Animation != "idle") striker.Play("idle");
        if (target.Animation != "idle") target.Play("idle");
        striker.Position = home;
    }

    private void Shake(float strength)
    {
        var tween = CreateTween();
        for (int i = 0; i < 6; i++)
        {
            float s = strength * (1f - i / 6f);
            tween.TweenProperty(_stage, "position", new Vector2(i % 2 == 0 ? s : -s, i % 3 == 0 ? s * 0.5f : 0f), 0.03f);
        }
        tween.TweenProperty(_stage, "position", Vector2.Zero, 0.03f);
    }

    private void FlashSprite(AnimatedSprite2D sprite)
    {
        var tween = CreateTween();
        tween.TweenProperty(sprite, "modulate", new Color(3f, 3f, 3f), 0.04f);
        tween.TweenProperty(sprite, "modulate", Colors.White, 0.16f);
    }

    private void Popup(Vector2 at, string text, Color color, bool big)
    {
        var label = new Label { Text = text, Modulate = color, ZIndex = 5 };
        label.AddThemeFontSizeOverride("font_size", big ? 56 : 40);
        label.AddThemeConstantOverride("outline_size", 12);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        _stage.AddChild(label);
        label.Position = at + new Vector2(-30, -230);
        var tween = label.CreateTween();
        tween.TweenProperty(label, "position:y", label.Position.Y - 50, 0.7f).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(label, "modulate:a", 0f, 0.4f).SetDelay(0.5f);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    private async Task TweenAlpha(ColorRect rect, float alpha, float seconds)
    {
        var tween = CreateTween();
        tween.TweenProperty(rect, "color:a", alpha, seconds);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task Wait(float seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

    /// <summary>One side's name, weapon, HP and Hit/Dmg/Crit box along the bottom of the screen.</summary>
    private sealed class SideHud
    {
        private readonly PanelContainer _panel;
        private readonly StyleBoxFlat _style;
        private readonly Label _name;
        private readonly Label _weapon;
        private readonly Label _stats;
        private readonly Label _hpText;
        private readonly ProgressBar _hp;

        public SideHud(Control parent, bool left)
        {
            _style = new StyleBoxFlat
            {
                BgColor = new Color(0.1f, 0.12f, 0.2f, 0.92f),
                BorderWidthTop = 3, BorderWidthBottom = 3, BorderWidthLeft = 3, BorderWidthRight = 3,
                CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
                ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 8, ContentMarginBottom = 8,
            };
            _panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            _panel.AddThemeStyleboxOverride("panel", _style);
            parent.AddChild(_panel);
            _panel.SetAnchorsAndOffsetsPreset(left ? Control.LayoutPreset.BottomLeft : Control.LayoutPreset.BottomRight);
            _panel.GrowVertical = Control.GrowDirection.Begin;
            _panel.GrowHorizontal = left ? Control.GrowDirection.End : Control.GrowDirection.Begin;
            _panel.CustomMinimumSize = new Vector2(560, 0);

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 18);
            _panel.AddChild(row);

            _stats = new Label();
            _stats.AddThemeFontSizeOverride("font_size", 16);
            row.AddChild(_stats);

            var main = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            row.AddChild(main);
            _name = new Label();
            _name.AddThemeFontSizeOverride("font_size", 24);
            main.AddChild(_name);
            _weapon = new Label { Modulate = new Color(1, 1, 1, 0.8f) };
            main.AddChild(_weapon);
            var hpRow = new HBoxContainer();
            main.AddChild(hpRow);
            _hpText = new Label { CustomMinimumSize = new Vector2(70, 0) };
            _hpText.AddThemeFontSizeOverride("font_size", 22);
            hpRow.AddChild(_hpText);
            _hp = new ProgressBar { ShowPercentage = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 16), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
            _hp.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color(0.35f, 0.9f, 0.45f) });
            _hp.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0.25f, 0.1f, 0.1f) });
            hpRow.AddChild(_hp);
            if (!left)
                row.MoveChild(_stats, 1); // stats box sits on the outer edge
        }

        public void Show(CombatantForecast f, Color factionColor)
        {
            _style.BorderColor = factionColor;
            _name.Text = f.Name;
            _name.Modulate = factionColor.Lightened(0.4f);
            string arrow = f.TriangleAdvantage > 0 ? "  ▲" : f.TriangleAdvantage < 0 ? "  ▼" : "";
            _weapon.Text = (f.WeaponName ?? "—") + arrow;
            _stats.Text = f.CanStrike
                ? $"HIT {f.Hit,3}\nDMG {f.Damage,3}{(f.StrikeCount > 1 ? " ×2" : "")}\nCRT {f.Crit,3}"
                : "HIT  --\nDMG  --\nCRT  --";
            _hp.MaxValue = f.MaxHp;
            _hp.Value = f.Hp;
            _hpText.Text = $"{f.Hp}";
        }

        public void AnimateHp(int hp)
        {
            var tween = _panel.CreateTween();
            tween.TweenMethod(Callable.From<double>(v =>
            {
                _hp.Value = v;
                _hpText.Text = $"{(int)Math.Ceiling(v)}";
            }), _hp.Value, (double)hp, 0.45f);
        }
    }
}
