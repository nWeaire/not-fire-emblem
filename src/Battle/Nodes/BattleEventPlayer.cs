using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using NotFireEmblem.Audio;
using NotFireEmblem.Grid;
using NotFireEmblem.UI;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>Replays the BattleEvents a command produced, one at a time, as animations, dialogue and HUD updates.</summary>
public partial class BattleEventPlayer : Node
{
    private static readonly Color HealColor = new(0.45f, 1f, 0.55f);
    private static readonly Color InfoColor = new(0.75f, 0.9f, 1f);

    private BattleController Battle => GetParent<BattleController>();

    private IReadOnlyList<BattleEvent> _batch = System.Array.Empty<BattleEvent>();
    private int _index;
    /// <summary>EXP the battle screen already showed; the map skips its own popup for these.</summary>
    private readonly HashSet<ExpGained> _shownExp = new();

    public async Task Play(IReadOnlyList<BattleEvent> events)
    {
        _batch = events;
        for (_index = 0; _index < events.Count; _index++)
        {
            var e = events[_index];
            await PlayOne(e);
            Battle.RefreshMapState();
        }
        Battle.Hud.UpdateObjective(Battle.State);
    }

    private Task PlayOne(BattleEvent e) => e switch
    {
        PhaseChanged p => PlayPhase(p),
        UnitMoved m => PlayMove(m),
        CombatResolved c => PlayCombat(c),
        UnitDefeated d => PlayDefeat(d),
        UnitActionStateChanged a => SetDone(a),
        DialogueRequested d => PlayDialogue(d),
        UnitSpawned s => PlaySpawn(s),
        FactionChanged f => PlayFaction(f),
        ItemReceived i => Info(i.UnitId, Battle.State.Db.ItemName(i.ItemId) + (i.ToConvoy ? " (to convoy)" : ""), $"Got {Battle.State.Db.ItemName(i.ItemId)}"),
        GoldReceived g => Toast($"+{g.Amount} gold", UiKit.Accent),
        TerrainChanged => PlayTerrain(),
        UnitDamaged d => PlayDamaged(d),
        UnitTalked t => PlayTalk(t),
        CellVisited v => Info(v.UnitId, "Visited", null),
        CellSeized s => Banner("Seized!", UiKit.Accent),
        UnitEscaped x => PlayLeave(x.UnitId, "Escaped"),
        UnitDeparted d => PlayLeave(d.UnitId, null),
        CrestSevered s => PlaySevered(s),
        UnitStunnedSkip s => Info(s.UnitId, "Stunned", null),
        ExpGained x => PlayExp(x),
        UnitHealed h => PlayHeal(h),
        UnitBuffed b => Info(b.UnitId, $"{b.Source} {Bonus(b.Bonus)}", null),
        UnitRefreshed r => Info(r.UnitId, "Dawn: act again!", null),
        CrestSkillUsed c => Banner(c.Skill.ToString(), new Color(1f, 1f, 0.85f), 0.4f),
        ItemUsed u => Info(u.UnitId, Battle.State.Db.ItemName(u.ItemId), null),
        ItemsTraded t => Info(t.UnitId, "Traded", null),
        _ => Task.CompletedTask,
    };

    private async Task PlayPhase(PhaseChanged p)
    {
        // The "done" tint only means something during that faction's own phase.
        foreach (var unit in Battle.State.Living)
        {
            Battle.ViewOf(unit.Id)?.SetDone(false);
            Battle.ViewOf(unit.Id)?.SetStatus(StatusOf(unit));
        }
        Battle.Hud.UpdateObjective(Battle.State);
        AudioDirector.PlayMusic(AudioCues.PhaseMusic(p.Phase));
        AudioDirector.PlaySfx("phase_change");
        await Battle.Hud.ShowBanner($"{p.Phase} Phase", UnitView.ColorFor(p.Phase).Lightened(0.3f));
        if (p.Phase == Faction.Player && ObjectiveTour.Countdown(Battle.State) is { } countdown)
            await Battle.Hud.ShowBanner(countdown, UiKit.Accent, 0.6f);
    }

