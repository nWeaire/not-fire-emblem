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
    private PanelContainer _levelPanel = null!;
    private Label _levelText = null!;
    private VBoxContainer _levelStats = null!;
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

        _levelPanel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        _levelPanel.AddThemeStyleboxOverride("panel", UiKit.PanelStyle(margin: 16));
        var levelBox = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        levelBox.AddThemeConstantOverride("separation", 2);
        _levelPanel.AddChild(levelBox);
        _levelText = UiKit.Label("", 22);
        levelBox.AddChild(_levelText);
        _levelStats = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        _levelStats.AddThemeConstantOverride("separation", 2);
        levelBox.AddChild(_levelStats);
        _ui.AddChild(_levelPanel);

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

    /// <summary>
    /// The battle background for the terrain the defender stands on (assets/backgrounds/). Forest and water keep
    /// their own; elsewhere a map's own backdrop (MapDef.Background, e.g. the keep or the marsh) wins.
    /// </summary>
    public static string BackgroundFor(string terrainId, string? mapBackground = null) => terrainId switch
    {
        "forest" => "forest",
        "water" or "bridge" => "river_road",
        "shallows" or "reeds" or "stilt" => mapBackground ?? "greyfen",
        _ when mapBackground is not null => mapBackground,
        "fort" or "wall" or "house" or "chapel" or "fire" or "village" => "village",
        _ => "plains",
    };

    /// <summary>
    /// Plays one fight, then (before fading back to the map) fills the player's EXP bar with what the fight earned
    /// and shows any level-ups here, GBA style. <paramref name="exp"/> holds the fight's ExpGained events.
    /// </summary>
    public async Task Play(BattleState s, CombatResolved c, IReadOnlyList<ExpGained>? exp = null)
    {
        var attacker = s.Unit(c.AttackerId);
        var defender = s.Unit(c.DefenderId);
        // The player's side always stands on the right, as in the GBA games.
        bool attackerOnRight = attacker.Faction.IsPlayerSide() || !defender.Faction.IsPlayerSide();
        var rightUnit = attackerOnRight ? attacker : defender;
        var leftUnit = attackerOnRight ? defender : attacker;
        var rightForecast = attackerOnRight ? c.Forecast.Attacker : c.Forecast.Defender;
        var leftForecast = attackerOnRight ? c.Forecast.Defender : c.Forecast.Attacker;

        var terrain = s.Grid[defender.Pos].Terrain;
        var bg = UiKit.Background(BackgroundFor(terrain.Id, s.Map.Background)) ?? UiKit.Background("plains");
        _bg.Texture = bg;
        _bg.Visible = bg is not null;
        _bg.Modulate = terrain.Id == "fire" ? new Color(1f, 0.72f, 0.6f)
            : s.Map.Lighting == "night" ? new Color(0.5f, 0.56f, 0.85f)
            : s.Map.Lighting == "snow" ? new Color(0.9f, 0.94f, 1.05f) : Colors.White;
        _dim.Color = DimColor with { A = 0f };
        _whiteout.Color = Colors.White with { A = 0f };
        _stage.Position = _origin;

        _left.Setup(leftUnit, LeftX, WeaponOf(s, leftUnit));
        _right.Setup(rightUnit, RightX, WeaponOf(s, rightUnit));
        _left.Spell = SpellOf(s, leftForecast.WeaponName);
        _right.Spell = SpellOf(s, rightForecast.WeaponName);
        _left.SwingSound = AudioCues.Swing(s.Db, leftUnit.State);
        _right.SwingSound = AudioCues.Swing(s.Db, rightUnit.State);
        _leftHud.Show(leftForecast, leftUnit.Faction);
        _rightHud.Show(rightForecast, rightUnit.Faction);
        var leftExp = exp?.FirstOrDefault(x => x.UnitId == leftUnit.Id);
        var rightExp = exp?.FirstOrDefault(x => x.UnitId == rightUnit.Id);
        _leftHud.ShowExp(leftUnit.IsRosterUnit ? ExpBefore(leftUnit, leftExp) : null);
        _rightHud.ShowExp(rightUnit.IsRosterUnit ? ExpBefore(rightUnit, rightExp) : null);

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

        // EXP, then level-ups, before leaving the battle screen.
        foreach (var (unit, hud, gained) in new[] { (rightUnit, _rightHud, rightExp), (leftUnit, _leftHud, leftExp) })
        {
            if (gained is null || gained.Amount <= 0 || unit.IsDefeated)
                continue;
            await PlayExp(s, unit, hud, gained);
        }

        await Wait(0.4f);
        await TweenAlpha(_fade, 1f, 0.25f);
        Visible = false;
        _levelPanel.Visible = false;
        _left.Reset();
        _right.Reset();
    }

    private static WeaponType? WeaponOf(BattleState s, BattleUnit unit) =>
        s.Db.TryWeapon(unit.State.EquippedItemId)?.Type;

    // ------------------------------------------------------------------ spell effects

    /// <summary>How a tome's spell looks; picked from the weapon used in this fight (all tomes share one type).</summary>
    private enum SpellLook { None, Fire, Shadow, Wind, Thunder, Light }

    private static SpellLook SpellOf(BattleState s, string? weaponName)
    {
        var weapon = s.Db.Weapons.Values.FirstOrDefault(w => w.Name == weaponName);
        if (weapon is null || weapon.Type is not (WeaponType.BlackMagic or WeaponType.WhiteMagic))
            return SpellLook.None;
        string id = weapon.Id;
        return id.Contains("fire") ? SpellLook.Fire
            : id.Contains("shadow") || id.Contains("dark") ? SpellLook.Shadow
            : id.Contains("wind") ? SpellLook.Wind
            : id.Contains("thunder") || id.Contains("bolt") ? SpellLook.Thunder
            : SpellLook.Light;
    }

    private static Color[] SpellColors(SpellLook look) => look switch
    {
        SpellLook.Fire => new[] { new Color(1f, 0.95f, 0.55f), new Color(1f, 0.6f, 0.15f), new Color(0.9f, 0.25f, 0.1f) },
        SpellLook.Shadow => new[] { new Color(0.75f, 0.45f, 1f), new Color(0.45f, 0.2f, 0.7f), new Color(0.15f, 0.05f, 0.25f) },
        SpellLook.Wind => new[] { new Color(0.85f, 1f, 0.85f), new Color(0.45f, 0.9f, 0.5f), new Color(0.2f, 0.6f, 0.3f) },
        SpellLook.Thunder => new[] { Colors.White, new Color(1f, 0.95f, 0.45f), new Color(0.6f, 0.75f, 1f) },
        _ => new[] { Colors.White, new Color(1f, 0.95f, 0.8f), new Color(0.8f, 0.9f, 1f) },
    };

    /// <summary>
    /// The spell on its way, timed to land on the cast animation's impact frame: Fire and Wind fly from the caster,
    /// Shadow gathers round the target in orbs that collapse inward, Thunder and Light fall from above. A miss carries
    /// on past (or fades) instead of landing.
    /// </summary>
    private async Task CastSpell(Fighter caster, Fighter target, bool hit, float toImpact)
    {
        var colors = SpellColors(caster.Spell);
        float travel = Math.Max(0.12f, toImpact * 0.5f);
        await Wait(Math.Max(0f, toImpact - travel));
        var from = caster.ChestPoint() + new Vector2(caster.Facing * 6f, 0f);
        var to = target.ChestPoint() + (hit ? Vector2.Zero : new Vector2(caster.Facing * 26f, -10f));
        var nodes = new List<(Node2D Node, Vector2 Start, Vector2 End)>();
        switch (caster.Spell)
        {
            case SpellLook.Fire:
                nodes.Add((Blob(colors, 4), from, to));
                break;
            case SpellLook.Wind:
                for (int i = 0; i < 3; i++)
                    nodes.Add((Blade(colors[1]), from + new Vector2(0, (i - 1) * 6f), to + new Vector2(0, (i - 1) * 4f)));
                break;
            case SpellLook.Shadow:
                for (int i = 0; i < 6; i++)
                {
                    var ring = Vector2.FromAngle(i * Mathf.Tau / 6f) * 22f;
                    nodes.Add((Blob(colors, 2), to + ring, to));
                }
                break;
            default: // Thunder and Light: from the top of the stage straight down
                nodes.Add((Blob(colors, 2), new Vector2(to.X, 0f), to));
                break;
        }
        foreach (var (node, start, _) in nodes)
        {
            node.Position = start;
            _fx.AddChild(node);
        }
        for (float t = 0f; t < travel; t += (float)GetProcessDeltaTime())
        {
            float k = t / travel;
            foreach (var (node, start, end) in nodes)
                node.Position = start.Lerp(end, k * k);
            if (caster.Spell == SpellLook.Fire && Engine.GetProcessFrames() % 2 == 0)
                _ = Particles(nodes[0].Node.Position, colors, 2, 4f, 12f, 0.3f, -20f, 1f); // trailing embers
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        if (caster.Spell is SpellLook.Thunder or SpellLook.Light && hit)
            Bolt(new Vector2(to.X, 0f), to, colors);
        foreach (var (node, _, _) in nodes)
            node.QueueFree();
    }

    /// <summary>The spell landing: a burst of its colours (embers rise, shadow sinks, sparks scatter).</summary>
    private Task SpellHit(SpellLook look, Vector2 at, float intensity)
    {
        var colors = SpellColors(look);
        int count = (int)(14 + 16 * intensity);
        return look switch
        {
            SpellLook.Fire => Particles(at, colors, count, 20f, 60f, 0.55f, -60f, 2f),
            SpellLook.Shadow => Particles(at, colors, count, 10f, 40f, 0.6f, 40f, 2f),
            SpellLook.Wind => Particles(at, colors, count, 30f, 70f, 0.45f, 0f, 1f),
            _ => Particles(at, colors, count, 25f, 70f, 0.4f, 30f, 1f),
        };
    }

    /// <summary>Square pixel particles flying out from <paramref name="at"/>, fading, pulled by <paramref name="gravity"/>.</summary>
    private async Task Particles(Vector2 at, Color[] colors, int count, float minSpeed, float maxSpeed, float life, float gravity, float size)
    {
        var rng = new RandomNumberGenerator { Seed = (ulong)Engine.GetProcessFrames() * 977u + (ulong)count };
        var bits = new List<(ColorRect Rect, Vector2 Velocity)>();
        for (int i = 0; i < count; i++)
        {
            float s = size * rng.RandiRange(1, 2);
            var rect = new ColorRect { Color = colors[rng.RandiRange(0, colors.Length - 1)], Size = new Vector2(s, s), Position = at };
            _fx.AddChild(rect);
            bits.Add((rect, Vector2.FromAngle(rng.RandfRange(0f, Mathf.Tau)) * rng.RandfRange(minSpeed, maxSpeed)));
        }
        for (float t = 0f; t < life; t += (float)GetProcessDeltaTime())
        {
            float dt = (float)GetProcessDeltaTime();
            for (int i = 0; i < bits.Count; i++)
            {
                var (rect, v) = bits[i];
                if (!IsInstanceValid(rect))
                    continue;
                rect.Position += v * dt;
                rect.Modulate = Colors.White with { A = 1f - t / life };
                bits[i] = (rect, v + new Vector2(0f, gravity * dt));
            }
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        foreach (var (rect, _) in bits)
            if (IsInstanceValid(rect))
                rect.QueueFree();
    }

    /// <summary>A small round pixel blob: a bright core inside its spell's darker rim.</summary>
    private static Node2D Blob(Color[] colors, int radius)
    {
        var node = new Node2D();
        for (int ring = radius; ring >= 0; ring--)
        {
            var color = colors[Math.Min(colors.Length - 1, ring * (colors.Length - 1) / Math.Max(1, radius))];
            node.AddChild(new ColorRect { Color = color, Size = new Vector2(ring * 2 + 1, ring * 2 + 1), Position = new Vector2(-ring, -ring) });
        }
        return node;
    }

    private static Node2D Blade(Color color)
    {
        var node = new Node2D();
        node.AddChild(new ColorRect { Color = color, Size = new Vector2(8, 1), Position = new Vector2(-4, 0) });
        node.AddChild(new ColorRect { Color = color.Lightened(0.4f), Size = new Vector2(4, 1), Position = new Vector2(-1, -1) });
        return node;
    }

    /// <summary>A jagged lightning bolt from <paramref name="from"/> down to <paramref name="to"/>, flashing briefly.</summary>
    private void Bolt(Vector2 from, Vector2 to, Color[] colors)
    {
        var rng = new RandomNumberGenerator { Seed = (ulong)Engine.GetProcessFrames() };
        var line = new Line2D { Width = 2f, DefaultColor = colors[0] };
        int steps = 7;
        for (int i = 0; i <= steps; i++)
        {
            var p = from.Lerp(to, i / (float)steps);
            line.AddPoint(i == 0 || i == steps ? p : p + new Vector2(rng.RandfRange(-5f, 5f), 0f));
        }
        _fx.AddChild(line);
        var tween = line.CreateTween();
        tween.TweenProperty(line, "modulate:a", 0f, 0.18f).SetDelay(0.06f);
        tween.TweenCallback(Callable.From(line.QueueFree));
    }

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
        if (striker.Spell != SpellLook.None)
            _ = CastSpell(striker, target, strike.Hit, toImpact);
        await Wait(toImpact);
        AudioDirector.PlaySfx(AudioCues.Impact(strike.Hit, strike.Crit));

        // 3–4. The hit: cut streak, white-out, impact burst, then shake, flash, knockback, damage pop, HP drain.
        var chest = target.ChestPoint();
        if (strike.Hit)
        {
            target.Sprite.Play("hit");
            if (melee)
                Effect(striker.Weapon is WeaponType.Fist ? "claw_rake" : "cut_streak", chest, 0.7f, !striker.FaceRight, 0.11f);
            if (striker.Spell != SpellLook.None)
                _ = SpellHit(striker.Spell, chest, intensity);
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

    /// <summary>The unit's EXP before this fight: the state already holds the result, so undo the gain.</summary>
    private static int ExpBefore(BattleUnit unit, ExpGained? gained) =>
        gained is null ? unit.State.Exp : gained.ExpAfter - gained.Amount + ExpCalculator.ExpPerLevel * gained.LevelUps.Count;

    /// <summary>Fills the EXP bar by the amount gained; each time it passes 100, plays a level-up here.</summary>
    private async Task PlayExp(BattleState s, BattleUnit unit, SideHud hud, ExpGained gained)
    {
        int exp = ExpBefore(unit, gained);
        int left = gained.Amount;
        hud.ExpGain(left);
        AudioDirector.PlaySfx("confirm");
        int levelIndex = 0;
        while (left > 0)
        {
            int step = Math.Min(left, ExpCalculator.ExpPerLevel - exp);
            await hud.AnimateExp(exp, exp + step, 0.12f + step * 0.012f);
            exp += step;
            left -= step;
            if (exp >= ExpCalculator.ExpPerLevel && levelIndex < gained.LevelUps.Count)
            {
                await ShowLevelUp(s, unit, gained, levelIndex++);
                exp = 0;
                hud.SetExp(0);
            }
            else if (exp >= ExpCalculator.ExpPerLevel)
                break; // level cap
        }
        await Wait(0.35f);
    }

    private async Task ShowLevelUp(BattleState s, BattleUnit unit, ExpGained gained, int index)
    {
        var level = gained.LevelUps[index];
        // The state already holds every level; take later levels' gains back off to show this one's stats.
        var stats = s.Stats(unit);
        for (int i = index + 1; i < gained.LevelUps.Count; i++)
            stats -= gained.LevelUps[i].Gains;
        _levelText.Text = $"LEVEL UP!\n{unit.Name}   {s.Db.Class(unit.State.ClassId).Name}  Lv {level.Level}\n";
        LevelStats.Fill(_levelStats, stats, level, 22);
        AudioDirector.PlaySfx("level_up");
        // Centre it over the stage, above the HUD panels; sized from its text, so place it after setting that.
        _levelPanel.ResetSize();
        var screen = _ui.Size;
        _levelPanel.Position = new Vector2((screen.X - _levelPanel.Size.X) / 2f, (screen.Y - 154 - _levelPanel.Size.Y) / 2f);
        _levelPanel.Visible = true;
        _levelPanel.Modulate = Colors.White with { A = 0f };
        var tween = CreateTween();
        tween.TweenProperty(_levelPanel, "modulate:a", 1f, 0.15f);
        tween.TweenInterval(1.8f);
        tween.TweenProperty(_levelPanel, "modulate:a", 0f, 0.25f);
        await ToSignal(tween, Tween.SignalName.Finished);
        _levelPanel.Visible = false;
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

    private async Task Wait(float seconds) => await ToSignal(GetTree().CreateTimer(seconds, processAlways: false), SceneTreeTimer.SignalName.Timeout);

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
        /// <summary>The spell this fighter casts in this fight (None for weapons).</summary>
        public SpellLook Spell { get; set; }
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
        private readonly HBoxContainer _expRow;
        private readonly ProgressBar _exp;
        private readonly Label _expText;
        private readonly Label _expGain;

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
            panel.OffsetTop = -154;
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

            // EXP under HP, for the player's own units (enemies don't earn EXP).
            _expRow = new HBoxContainer();
            _expRow.AddThemeConstantOverride("separation", 10);
            box.AddChild(_expRow);
            var expCaps = UiKit.Caps("EXP", 13);
            expCaps.CustomMinimumSize = new Vector2(40, 0);
            expCaps.VerticalAlignment = VerticalAlignment.Center;
            _expRow.AddChild(expCaps);
            _exp = new ProgressBar
            {
                ShowPercentage = false, MaxValue = ExpCalculator.ExpPerLevel, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0, 8), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            };
            _exp.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = UiKit.Accent });
            _expRow.AddChild(_exp);
            _expText = UiKit.Label("", 16);
            _expText.CustomMinimumSize = new Vector2(34, 0);
            _expRow.AddChild(_expText);
            _expGain = UiKit.Label("", 16, UiKit.Accent);
            _expGain.CustomMinimumSize = new Vector2(70, 0);
            _expRow.AddChild(_expGain);
        }

        /// <summary>Shows the EXP row at <paramref name="exp"/>, or hides it (null) for units that don't earn EXP.</summary>
        public void ShowExp(int? exp)
        {
            _expRow.Modulate = exp is null ? Colors.Transparent : Colors.White; // keep the row's space so panels match
            _expGain.Text = "";
            SetExp(exp ?? 0);
        }

        public void SetExp(int exp)
        {
            _exp.Value = exp;
            _expText.Text = $"{exp}";
        }

        public void ExpGain(int amount) => _expGain.Text = $"+{amount}";

        public async Task AnimateExp(int from, int to, float seconds)
        {
            var tween = _exp.CreateTween();
            tween.TweenMethod(Callable.From<double>(v => SetExp((int)v)), (double)from, (double)to, seconds);
            await _exp.ToSignal(tween, Tween.SignalName.Finished);
        }

        public void Show(CombatantForecast f, Faction faction)
        {
            _name.Text = f.Name;
            string arrow = f.TriangleAdvantage > 0 ? "  ▲" : f.TriangleAdvantage < 0 ? "  ▼" : "";
            _weapon.Text = (f.WeaponName ?? "—") + arrow;
            _values[0].Text = f.CanStrike ? $"{f.Hit}" : "--";
            _values[1].Text = !f.CanStrike ? "--" : f.Harmless ? "—" : $"{f.Damage}{(f.StrikeCount > 1 ? "×2" : "")}";
            _values[2].Text = f.CanStrike ? $"{f.Crit}" : "--";
            _fill.BgColor = faction switch
            {
                Faction.Enemy => UiKit.Bad,
                Faction.Ally => new Color(0.5f, 0.85f, 0.45f),
                Faction.Neutral => new Color(1f, 0.78f, 0.3f),
                _ => UiKit.Good,
            };
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
