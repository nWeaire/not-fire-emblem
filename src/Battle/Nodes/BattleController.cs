using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using NotFireEmblem.AI;
using NotFireEmblem.Core;
using NotFireEmblem.Grid;
using NotFireEmblem.UI;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>
/// Battle scene root. Builds the BattleState from the map, owns the TurnManager, and runs the phase loop:
/// player phases hand control to PlayerInputController, AI phases submit one unit's command at a time.
/// </summary>
public partial class BattleController : Node3D
{
    [Export] public string MapId { get; set; } = "ch1";
    /// <summary>0 = pick a random seed each battle.</summary>
    [Export] public ulong Seed { get; set; }

    private readonly Dictionary<string, UnitView> _views = new();

    public BattleState State { get; private set; } = null!;
    public TurnManager Turns { get; private set; } = null!;
    public MapView MapView { get; private set; } = null!;
    public CellOverlay RangeOverlay { get; private set; } = null!;
    public CellOverlay PathOverlay { get; private set; } = null!;
    public CellOverlay DangerOverlay { get; private set; } = null!;
    public GridCursor Cursor { get; private set; } = null!;
    public TacticsCamera Camera { get; private set; } = null!;
    public BattleHud Hud { get; private set; } = null!;
    public BattleEventPlayer EventPlayer { get; private set; } = null!;
    public PlayerInputController PlayerInput { get; private set; } = null!;
    public Node3D UnitsRoot { get; private set; } = null!;

    public override void _Ready()
    {
        MapView = GetNode<MapView>("%MapView");
        RangeOverlay = GetNode<CellOverlay>("%RangeOverlay");
        PathOverlay = GetNode<CellOverlay>("%PathOverlay");
        DangerOverlay = GetNode<CellOverlay>("%DangerOverlay");
        Cursor = GetNode<GridCursor>("%Cursor");
        Camera = GetNode<TacticsCamera>("%Camera");
        Hud = GetNode<BattleHud>("%Hud");
        EventPlayer = GetNode<BattleEventPlayer>("%EventPlayer");
        PlayerInput = GetNode<PlayerInputController>("%PlayerInput");
        UnitsRoot = GetNode<Node3D>("%Units");

        var db = GameSessionNode.Instance.Content;
        ulong seed = Seed;
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--seed=", StringComparison.Ordinal))
                seed = ulong.Parse(arg["--seed=".Length..]);
        if (seed == 0)
            seed = ((ulong)GD.Randi() << 32) | GD.Randi();
        State = BattleSetup.Create(db, db.Map(MapId), seed);
        Turns = new TurnManager(State);
        GD.Print($"Battle '{MapId}' seed {seed}");

        MapView.Build(State.Grid);
        RangeOverlay.Init(State.Grid);
        PathOverlay.Init(State.Grid);
        DangerOverlay.Init(State.Grid);
        Cursor.Init(State.Grid);
        foreach (var unit in State.Living)
            SpawnView(unit);

        var lord = State.LivingOf(Faction.Player).FirstOrDefault(u => u.IsLord) ?? State.LivingOf(Faction.Player).First();
        Cursor.MoveTo(lord.Pos, instant: true);
        Camera.Focus(CellWorld(lord.Pos), instant: true);
        Hud.UpdateObjective(State);
        Hud.ShowTerrain(State, lord.Pos);
        Hud.RestartRequested += () => GetTree().ReloadCurrentScene();
        PlayerInput.Init(this);

        Run(async () =>
        {
            await EventPlayer.Play(Turns.StartBattle());
            await ContinueUntilPlayerInput();
        });
    }

    public Vector3 CellWorld(GridPos p) => GridMetrics.CellCenter(State.Grid, p);

    public UnitView? ViewOf(string unitId) => _views.TryGetValue(unitId, out var v) && IsInstanceValid(v) ? v : null;

    public void RemoveView(string unitId) => _views.Remove(unitId);

    private void SpawnView(BattleUnit unit)
    {
        var view = new UnitView();
        UnitsRoot.AddChild(view);
        var cls = State.Db.Class(unit.State.ClassId);
        view.Setup(unit.Id, unit.Name, cls.Name, unit.Faction, unit.IsBoss, unit.CurrentHp, State.Stats(unit).HP);
        view.Position = CellWorld(unit.Pos);
        view.FaceToward(view.Position + (unit.Faction == Faction.Player ? Vector3.Forward : Vector3.Back));
        _views[unit.Id] = view;
    }

    /// <summary>Called by the player input controller once the player has confirmed a command.</summary>
    public void SubmitPlayerCommand(IBattleCommand command)
    {
        PlayerInput.Disable();
        Run(async () =>
        {
            await Execute(command);
            await ContinueUntilPlayerInput();
        });
    }

    private async Task<bool> Execute(IBattleCommand command)
    {
        string label = $"[T{State.Turn} {State.Phase}]";
        var result = Turns.Submit(command);
        if (!result.Ok)
        {
            GD.PushWarning($"Command rejected: {command} — {result.Error}");
            return false;
        }
        foreach (var e in result.Events)
            GD.Print($"{label} {Describe(e)}");
        await EventPlayer.Play(result.Events);
        return true;
    }

    /// <summary>Plays AI phases until it is the player's turn again or the battle ends.</summary>
    private async Task ContinueUntilPlayerInput()
    {
        while (true)
        {
            if (State.Outcome != BattleOutcome.InProgress)
            {
                PlayerInput.Disable();
                Hud.ShowResult(State.Outcome);
                return;
            }
            if (State.Phase == Faction.Player)
            {
                PlayerInput.Begin();
                return;
            }

            var decision = AiController.NextDecision(State);
            if (decision is not null)
            {
                // Show who is about to act before they move.
                Cursor.MoveTo(decision.Unit.Pos);
                Camera.Focus(CellWorld(decision.Unit.Pos));
                await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
            }
            IBattleCommand command = decision?.Command ?? new EndPhaseCommand();
            if (!await Execute(command))
                await Execute(new EndPhaseCommand()); // never stall the loop on a bad AI command
        }
    }

    private string Describe(BattleEvent e) => e switch
    {
        UnitMoved m => $"{NameOf(m.UnitId)} moves {m.Path[0]} -> {m.Path[^1]}",
        CombatResolved c => $"{NameOf(c.AttackerId)} attacks {NameOf(c.DefenderId)}: " + string.Join(" ", c.Result.Strikes.Select(x =>
            $"{(x.ByAttacker ? "A" : "D")}:{(x.Hit ? (x.Crit ? "CRIT" : "hit") : "miss")}({x.Damage})")) + $" -> HP {c.Result.AttackerHpAfter}/{c.Result.DefenderHpAfter}",
        UnitDefeated d => $"{NameOf(d.UnitId)} is defeated",
        _ => e.ToString(),
    };

    private string NameOf(string unitId) => State.TryUnit(unitId) is { } u ? $"{u.Name}[{u.Id}]" : unitId;

    /// <summary>Runs an async flow from a sync Godot callback, surfacing exceptions instead of losing them.</summary>
    private static async void Run(Func<Task> flow)
    {
        try
        {
            await flow();
        }
        catch (Exception e)
        {
            GD.PushError($"Battle flow failed: {e}");
        }
    }
}
