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

    public async Task Play(IReadOnlyList<BattleEvent> events)
    {
        foreach (var e in events)
        {
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
    }

    private string StatusOf(BattleUnit unit) =>
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
            await Battle.BattleScene.Play(state, c);
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
        Battle.MapView.Build(Battle.State.Grid);
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

    private async Task PlayLeave(string unitId, string text)
    {
        var view = Battle.ViewOf(unitId);
        Battle.RemoveView(unitId);
        if (view is null)
            return;
        Popup(view.Position, text, InfoColor);
        await view.Leave();
    }

    private async Task PlaySevered(CrestSevered s)
    {
        await Battle.Hud.ShowBanner("Severed!", new Color(0.85f, 0.75f, 1f), 0.5f);
        foreach (var id in s.Stunned)
        {
            if (Battle.ViewOf(id) is { } view)
            {
                view.SetStatus("Stunned");
                Popup(view.Position, "Stunned", InfoColor);
            }
        }
        foreach (var id in s.Surrendered)
            _ = PlayLeave(id, "Surrenders");
        await Wait(0.5f);
    }

    private async Task PlayExp(ExpGained x)
    {
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
        Popup(view.Position, $"+{h.Amount}", HealColor);
        view.SetHp(h.HpAfter, Battle.State.Stats(unit).HP);
        await Wait(0.35f);
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
            FontSize = 64,
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

    private async Task Wait(float seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
}