    public static string StatusOf(BattleUnit unit) =>
        unit.StunnedPhases > 0 ? "Stunned" : unit.TempBonusSource ?? "";

    private bool Visible(string unitId) => Battle.State.TryUnit(unitId) is { } u && Battle.State.CanSee(Faction.Player, u);

    private async Task PlayMove(UnitMoved m)
    {
        var view = Battle.ViewOf(m.UnitId);
        if (view is null || m.Path.Count < 2)
            return;
        var end = Battle.CellWorld(m.Path[^1]);
        if (view.Position.DistanceSquaredTo(end) < 0.001f)
            return; // the player already previewed this move
        // Units moving entirely inside the fog just appear where they stop.
        bool seenAlong = !Battle.State.Map.Fog || m.Path.Any(p => Battle.State.VisibleCells(Faction.Player).Contains(p));
        if (!seenAlong)
        {
            view.Position = end;
            return;
        }
        view.SetHidden(false);
        Battle.Camera.Focus(end);
        await view.MoveAlong(m.Path.Select(Battle.CellWorld).ToList());
    }

    private async Task PlayCombat(CombatResolved c)
    {
        var state = Battle.State;
        var attackerUnit = state.Unit(c.AttackerId);
        var defenderUnit = state.Unit(c.DefenderId);
        Battle.ViewOf(c.AttackerId)?.SetHidden(false);
        Battle.ViewOf(c.DefenderId)?.SetHidden(false);
        if (Battle.UseBattleScenes && BattleSceneView.CanShow(attackerUnit, defenderUnit))
        {
            // The fight's EXP follows it in the same batch; the battle screen shows it before closing.
            var exp = _batch.Skip(_index + 1).TakeWhile(e => e is not CombatResolved).OfType<ExpGained>()
                .Where(x => x.UnitId == c.AttackerId || x.UnitId == c.DefenderId).ToList();
            await Battle.BattleScene.Play(state, c, exp);
            foreach (var x in exp)
                _shownExp.Add(x);
            Battle.ViewOf(c.AttackerId)?.SetHp(c.Result.AttackerHpAfter, state.Stats(attackerUnit).HP);
            Battle.ViewOf(c.DefenderId)?.SetHp(c.Result.DefenderHpAfter, state.Stats(defenderUnit).HP);
            return;
        }

        var attacker = Battle.ViewOf(c.AttackerId);
        var defender = Battle.ViewOf(c.DefenderId);
        if (attacker is null || defender is null)
            return;
        Battle.Camera.Focus((attacker.Position + defender.Position) / 2f);
        attacker.FaceToward(defender.Position);
        defender.FaceToward(attacker.Position);
        Battle.Hud.ShowForecast(c.Forecast, "");

        int attackerMax = state.Stats(attackerUnit).HP;
        int defenderMax = state.Stats(defenderUnit).HP;

        foreach (var strike in c.Result.Strikes)
        {
            var striker = strike.ByAttacker ? attacker : defender;
            var target = strike.ByAttacker ? defender : attacker;
            AudioDirector.PlaySfx(AudioCues.Swing(state.Db, (strike.ByAttacker ? attackerUnit : defenderUnit).State));
            await striker.Lunge(target.Position);
            AudioDirector.PlaySfx(AudioCues.Impact(strike.Hit, strike.Crit));
            if (strike.Hit)
            {
                if (strike.Crit)
                    Battle.Hud.FlashScreen();
                Popup(target.Position, strike.Crit ? $"Critical! {strike.Damage}" : $"{strike.Damage}", strike.Crit ? new Color(1f, 0.85f, 0.2f) : Colors.White);
                target.SetHp(strike.TargetHpAfter, strike.ByAttacker ? defenderMax : attackerMax);
                await target.Flash();
            }
            else
            {
                Popup(target.Position, "Miss", new Color(0.7f, 0.8f, 1f));
                await target.Dodge(striker.Position);
            }
            await Wait(0.2f);
        }
        await Wait(0.3f);
        Battle.Hud.HideForecast();
    }

