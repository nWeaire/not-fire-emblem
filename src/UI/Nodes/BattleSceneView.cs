using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using NotFireEmblem.Audio;
using NotFireEmblem.Battle;
using NotFireEmblem.Combat;
using NotFireEmblem.Units;

namespace NotFireEmblem.UI;

/// <summary>
/// The GBA Fire Emblem style battle cut-in. A 240×135 pixel stage (a GBA battle background with two 64 px side-view
/// fighters, the player's side on the right) is scaled up to fill the screen, with the Clean UI on top. Each strike of a
/// CombatResolved event plays with the screen effects from docs/ART_DIRECTION.md §4: critical wind-up (flash pop, dim,
/// CRITICAL! banner), lunge to contact (the weapon tip stops on the target's body), cut streak, white-out, impact
/// burst, shake, white/red hit flash, knockback, damage pop and a draining HP bar. Purely presentational.
/// </summary>
public partial class BattleSceneView : CanvasLayer
{
    // Stage coordinates are GBA pixels.
    private const int StageW = 240;
    private const int StageH = 135;
    private const float FeetY = 101f;
    private const float LeftX = 64f;
    private const float RightX = 176f;
    private const float ArtHeight = 64f; // legacy art taller than MaxNativeHeight is scaled down to this canvas size
    private const float MaxNativeHeight = 84f; // battle art up to this size (64 px, and the 84 px "E" style) draws 1:1

    private static readonly Color DimColor = new(12 / 255f, 8 / 255f, 28 / 255f);

    private Control _root = null!;
    private TextureRect _sky = null!;
    private Node2D _stage = null!;
    private Sprite2D _bg = null!;
    private ColorRect _dim = null!;
    private Fighter _left = null!;
    private Fighter _right = null!;
    private Node2D _fx = null!;
    private ColorRect _whiteout = null!;
    private Control _ui = null!;
    private SideHud _leftHud = null!;
    private SideHud _rightHud = null!;
    private Label _banner = null!;
    private ColorRect _fade = null!;
    private float _scale = 4.8f;
    private Vector2 _origin;

    private static Shader? _flashShader;

