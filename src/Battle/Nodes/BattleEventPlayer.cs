using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using NotFireEmblem.UI;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>Replays the BattleEvents a command produced, one at a time, as animations and HUD updates.</summary>
public partial class BattleEventPlayer : Node
{
    private BattleController Battle => GetParent<BattleController>();

    public async Task Play(IReadOnlyList<BattleEvent> events)
    {
        foreach (var e in events)
            await PlayOne(e);
        Battle.Hud.UpdateObjective(Battle.State);
    }

    private Task PlayOne(BattleEvent e) => e switch
    {
        PhaseChanged p => PlayPhase(p),
        UnitMoved m => PlayMove(m),
        CombatResolved c => PlayCombat(c),
        UnitDefeated d => PlayDefeat(d),
        UnitActionStateChanged a => SetDone(a),
        _ => Task.CompletedTask,
    };

    private async Task PlayPhase(PhaseChanged p)
    {
        // The "done" tint only means something during that faction's own phase.
        foreach (var unit in Battle.State.Living)
            Battle.ViewOf(unit.Id)?.SetDone(false);
        Battle.Hud.UpdateObjective(Battle.State);
        await Battle.Hud.ShowBanner($"{p.Phase} Phase", UnitView.ColorFor(p.Phase).Lightened(0.3f));
    }

    private async Task PlayMove(UnitMoved m)
    {
        var view = Battle.ViewOf(m.UnitId);
        if (view is null || m.Path.Count < 2)
            return;
        var end = Battle.CellWorld(m.Path[^1]);
        if (view.Position.DistanceSquaredTo(end) < 0.001f)
            return; // the player already previewed this move
        Battle.Camera.Focus(end);
        await view.MoveAlong(m.Path.Select(Battle.CellWorld).ToList());
    }

    private async Task PlayCombat(CombatResolved c)
    {
        var state = Battle.State;
        var attackerUnit = state.Unit(c.AttackerId);
        var defenderUnit = state.Unit(c.DefenderId);
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

        int attackerMax = state.Stats(state.Unit(c.AttackerId)).HP;
        int defenderMax = state.Stats(state.Unit(c.DefenderId)).HP;

        foreach (var strike in c.Result.Strikes)
        {
            var striker = strike.ByAttacker ? attacker : defender;
            var target = strike.ByAttacker ? defender : attacker;
            await striker.Lunge(target.Position);
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
            await view.Die();
    }

    private Task SetDone(UnitActionStateChanged a)
    {
        Battle.ViewOf(a.UnitId)?.SetDone(a.State == ActionState.Done);
        return Task.CompletedTask;
    }

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