    private async Task PlayDefeat(UnitDefeated d)
    {
        var view = Battle.ViewOf(d.UnitId);
        Battle.RemoveView(d.UnitId);
        if (view is not null)
            AudioDirector.PlaySfx("kill");
        if (view is not null)
            await view.Die();
        var unit = Battle.State.TryUnit(d.UnitId);
        if (unit is { CrestBearer: true } && CrestEmblem.BearerCrest(unit.Name) is { } crest)
            await PlayCrestAbsorb(unit, crest.Motif, crest.Tier);
    }

    /// <summary>How high above a cell (world units) the crest appears and the shards land: about chest height.</summary>
    private const float CrestHeight = 1.1f;

    /// <summary>
    /// A fallen crest-bearer's crest rises over the spot, shatters into white shards, and the shards stream across
    /// the battlefield into Rowan's crest (the camera follows them to him). Shards track his place on screen every
    /// frame, so they arrive wherever he is.
    /// </summary>
    private async Task PlayCrestAbsorb(BattleUnit bearer, string motif, int tier)
    {
        var camera = Battle.Camera.Camera;
        var lord = Battle.State.LivingOf(Faction.Player).FirstOrDefault(u => u.IsLord);
        var fallAt = Battle.CellWorld(bearer.Pos) + Vector3.Up * CrestHeight;
        Battle.Hud.HideForecast();
        Battle.Camera.Focus(Battle.CellWorld(bearer.Pos));
        await Wait(0.45f);

        var layer = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        Battle.Hud.AddChild(layer);
        layer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // 1. The crest rises out of the fallen bearer, in its shrouded colour.
        const float size = 96f;
        var emblem = new CrestEmblem(motif, tier, radiant: false, size) { Modulate = Colors.White with { A = 0f } };
        layer.AddChild(emblem);
        emblem.Size = new Vector2(size, size);
        emblem.PivotOffset = emblem.Size / 2f;
        var start = camera.UnprojectPosition(fallAt) - emblem.Size / 2f;
        emblem.Position = start;
        AudioDirector.PlaySfx("crest_rise");
        var rise = CreateTween().SetParallel();
        rise.TweenProperty(emblem, "modulate:a", 1f, 0.5f);
        rise.TweenProperty(emblem, "position:y", start.Y - 70f, 1.1f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        rise.TweenProperty(emblem, "scale", Vector2.One * 1.25f, 1.1f).SetTrans(Tween.TransitionType.Sine);
        await ToSignal(rise, Tween.SignalName.Finished);
        await Wait(0.35f);

        // 2. It cracks white and shatters into pixel shards that burst outward.
        var flare = CreateTween();
        flare.TweenProperty(emblem, "modulate", new Color(3f, 3f, 3f), 0.18f);
        await ToSignal(flare, Tween.SignalName.Finished);
        AudioDirector.PlaySfx("crest_burn");
        var centre = emblem.Position + emblem.Size / 2f;
        emblem.QueueFree();
        var rng = new RandomNumberGenerator { Seed = (ulong)bearer.Id.GetHashCode() }; // visual only, never the battle RNG
        var shards = new List<(ColorRect Rect, Vector2 Velocity, float Delay)>();
        for (int i = 0; i < 28; i++)
        {
            float s = rng.RandiRange(3, 6);
            var rect = new ColorRect { Color = Colors.White, Size = new Vector2(s, s), MouseFilter = Control.MouseFilterEnum.Ignore };
            layer.AddChild(rect);
            rect.Position = centre - rect.Size / 2f;
            float angle = rng.RandfRange(0f, Mathf.Tau);
            shards.Add((rect, Vector2.FromAngle(angle) * rng.RandfRange(140f, 320f), rng.RandfRange(0f, 0.35f)));
        }
        for (float t = 0f; t < 0.5f; t += (float)GetProcessDeltaTime())
        {
            float delta = (float)GetProcessDeltaTime();
            for (int i = 0; i < shards.Count; i++)
            {
                var (rect, velocity, delay) = shards[i];
                rect.Position += velocity * delta;
                shards[i] = (rect, velocity * Mathf.Pow(0.04f, delta), delay); // they slow and hang in the air
            }
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        // 3. With no lord on the field the light just fades out.
        if (lord is null)
        {
            var fade = CreateTween().SetParallel();
            foreach (var (rect, _, _) in shards)
                fade.TweenProperty(rect, "modulate:a", 0f, 0.5f);
            await ToSignal(fade, Tween.SignalName.Finished);
            layer.QueueFree();
            return;
        }

        // 4. They stream to Rowan; the camera follows them. Each shard homes in, faster as it goes.
        Battle.Camera.Focus(Battle.CellWorld(lord.Pos));
        var lordView = Battle.ViewOf(lord.Id);
        Vector2 Target() => camera.UnprojectPosition(Battle.CellWorld(lord.Pos) + Vector3.Up * CrestHeight);
        var speed = new float[shards.Count];
        var arrived = new bool[shards.Count];
        int landed = 0;
        for (float t = 0f; landed < shards.Count && t < 4f; t += (float)GetProcessDeltaTime())
        {
            float delta = (float)GetProcessDeltaTime();
            var target = Target();
            for (int i = 0; i < shards.Count; i++)
            {
                var (rect, _, delay) = shards[i];
                if (arrived[i] || t < delay)
                    continue;
                speed[i] = Mathf.Min(speed[i] + 1400f * delta, 1500f);
                var to = target - rect.Size / 2f - rect.Position;
                float step = speed[i] * delta;
                if (to.Length() <= step)
                {
                    arrived[i] = true;
                    rect.Visible = false;
                    landed++;
                    if (landed == 1)
                        AudioDirector.PlaySfx("heal");
                    continue;
                }
                rect.Position += to.Normalized() * step;
            }
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        // 5. Rowan's crest drinks the light: it flares over him and he glows white for a moment.
        const float flareSize = 120f;
        var rowanCrest = new CrestEmblem("rowan", Mathf.Max(1, Battle.State.Options.CrestTier), radiant: true, flareSize);
        layer.AddChild(rowanCrest);
        rowanCrest.Size = new Vector2(flareSize, flareSize);
        rowanCrest.PivotOffset = rowanCrest.Size / 2f;
        rowanCrest.Position = Target() - rowanCrest.Size / 2f;
        rowanCrest.Modulate = new Color(2f, 2f, 2f, 0f);
        var glow = CreateTween();
        glow.TweenProperty(rowanCrest, "modulate:a", 1f, 0.15f);
        glow.Parallel().TweenProperty(rowanCrest, "scale", Vector2.One * 1.4f, 0.9f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        glow.TweenProperty(rowanCrest, "modulate:a", 0f, 0.5f);
        if (lordView is not null)
            _ = lordView.Glow(new Color(1.8f, 1.8f, 2f), 1f);
        await ToSignal(glow, Tween.SignalName.Finished);
        layer.QueueFree();
    }

    private Task SetDone(UnitActionStateChanged a)
    {
        Battle.ViewOf(a.UnitId)?.SetDone(a.State == ActionState.Done);
        return Task.CompletedTask;
    }

    private async Task PlayDialogue(DialogueRequested d)
    {
        if (!Battle.State.Db.Scenes.TryGetValue(d.SceneId, out var scene))
            return;
        Battle.Hud.HideForecast();
        Battle.Hud.Dialogue.OnFocus = async cell =>
        {
            Battle.Camera.Focus(Battle.CellWorld(cell));
            Battle.Cursor.MoveTo(cell);
            await Wait(0.35f);
        };
        await Battle.Hud.Dialogue.Play(scene, Battle.State.Db);
    }

    private async Task PlaySpawn(UnitSpawned s)
    {
        var unit = Battle.State.Unit(s.UnitId);
        var view = Battle.SpawnView(unit);
        if (!Battle.State.CanSee(Faction.Player, unit))
        {
            view.SetHidden(true);
            return;
        }
        Battle.Camera.Focus(Battle.CellWorld(unit.Pos));
        await view.Appear();
    }

    private async Task PlayFaction(FactionChanged f)
    {
        var unit = Battle.State.Unit(f.UnitId);
        Battle.ViewOf(f.UnitId)?.SetFaction(f.Faction);
        if (f.Faction == Faction.Player)
        {
            Battle.Hud.Toast($"{unit.Name} joined!", UiKit.Good);
            Popup(Battle.CellWorld(unit.Pos), "Joined!", UiKit.Good);
            await Wait(0.4f);
        }
    }

    private async Task PlayTerrain()
    {
        Battle.MapView.Build(Battle.State.Grid, Battle.State.Map.ArtSet);
        await Wait(0.2f);
    }

    private async Task PlayDamaged(UnitDamaged d)
    {
        var unit = Battle.State.Unit(d.UnitId);
        var view = Battle.ViewOf(d.UnitId);
        if (view is null)
            return;
        Popup(view.Position, $"{d.Amount}", Colors.White);
        view.SetHp(d.HpAfter, Battle.State.Stats(unit).HP);
        await view.Flash();
    }

    private async Task PlayTalk(UnitTalked t)
    {
        var a = Battle.ViewOf(t.UnitId);
        var b = Battle.ViewOf(t.TargetId);
        if (a is null || b is null)
            return;
        a.FaceToward(b.Position);
        b.FaceToward(a.Position);
        Battle.Camera.Focus((a.Position + b.Position) / 2f);
        await Wait(0.2f);
    }

    private async Task PlayLeave(string unitId, string? text)
    {
        var view = Battle.ViewOf(unitId);
        Battle.RemoveView(unitId);
        if (view is null)
            return;
        if (text is not null)
            Popup(view.Position, text, InfoColor);
        await view.Leave();
    }

    private async Task PlaySevered(CrestSevered s)
    {
        // A crest's fall severs its hold; a fallen ringleader (Ch 8's agitators) just leaves a crowd that calms down.
        bool crest = Battle.State.TryUnit(s.BearerId) is { CrestBearer: true };
        await Battle.Hud.ShowBanner(crest ? "Severed!" : "Calmed!", new Color(0.85f, 0.75f, 1f), 0.5f);
        foreach (var id in s.Stunned)
        {
            if (Battle.ViewOf(id) is { } view)
            {
                view.SetStatus("Stunned");
                Popup(view.Position, "Stunned", InfoColor);
            }
        }
        foreach (var id in s.Surrendered)
            _ = PlayLeave(id, crest ? "Surrenders" : "Goes home");
        await Wait(0.5f);
    }

    private async Task PlayExp(ExpGained x)
    {
        if (_shownExp.Remove(x))
        {
            // Already shown on the battle screen; just bring the map's HP bar up to date.
            var shown = Battle.State.Unit(x.UnitId);
            Battle.ViewOf(x.UnitId)?.SetHp(shown.CurrentHp, Battle.State.Stats(shown).HP);
            return;
        }
        var unit = Battle.State.Unit(x.UnitId);
        if (Battle.ViewOf(x.UnitId) is { } view)
            Popup(view.Position + new Vector3(0, 0.3f, 0), $"+{x.Amount} EXP", new Color(0.6f, 0.85f, 1f));
        foreach (var level in x.LevelUps)
        {
            var stats = Battle.State.Stats(unit);
            Battle.ViewOf(x.UnitId)?.SetHp(unit.CurrentHp, stats.HP);
            AudioDirector.PlaySfx("level_up");
            await Battle.Hud.ShowLevelUp(unit.Name, Battle.State.Db.Class(unit.State.ClassId).Name, level, stats);
        }
    }

    private async Task PlayHeal(UnitHealed h)
    {
        var unit = Battle.State.Unit(h.TargetId);
        var view = Battle.ViewOf(h.TargetId);
        if (view is null)
            return;
        if (h.SourceId != h.TargetId)
            Battle.ViewOf(h.SourceId)?.FaceToward(view.Position);
        AudioDirector.PlaySfx("heal");
        _ = HealSparkles(view.GlobalPosition + Vector3.Up * 0.7f);
        Popup(view.Position, $"+{h.Amount}", HealColor);
        view.SetHp(h.HpAfter, Battle.State.Stats(unit).HP);
        await Wait(0.35f);
    }

    /// <summary>Green and white pixel sparkles rising round a healed unit (screen space, so they keep their size).</summary>
    private async Task HealSparkles(Vector3 world)
    {
        var camera = Battle.Camera.Camera;
        var layer = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        Battle.Hud.AddChild(layer);
        var rng = new RandomNumberGenerator { Seed = (ulong)Engine.GetProcessFrames() };
        var colors = new[] { Colors.White, new Color(0.7f, 1f, 0.75f), new Color(0.35f, 0.9f, 0.45f) };
        var sparks = new List<(ColorRect Rect, Vector2 Offset, float Delay, float Rise)>();
        for (int i = 0; i < 18; i++)
        {
            float s = rng.RandiRange(1, 2) * 3f;
            var rect = new ColorRect { Color = colors[rng.RandiRange(0, colors.Length - 1)], Size = new Vector2(s, s), MouseFilter = Control.MouseFilterEnum.Ignore };
            layer.AddChild(rect);
            sparks.Add((rect, new Vector2(rng.RandfRange(-28f, 28f), rng.RandfRange(-20f, 30f)), rng.RandfRange(0f, 0.35f), rng.RandfRange(40f, 70f)));
        }
        const float life = 0.9f;
        for (float t = 0f; t < life + 0.35f; t += (float)GetProcessDeltaTime())
        {
            var centre = camera.UnprojectPosition(world);
            foreach (var (rect, offset, delay, rise) in sparks)
            {
                float k = Mathf.Clamp((t - delay) / life, 0f, 1f);
                rect.Visible = t >= delay && k < 1f;
                rect.Position = centre + offset + new Vector2(0f, -rise * k);
                rect.Modulate = Colors.White with { A = 1f - k };
            }
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        layer.QueueFree();
    }

    private async Task Info(string unitId, string text, string? toast)
    {
        if (toast is not null)
            Battle.Hud.Toast(toast, UiKit.Good);
        if (Battle.ViewOf(unitId) is { } view && Visible(unitId))
        {
            Popup(view.Position, text, InfoColor);
            await Wait(0.35f);
        }
    }

    private Task Toast(string text, Color color)
    {
        Battle.Hud.Toast(text, color);
        return Task.CompletedTask;
    }

    private Task Banner(string text, Color color, float hold = 0.7f) => Battle.Hud.ShowBanner(text, color, hold);

    private static string Bonus(StatBlock b) =>
        string.Join(" ", StatBlock.All.Where(s => b[s] != 0).Select(s => $"{s}+{b[s]}"));

    private void Popup(Vector3 at, string text, Color color)
    {
        var label = new Label3D
        {
            Text = text,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            FontSize = NotFireEmblem.UI.UiKit.Px(64),
            OutlineSize = 14,
            PixelSize = 0.006f,
            Modulate = color,
            Position = at + new Vector3(0f, 1.1f, 0f),
            RenderPriority = 2,
            OutlineRenderPriority = 1,
        };
        Battle.AddChild(label);
        var tween = label.CreateTween();
        tween.TweenProperty(label, "position", label.Position + new Vector3(0f, 0.6f, 0f), 0.7f).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(label, "modulate:a", 0f, 0.7f).SetDelay(0.3f);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    private async Task Wait(float seconds) => await ToSignal(GetTree().CreateTimer(seconds, processAlways: false), SceneTreeTimer.SignalName.Timeout);
}