    public override void _Ready()
    {
        Layer = 10;
        Visible = false;

        _root = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // Shown around the stage on very wide windows and when a background is missing.
        _sky = UiKit.Backdrop(new Color(0.32f, 0.5f, 0.82f), new Color(0.55f, 0.66f, 0.5f));
        _root.AddChild(_sky);

        _stage = new Node2D { TextureFilter = CanvasItem.TextureFilterEnum.Nearest };
        _root.AddChild(_stage);
        _bg = new Sprite2D { Centered = false };
        _stage.AddChild(_bg);
        _dim = new ColorRect { Color = DimColor with { A = 0f }, Size = new Vector2(StageW, StageH), MouseFilter = Control.MouseFilterEnum.Ignore };
        _stage.AddChild(_dim);
        _left = new Fighter(_stage, faceRight: true);
        _right = new Fighter(_stage, faceRight: false);
        _fx = new Node2D();
        _stage.AddChild(_fx);
        _whiteout = new ColorRect { Color = new Color(1, 1, 1, 0), Size = new Vector2(StageW, StageH), MouseFilter = Control.MouseFilterEnum.Ignore };
        _stage.AddChild(_whiteout);

        _ui = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(_ui);
        _ui.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _leftHud = new SideHud(_ui, left: true);
        _rightHud = new SideHud(_ui, left: false);

        _banner = UiKit.Outlined(UiKit.Label("CRITICAL!", 54, new Color(1f, 0.93f, 0.43f), HorizontalAlignment.Center), 14);
        _banner.AddThemeColorOverride("font_outline_color", new Color(0.47f, 0.09f, 0f));
        _banner.Visible = false;
        _ui.AddChild(_banner);
        _banner.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        _banner.GrowHorizontal = Control.GrowDirection.Both;
        _banner.Position += new Vector2(0, 92);

        _fade = new ColorRect { Color = new Color(0, 0, 0, 1), MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(_fade);
        _fade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        GetViewport().SizeChanged += Layout;
        Layout();
    }

    /// <summary>Scales the stage to cover the screen (4.8× at the 1152×648 base size) and centres it.</summary>
    private void Layout()
    {
        var size = GetViewport().GetVisibleRect().Size;
        _scale = Math.Max(size.X / StageW, size.Y / StageH);
        _origin = new Vector2((size.X - StageW * _scale) / 2f, (size.Y - StageH * _scale) / 2f);
        _stage.Scale = Vector2.One * _scale;
        _stage.Position = _origin;
    }

    private Vector2 ToScreen(Vector2 stagePoint) => _origin + stagePoint * _scale;

    /// <summary>True when both units have battle art; otherwise the caller plays the on-map animation instead.</summary>
    public static bool CanShow(BattleUnit a, BattleUnit b) =>
        SpriteLibrary.Battle(a) is not null && SpriteLibrary.Battle(b) is not null;

    /// <summary>The battle background for the terrain the defender stands on (assets/backgrounds/).</summary>
    public static string BackgroundFor(string terrainId) => terrainId switch
    {
        "forest" => "forest",
        "water" or "bridge" or "shallows" => "river_road",
        "fort" or "wall" or "house" or "chapel" or "fire" or "village" => "village",
        _ => "plains",
    };

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

        var terrain = s.Grid[defender.Pos].Terrain;
        var bg = UiKit.Background(BackgroundFor(terrain.Id)) ?? UiKit.Background("plains");
        _bg.Texture = bg;
        _bg.Visible = bg is not null;
        _bg.Modulate = terrain.Id == "fire" ? new Color(1f, 0.72f, 0.6f) : Colors.White;
        _dim.Color = DimColor with { A = 0f };
        _whiteout.Color = Colors.White with { A = 0f };
        _stage.Position = _origin;

        _left.Setup(leftUnit, LeftX, WeaponOf(s, leftUnit));
        _right.Setup(rightUnit, RightX, WeaponOf(s, rightUnit));
        _left.SwingSound = AudioCues.Swing(s.Db, leftUnit.State);
        _right.SwingSound = AudioCues.Swing(s.Db, rightUnit.State);
        _leftHud.Show(leftForecast, leftUnit.Faction);
        _rightHud.Show(rightForecast, rightUnit.Faction);

        Visible = true;
        await TweenAlpha(_fade, 0f, 0.25f);
        await Wait(0.3f);

        bool melee = c.Forecast.Distance <= 1;
        foreach (var strike in c.Result.Strikes)
        {
            bool strikerOnRight = strike.ByAttacker == attackerOnRight;
            var striker = strikerOnRight ? _right : _left;
            var target = strikerOnRight ? _left : _right;
            var targetHud = strikerOnRight ? _leftHud : _rightHud;
            // Counters play the same beats at a lower intensity.
            await PlayStrike(striker, target, targetHud, strike, melee, counter: !strike.ByAttacker);
            await Wait(0.2f);
        }

        // Defeated units flicker out, as in the GBA games.
        bool rightDied = attackerOnRight ? c.Result.AttackerDied : c.Result.DefenderDied;
        bool leftDied = attackerOnRight ? c.Result.DefenderDied : c.Result.AttackerDied;
        if (rightDied || leftDied)
        {
            var dying = new[] { rightDied ? _right : null, leftDied ? _left : null }.OfType<Fighter>().ToList();
            for (int i = 0; i < 8; i++)
            {
                foreach (var f in dying)
                    f.Sprite.Visible = i % 2 == 1;
                await Wait(0.06f);
            }
            var tween = CreateTween().SetParallel();
            foreach (var f in dying)
            {
                f.Sprite.Visible = true;
                f.SetFlash(Colors.White, 1f);
                tween.TweenProperty(f.Sprite, "modulate:a", 0f, 0.5f);
            }
            await ToSignal(tween, Tween.SignalName.Finished);
        }

        await Wait(0.4f);
        await TweenAlpha(_fade, 1f, 0.25f);
        Visible = false;
        _left.Reset();
        _right.Reset();
    }

    private static WeaponType? WeaponOf(BattleState s, BattleUnit unit) =>
        s.Db.TryWeapon(unit.State.EquippedItemId)?.Type;

    private async Task PlayStrike(Fighter striker, Fighter target, SideHud targetHud, Strike strike, bool melee, bool counter)
    {
        float intensity = strike.Crit ? 1f : counter ? 0.45f : 0.7f;
        string anim = strike.Crit ? "critical" : "attack";
        var frames = striker.Sprite.SpriteFrames;
        int frameCount = frames.GetFrameCount(anim);
        double fps = frames.GetAnimationSpeed(anim);
        int impactFrame = striker.ImpactFrame(anim);
        float toImpact = (float)(impactFrame / fps);
        float total = (float)(frameCount / fps);

        // 1. Critical wind-up: a white pop, then the screen dims toward violet and the banner shows.
        if (strike.Crit)
        {
            AudioDirector.PlaySfx("critical");
            _whiteout.Color = Colors.White with { A = 0.9f };
            await Wait(0.05f);
            _whiteout.Color = Colors.White with { A = 0f };
            _banner.Visible = true;
            _banner.Scale = Vector2.One * 1.4f;
            _banner.PivotOffset = _banner.Size / 2f;
            var dim = CreateTween().SetParallel();
            dim.TweenProperty(_dim, "color:a", 0.5f, 0.35f);
            dim.TweenProperty(_banner, "scale", Vector2.One, 0.15f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }

        // 2. Lunge to contact: dash in over the frames before the impact frame and stop where the weapon tip
        //    touches the front of the target's body.
        float home = striker.HomeX;
        float contact = home;
        if (melee)
        {
            float d = striker.Facing;
            float bodyFront = target.HomeX - d * target.BodyFront;      // nearest edge of the target's torso
            contact = bodyFront + d * 3f - d * striker.Reach(anim);    // a few pixels into the body
            if ((contact - home) * d < 0f)
                contact = home; // already in reach
            var lunge = CreateTween();
            lunge.TweenInterval(toImpact * 0.45f);
            lunge.TweenProperty(striker.Sprite, "position:x", contact, Math.Max(0.05f, toImpact * 0.55f))
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        }
        striker.Sprite.Play(anim);
        AudioDirector.PlaySfx(striker.SwingSound);
        await Wait(toImpact);
        AudioDirector.PlaySfx(AudioCues.Impact(strike.Hit, strike.Crit));

        // 3–4. The hit: cut streak, white-out, impact burst, then shake, flash, knockback, damage pop, HP drain.
        var chest = target.ChestPoint();
        if (strike.Hit)
        {
            target.Sprite.Play("hit");
            if (melee)
                Effect(striker.Weapon is WeaponType.Fist ? "claw_rake" : "cut_streak", chest, 0.7f, !striker.FaceRight, 0.11f);
            _whiteout.Color = Colors.White with { A = 0.35f + 0.65f * intensity };
            await Wait(strike.Crit ? 0.07f : 0.04f);
            _whiteout.Color = Colors.White with { A = 0f };
            Burst(chest, striker.Weapon, intensity);
            Shake(3f * intensity + 0.5f);
            _ = HitFlash(target);
            Knockback(target, (strike.Crit ? 6f : 3f) * striker.Facing);
            Popup(chest + new Vector2(0, -26), strike.Crit ? $"{strike.Damage}!" : $"{strike.Damage}",
                strike.Crit ? new Color(1f, 0.85f, 0.2f) : Colors.White, strike.Crit);
            targetHud.AnimateHp(strike.TargetHpAfter);
        }
        else
        {
            target.Sprite.Play("dodge");
            Knockback(target, 8f * striker.Facing, 0.25f);
            Popup(chest + new Vector2(0, -26), "Miss", new Color(0.75f, 0.85f, 1f), false);
        }

        // 5. Recovery: let the swing finish, then slide home and clear the dim.
        await Wait(Math.Max(0.3f, total - toImpact));
        var back = CreateTween().SetParallel();
        back.TweenProperty(striker.Sprite, "position:x", home, 0.22f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        if (strike.Crit)
        {
            back.TweenProperty(_dim, "color:a", 0f, 0.3f);
            back.TweenProperty(_banner, "modulate:a", 0f, 0.2f);
        }
        await ToSignal(back, Tween.SignalName.Finished);
        _banner.Visible = false;
        _banner.Modulate = Colors.White;
        if (striker.Sprite.Animation != "idle") striker.Sprite.Play("idle");
        if (target.Sprite.Animation != "idle") target.Sprite.Play("idle");
    }

    private void Effect(string name, Vector2 at, float alpha, bool flip, float seconds)
    {
        var texture = Vfx(name);
        if (texture is null)
            return;
        var sprite = new Sprite2D { Texture = texture, Position = at, FlipH = flip, Modulate = Colors.White with { A = alpha }, Scale = Vector2.One * 0.75f };
        _fx.AddChild(sprite);
        var tween = sprite.CreateTween();
        tween.TweenInterval(seconds);
        tween.TweenProperty(sprite, "modulate:a", 0f, 0.08f);
        tween.TweenCallback(Callable.From(sprite.QueueFree));
    }

    private void Burst(Vector2 at, WeaponType? weapon, float intensity)
    {
        var texture = Vfx("impact_burst");
        if (texture is null)
            return;
        var tint = weapon switch
        {
            WeaponType.BlackMagic => new Color(0.75f, 0.5f, 1f),
            WeaponType.WhiteMagic => new Color(1f, 0.95f, 0.75f),
            _ => Colors.White,
        };
        var sprite = new Sprite2D { Texture = texture, Position = at, Scale = Vector2.One * 0.35f, Modulate = tint };
        _fx.AddChild(sprite);
        float size = 0.55f + 0.45f * intensity;
        var tween = sprite.CreateTween().SetParallel();
        tween.TweenProperty(sprite, "scale", Vector2.One * size, 0.16f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(sprite, "modulate:a", 0f, 0.12f).SetDelay(0.12f);
        tween.Chain().TweenCallback(Callable.From(sprite.QueueFree));
    }

    private static readonly Dictionary<string, Texture2D?> VfxCache = new();

    private static Texture2D? Vfx(string name)
    {
        if (!VfxCache.TryGetValue(name, out var texture))
        {
            string path = $"res://assets/vfx/{name}.png";
            VfxCache[name] = texture = ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
        }
        return texture;
    }

    /// <summary>Six decaying jolts of the whole stage, in GBA pixels.</summary>
    private void Shake(float strength)
    {
        var tween = CreateTween();
        for (int i = 0; i < 6; i++)
        {
            float s = Mathf.Round(strength * (1f - i / 6f));
            var offset = new Vector2(i % 2 == 0 ? s : -s, i % 3 == 0 ? -s * 0.6f : s * 0.4f);
            tween.TweenProperty(_stage, "position", _origin + offset * _scale, 1f / 30f);
        }
        tween.TweenProperty(_stage, "position", _origin, 1f / 30f);
    }

    /// <summary>The target flashes white and red alternately while the hit lands.</summary>
    private async Task HitFlash(Fighter target)
    {
        for (int i = 0; i < 6; i++)
        {
            target.SetFlash(i % 2 == 0 ? Colors.White : new Color(1f, 0.16f, 0.16f), i < 3 ? 0.85f : 0.3f);
            await Wait(1f / 15f);
        }
        target.SetFlash(Colors.White, 0f);
    }

    private void Knockback(Fighter target, float dx, float seconds = 0.12f)
    {
        var tween = CreateTween();
        tween.TweenProperty(target.Sprite, "position:x", target.HomeX + dx, seconds).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenInterval(0.12f);
        tween.TweenProperty(target.Sprite, "position:x", target.HomeX, 0.2f);
    }

    private void Popup(Vector2 stagePoint, string text, Color color, bool big)
    {
        var label = UiKit.Outlined(UiKit.Label(text, big ? 48 : 38, color, HorizontalAlignment.Center), 10);
        label.AddThemeColorOverride("font_outline_color", big ? new Color(0.6f, 0f, 0f) : UiKit.Outline);
        label.ZIndex = 5;
        _ui.AddChild(label);
        label.Size = new Vector2(200, 60);
        label.Position = ToScreen(stagePoint) - new Vector2(100, 30);
        var tween = label.CreateTween();
        tween.TweenProperty(label, "position:y", label.Position.Y - 48, 0.6f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(label, "modulate:a", 0f, 0.3f).SetDelay(0.55f);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    private async Task TweenAlpha(ColorRect rect, float alpha, float seconds)
    {
        var tween = CreateTween();
        tween.TweenProperty(rect, "color:a", alpha, seconds);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task Wait(float seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

    private static Shader FlashShader => _flashShader ??= new Shader
    {
        Code = """
            shader_type canvas_item;
            uniform vec4 flash_color : source_color = vec4(1.0);
            uniform float amount = 0.0;
            void fragment() {
                COLOR.rgb = mix(COLOR.rgb, flash_color.rgb, amount);
            }
            """,
    };

    /// <summary>
    /// One side's sprite on the stage. Art faces east; the right-hand fighter is mirrored with a negative x scale so
    /// that every measurement (anchor, weapon reach, body front) can be taken from the unmirrored frames.
    /// </summary>
    private sealed class Fighter
    {
        public AnimatedSprite2D Sprite { get; }
        public bool FaceRight { get; }
        /// <summary>+1 when facing right (toward the other fighter on the left side), −1 when facing left.</summary>
        public float Facing => FaceRight ? 1f : -1f;
        public float HomeX { get; private set; }
        public WeaponType? Weapon { get; private set; }
        public string SwingSound { get; set; } = "sword_swing";
        /// <summary>Distance in stage pixels from the anchor (feet centre) to the front of the torso.</summary>
        public float BodyFront { get; private set; } = 8f;
        private float _bodyHeight = 32f;
        private float _artScale = 1f;
        private readonly ShaderMaterial _material;
        private readonly Dictionary<string, (int Frame, float Reach)> _strikes = new();

        public Fighter(Node2D stage, bool faceRight)
        {
            FaceRight = faceRight;
            _material = new ShaderMaterial { Shader = FlashShader };
            Sprite = new AnimatedSprite2D { Centered = false, Material = _material };
            stage.AddChild(Sprite);
        }

        public void Setup(BattleUnit unit, float x, WeaponType? weapon)
        {
            // Play() only runs after CanShow() confirmed both units have battle art.
            Sprite.SpriteFrames = SpriteLibrary.Battle(unit)!;
            Weapon = weapon;
            _strikes.Clear();
            var idle = Sprite.SpriteFrames.GetFrameTexture("idle", 0).GetImage();
            _artScale = idle.GetHeight() <= MaxNativeHeight ? 1f : ArtHeight / idle.GetHeight();
            var used = idle.GetUsedRect();
            float anchorX = used.Position.X + used.Size.X / 2f;
            Sprite.Offset = new Vector2(-anchorX, -used.End.Y);
            Sprite.Scale = new Vector2(Facing * _artScale, _artScale);
            HomeX = x;
            Sprite.Position = new Vector2(x, FeetY);
            _bodyHeight = used.Size.Y * _artScale;
            BodyFront = TorsoFront(idle, used, anchorX) * _artScale;
            Reset();
            Sprite.Play("idle");
        }

        public void Reset()
        {
            Sprite.Position = new Vector2(HomeX, FeetY);
            Sprite.Modulate = Colors.White;
            Sprite.Visible = true;
            SetFlash(Colors.White, 0f);
        }

        public void SetFlash(Color color, float amount)
        {
            _material.SetShaderParameter("flash_color", color);
            _material.SetShaderParameter("amount", amount);
        }

        /// <summary>A point on the torso, a little inside the body front, at chest height.</summary>
        public Vector2 ChestPoint() => new(HomeX + Facing * (BodyFront - 3f), FeetY - _bodyHeight * 0.58f);

        /// <summary>The frame where the weapon reaches furthest forward: the moment of impact.</summary>
        public int ImpactFrame(string anim) => Strike(anim).Frame;

        /// <summary>How far forward of the anchor the weapon reaches on the impact frame, in stage pixels.</summary>
        public float Reach(string anim) => Strike(anim).Reach;

        private (int Frame, float Reach) Strike(string anim)
        {
            if (_strikes.TryGetValue(anim, out var cached))
                return cached;
            var frames = Sprite.SpriteFrames;
            float anchorX = -Sprite.Offset.X;
            int count = frames.GetFrameCount(anim);
            int best = 0;
            float reach = float.MinValue;
            // The earliest frame with the longest reach (a critical's lunge often peaks on its last frame).
            for (int i = 0; i < count; i++)
            {
                var used = frames.GetFrameTexture(anim, i).GetImage().GetUsedRect();
                float r = used.End.X - anchorX;
                if (r > reach)
                {
                    reach = r;
                    best = i;
                }
            }
            if (count > 2 && best == 0)
                best = count / 2; // no visible reach (e.g. a spell): impact half-way
            return _strikes[anim] = (best, reach * _artScale);
        }

        /// <summary>
        /// The front of the torso: the median of the forward-most opaque pixel over the rows of the middle of the body.
        /// The median ignores a weapon held out in front, which only covers a few rows.
        /// </summary>
        private static float TorsoFront(Image image, Rect2I used, float anchorX)
        {
            var fronts = new List<float>();
            int top = used.Position.Y + used.Size.Y * 35 / 100;
            int bottom = used.Position.Y + used.Size.Y * 65 / 100;
            for (int y = top; y <= bottom; y++)
            {
                for (int x = used.End.X - 1; x >= used.Position.X; x--)
                {
                    if (image.GetPixel(x, y).A > 0.5f)
                    {
                        fronts.Add(x + 1 - anchorX);
                        break;
                    }
                }
            }
            if (fronts.Count == 0)
                return used.Size.X / 4f;
            fronts.Sort();
            return Math.Max(2f, fronts[fronts.Count / 2]);
        }
    }

    /// <summary>One side's name plate (top) and weapon, Hit/Dmg/Crit and HP panel (bottom), in the Clean style.</summary>
    private sealed class SideHud
    {
        private readonly Label _name;
        private readonly Label _weapon;
        private readonly Label[] _values = new Label[3];
        private readonly Label _hpText;
        private readonly ProgressBar _hp;
        private readonly StyleBoxFlat _fill;

        public SideHud(Control parent, bool left)
        {
            _name = UiKit.Label("", 24);
            var plate = UiKit.Plate(_name);
            parent.AddChild(plate);
            plate.SetAnchorsAndOffsetsPreset(left ? Control.LayoutPreset.TopLeft : Control.LayoutPreset.TopRight);
            plate.GrowHorizontal = left ? Control.GrowDirection.End : Control.GrowDirection.Begin;
            plate.CustomMinimumSize = new Vector2(260, 0);
            plate.Position += new Vector2(left ? 18 : -18, 18);

            var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            panel.AddThemeStyleboxOverride("panel", UiKit.PanelStyle(margin: 10));
            parent.AddChild(panel);
            panel.AnchorLeft = left ? 0f : 0.5f;
            panel.AnchorRight = left ? 0.5f : 1f;
            panel.AnchorTop = panel.AnchorBottom = 1f;
            panel.OffsetTop = -126;
            panel.OffsetBottom = 0;

            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 2);
            panel.AddChild(box);
            var top = new HBoxContainer();
            top.AddThemeConstantOverride("separation", 18);
            box.AddChild(top);
            _weapon = UiKit.Label("", 20);
            _weapon.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            top.AddChild(_weapon);
            string[] names = { "HIT", "DMG", "CRT" };
            for (int i = 0; i < 3; i++)
            {
                var col = new VBoxContainer { CustomMinimumSize = new Vector2(52, 0) };
                col.AddThemeConstantOverride("separation", -4);
                col.AddChild(UiKit.Caps(names[i], 13));
                _values[i] = UiKit.Label("", 20);
                col.AddChild(_values[i]);
                top.AddChild(col);
            }
            var hpRow = new HBoxContainer();
            hpRow.AddThemeConstantOverride("separation", 10);
            box.AddChild(hpRow);
            _hpText = UiKit.Label("", 22);
            _hpText.CustomMinimumSize = new Vector2(40, 0);
            hpRow.AddChild(_hpText);
            _hp = new ProgressBar { ShowPercentage = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 14), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
            _fill = new StyleBoxFlat { BgColor = UiKit.Good };
            _hp.AddThemeStyleboxOverride("fill", _fill);
            hpRow.AddChild(_hp);
        }

        public void Show(CombatantForecast f, Faction faction)
        {
            _name.Text = f.Name;
            string arrow = f.TriangleAdvantage > 0 ? "  ▲" : f.TriangleAdvantage < 0 ? "  ▼" : "";
            _weapon.Text = (f.WeaponName ?? "—") + arrow;
            _values[0].Text = f.CanStrike ? $"{f.Hit}" : "--";
            _values[1].Text = f.CanStrike ? $"{f.Damage}{(f.StrikeCount > 1 ? "×2" : "")}" : "--";
            _values[2].Text = f.CanStrike ? $"{f.Crit}" : "--";
            _fill.BgColor = faction == Faction.Enemy ? UiKit.Bad : faction == Faction.Ally ? new Color(0.5f, 0.85f, 0.45f) : UiKit.Good;
            _hp.MaxValue = f.MaxHp;
            _hp.Value = f.Hp;
            _hpText.Text = $"{f.Hp}";
        }

        /// <summary>Drains (or fills) the bar over about four GBA frames' worth of steps.</summary>
        public void AnimateHp(int hp)
        {
            var tween = _hp.CreateTween();
            tween.TweenMethod(Callable.From<double>(v =>
            {
                _hp.Value = v;
                _hpText.Text = $"{(int)Math.Ceiling(v)}";
            }), _hp.Value, (double)hp, 0.3f);
        }
    }
}
