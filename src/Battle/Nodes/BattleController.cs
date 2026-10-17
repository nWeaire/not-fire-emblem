using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using NotFireEmblem.AI;
using NotFireEmblem.Audio;
using NotFireEmblem.Core;
using NotFireEmblem.Grid;
using NotFireEmblem.UI;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>
/// Battle scene root. Builds the BattleState (from the campaign's pending battle, a skirmish map, or MapId), owns
/// the TurnManager, and runs the phase loop: player phases hand control to PlayerInputController, AI phases submit
/// one unit's command at a time. At the end it hands a won campaign battle back to CampaignFlow.
/// </summary>
public partial class BattleController : Node3D
{
    [Export] public string MapId { get; set; } = "test_ford";
    /// <summary>0 = pick a random seed each battle.</summary>
    [Export] public ulong Seed { get; set; }
    /// <summary>Play fights in the side-view battle screen (when both units have battle art). Toggled with B.</summary>
    /// <summary>Show fights on the side-view battle screen; follows (and the B key updates) the saved setting.</summary>
    public bool UseBattleScenes { get => Settings.BattleScenes; set => Settings.BattleScenes = value; }

    /// <summary>Who single-map battles deploy when the map has deploy slots, in order.</summary>
    private static readonly string[] SkirmishParty = { "rowan", "aldric", "kai", "mira", "brann", "tamsin", "elise", "dorran", "sable" };

    private readonly Dictionary<string, UnitView> _views = new();
    private AiWeights _aiWeights = AiWeights.Default;

    public BattleState State { get; private set; } = null!;
    public TurnManager Turns { get; private set; } = null!;
    public MapView MapView { get; private set; } = null!;
    public CellOverlay RangeOverlay { get; private set; } = null!;
    public CellOverlay PathOverlay { get; private set; } = null!;
    public CellOverlay DangerOverlay { get; private set; } = null!;
    public CellOverlay FogOverlay { get; private set; } = null!;
    public CellOverlay MarkerOverlay { get; private set; } = null!;
    public GridCursor Cursor { get; private set; } = null!;
    public TacticsCamera Camera { get; private set; } = null!;
    public BattleHud Hud { get; private set; } = null!;
    public BattleSceneView BattleScene { get; private set; } = null!;
    public BattleEventPlayer EventPlayer { get; private set; } = null!;
    public PlayerInputController PlayerInput { get; private set; } = null!;
    public Node3D UnitsRoot { get; private set; } = null!;
    /// <summary>True when this battle belongs to the campaign (results are written back on victory).</summary>
    public bool IsCampaign { get; private set; }

    public override void _Ready()
    {
        MapView = GetNode<MapView>("%MapView");
        RangeOverlay = GetNode<CellOverlay>("%RangeOverlay");
        PathOverlay = GetNode<CellOverlay>("%PathOverlay");
        DangerOverlay = GetNode<CellOverlay>("%DangerOverlay");
        FogOverlay = GetNode<CellOverlay>("%FogOverlay");
        MarkerOverlay = GetNode<CellOverlay>("%MarkerOverlay");
        Cursor = GetNode<GridCursor>("%Cursor");
        Camera = GetNode<TacticsCamera>("%Camera");
        Hud = GetNode<BattleHud>("%Hud");
        BattleScene = GetNode<BattleSceneView>("%BattleScene");
        EventPlayer = GetNode<BattleEventPlayer>("%EventPlayer");
        PlayerInput = GetNode<PlayerInputController>("%PlayerInput");
        UnitsRoot = GetNode<Node3D>("%Units");

        var node = GameSessionNode.Instance;
        var db = node.Content;
        if (node.PendingBattle is { } launch)
        {
            IsCampaign = true;
            State = BattleSetup.Create(db, launch.Map, launch.Seed, launch.Roster, launch.Options, launch.Deployed);
            GD.Print($"Battle '{launch.Map.Id}' (campaign) seed {launch.Seed}");
        }
        else
        {
            ulong seed = Seed;
            string mapId = node.SkirmishMapId ?? MapId;
            foreach (var arg in OS.GetCmdlineUserArgs())
            {
                if (arg.StartsWith("--seed=", StringComparison.Ordinal))
                    seed = ulong.Parse(arg["--seed=".Length..]);
                else if (arg.StartsWith("--map=", StringComparison.Ordinal))
                    mapId = arg["--map=".Length..];
            }
            if (seed == 0)
                seed = ((ulong)GD.Randi() << 32) | GD.Randi();
            // Single maps outside the campaign: Rowan has the Kindled Crest except in the Prologue.
            var options = new BattleOptions(CrestTier: mapId == "prologue" ? 0 : 1);
            var map = db.Map(mapId);
            // Maps that rely on deployment get a default party, fresh from the unit definitions.
            var spawned = map.Spawns.Select(sp => sp.UnitId).OfType<string>().ToHashSet();
            var party = SkirmishParty.Where(id => db.Units.ContainsKey(id) && !spawned.Contains(id)).Take(map.Slots.Count).ToList();
            var roster = party.ToDictionary(id => id, id => UnitState.FromDef(db.Unit(id)));
            State = BattleSetup.Create(db, map, seed, roster, options, party);
            GD.Print($"Battle '{mapId}' seed {seed}");
        }
        if (State.Options.Difficulty == Difficulty.Hard)
            _aiWeights = AiWeights.Aggressive;
        if (DevTools.StartExp is { } startExp)
            foreach (var unit in State.LivingOf(Faction.Player).Where(u => u.IsRosterUnit))
                unit.State.Exp = Math.Clamp(startExp, 0, ExpCalculator.ExpPerLevel - 1);
        Turns = new TurnManager(State);

        MapView.Build(State.Grid, State.Map.ArtSet);
        if (DevTools.HideUnits)
        {
            UnitsRoot.Visible = false;
            Hud.Visible = false;
            Cursor.Visible = false;
        }
        if (State.Map.Lighting == "night")
            AddNightTint();
        foreach (var overlay in new[] { RangeOverlay, PathOverlay, DangerOverlay, FogOverlay, MarkerOverlay })
            overlay.Init(State.Grid);
        Cursor.Init(State.Grid);
        UpdateCrestSprites(rebuild: false);
        foreach (var unit in State.Living)
            SpawnView(unit);

        var lord = State.LivingOf(Faction.Player).FirstOrDefault(u => u.IsLord) ?? State.LivingOf(Faction.Player).First();
        Cursor.MoveTo(lord.Pos, instant: true);
        Camera.Focus(CellWorld(lord.Pos), instant: true);
        Hud.UpdateObjective(State);
        Hud.ShowTerrain(State, lord.Pos);
        PlayerInput.Init(this);
        RefreshMapState();

        Run(async () =>
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await EventPlayer.Play(Turns.StartBattle());
            await ContinueUntilPlayerInput();
        });
    }

    /// <summary>Night battles: the map is multiplied by a moonlit blue (the HUD sits on its own layer above).</summary>
    private void AddNightTint()
    {
        var layer = new CanvasLayer { Layer = 0, Name = "NightTint" };
        var shader = new Shader
        {
            Code = "shader_type canvas_item; render_mode blend_mul; void fragment() { COLOR = vec4(0.45, 0.52, 0.82, 1.0); }",
        };
        var rect = new ColorRect { Material = new ShaderMaterial { Shader = shader }, MouseFilter = Control.MouseFilterEnum.Ignore };
        rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(rect);
        AddChild(layer);
    }

    public Vector3 CellWorld(GridPos p) => GridMetrics.CellCenter(State.Grid, p);

    public UnitView? ViewOf(string unitId) => _views.TryGetValue(unitId, out var v) && IsInstanceValid(v) ? v : null;

    public void RemoveView(string unitId) => _views.Remove(unitId);

    public UnitView SpawnView(BattleUnit unit)
    {
        var view = new UnitView();
        UnitsRoot.AddChild(view);
        var cls = State.Db.Class(unit.State.ClassId);
        view.Setup(unit.Id, unit.Name, cls.Name, unit.Faction, unit.IsBoss, unit.CurrentHp, State.Stats(unit).HP, SpriteLibrary.Map(unit));
        view.Position = CellWorld(unit.Pos);
        view.FaceToward(view.Position + (unit.Faction == Faction.Player ? Vector3.Forward : Vector3.Back));
        _views[unit.Id] = view;
        return view;
    }

    /// <summary>
    /// Rowan's crest shows on his sprite from the moment it awakens: a crest tier from the campaign, or the
    /// prologue's crest_tier_1 flag mid-battle. Rebuilds his view when that changes.
    /// </summary>
    private void UpdateCrestSprites(bool rebuild)
    {
        bool awake = State.Options.CrestTier > 0 || State.Flags.Contains("crest_tier_1");
        foreach (var unit in State.Living.Where(u => u.IsLord && u.Faction == Faction.Player && u.ShowsCrest != awake))
        {
            unit.ShowsCrest = awake;
            if (!rebuild || ViewOf(unit.Id) is not { } old)
                continue;
            old.QueueFree();
            SpawnView(unit);
        }
    }

    /// <summary>Re-applies fog (hidden cells and enemy views) and the objective/visit markers.</summary>
    public void RefreshMapState()
    {
        UpdateCrestSprites(rebuild: true);
        if (State.Map.Fog)
        {
            var visible = State.VisibleCells(Faction.Player);
            FogOverlay.ShowCells(State.Grid.AllPositions().Where(p => !visible.Contains(p)).Select(p => (p, CellOverlay.FogColor)).ToList());
        }
        foreach (var unit in State.Living)
            ViewOf(unit.Id)?.SetHidden(!State.CanSee(Faction.Player, unit));

        var markers = new List<(GridPos, Color)>();
        var goals = State.Map.Goals;
        if (goals.Victory is VictoryKind.Seize or VictoryKind.Escape)
            markers.AddRange(goals.Cells.Select(c => (c, CellOverlay.ObjectiveColor)));
        foreach (var trigger in State.Map.AllTriggers.Where(t => !State.FiredTriggers.Contains(t.Id)))
            if (trigger.Condition is VisitCondition v)
                markers.Add((v.Cell, CellOverlay.VisitColor));
        MarkerOverlay.ShowCells(markers);
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
                await EndBattle();
                return;
            }
            if (State.Phase == Faction.Player && !DevTools.Autoplay)
            {
                PlayerInput.Begin();
                return;
            }

            var decision = State.Phase == Faction.Player ? AutoplayDecision() : AiController.NextDecision(State, _aiWeights);
            if (decision is not null && State.CanSee(Faction.Player, decision.Unit))
            {
                // Show who is about to act before they move (unless they're hidden in the fog).
                Cursor.MoveTo(decision.Unit.Pos);
                Camera.Focus(CellWorld(decision.Unit.Pos));
                await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
            }
            IBattleCommand command = decision?.Command ?? new EndPhaseCommand();
            if (!await Execute(command))
                await Execute(new EndPhaseCommand()); // never stall the loop on a bad AI command
        }
    }

    /// <summary>
    /// Dev autoplay for the player's side: seize or escape when the lord can, talk to recruits in reach,
    /// otherwise act like the enemy AI.
    /// </summary>
    private AiDecision? AutoplayDecision()
    {
        if (State.Turn > 60)
        {
            GD.PrintErr($"AUTOPLAY STUCK on '{State.Map.Id}' at turn {State.Turn}");
            GetTree().Quit();
            return null;
        }
        foreach (var unit in State.LivingOf(Faction.Player).Where(u => u.ActionState != ActionState.Done).OrderBy(u => u.Id))
        {
            var reach = unit.ActionState == ActionState.Moved ? null : State.Reach(unit);
            var cells = reach?.StopCells ?? (IEnumerable<GridPos>)new[] { unit.Pos };
            foreach (var cell in cells.OrderBy(c => c.Y).ThenBy(c => c.X))
            {
                if (ActionRules.CanSeize(State, unit, cell))
                    return new AiDecision(unit, new ActCommand(unit.Id, cell, new SeizeAction()), null);
                // The lord leaves last: once he's gone, the others can't be escorted out.
                if (ActionRules.CanEscape(State, unit, cell)
                    && (!unit.IsLord || State.Map.Goals.EscapeKeys.All(k => State.ByKey(k) is not { IsOnMap: true } other || other == unit)))
                    return new AiDecision(unit, new ActCommand(unit.Id, cell, new EscapeAction()), null);
                if (EventDirector.TalkTargets(State, unit, cell).FirstOrDefault() is { } talk)
                    return new AiDecision(unit, new ActCommand(unit.Id, cell, new TalkAction(talk.Id)), null);
            }
        }
        // Units like the seize tile (often a fort) and would park on it forever: clear it for the lord.
        if (State.Map.Goals.Victory == VictoryKind.Seize)
            foreach (var unit in State.LivingOf(Faction.Player).Where(u => !u.IsLord && u.ActionState == ActionState.Ready
                                                                           && State.Map.Goals.Cells.Contains(u.Pos)))
            {
                var off = State.Reach(unit).StopCells.Where(c => !State.Map.Goals.Cells.Contains(c))
                    .OrderBy(c => c.DistanceTo(unit.Pos)).FirstOrDefault(c => c != unit.Pos);
                if (off != default)
                    return new AiDecision(unit, new ActCommand(unit.Id, off, new WaitAction()), null);
            }
        // Look after the party first: heal the hurt, and pull back the wounded (the lord early, even without a
        // vulnerary; anyone else only when they can drink one).
        foreach (var unit in State.LivingOf(Faction.Player).Where(u => u.ActionState == ActionState.Ready).OrderBy(u => u.Id))
        {
            var reach = State.Reach(unit);
            var care = CarefulAi.Heal(State, unit, reach)
                       ?? CarefulAi.Recover(State, unit, reach, IsVital(unit) || MustSurvive(unit) ? 0.5 : 0.35, needsItem: !IsVital(unit));
            if (care is not null && care.Validate(State).Ok)
                return new AiDecision(unit, care, null);
        }
        // Everyone else fights like the enemy AI; vital units (the lord, and anyone who must escape) are far more
        // careful. Units that only must survive still fight, but pull back sooner (above).
        var decisions = State.LivingOf(Faction.Player).Where(u => u.ActionState != ActionState.Done)
            .Select(u => IsVital(u) ? LordDecision(u)
                : CarefulAi.Shelter(State, u, State.Reach(u)) is { } shelter ? new AiDecision(u, shelter, null)
                : AiController.Decide(State, u, AutoplayWeights)).ToList();
        return decisions.Where(d => d.AttackScore is not null).OrderByDescending(d => d.AttackScore).FirstOrDefault()
               ?? decisions.FirstOrDefault(d => !IsVital(d.Unit)) ?? decisions.FirstOrDefault();
    }

    /// <summary>The lord, and units the map's escape objective names.</summary>
    private bool IsVital(BattleUnit unit) =>
        unit.IsLord || State.Map.Goals.EscapeKeys.Any(key => State.ByKey(key) == unit);

    /// <summary>Units whose death loses the map ("die KEY").</summary>
    private bool MustSurvive(BattleUnit unit) => State.Map.Goals.MustSurvive.Any(key => State.ByKey(key) == unit);

    private AiDecision LordDecision(BattleUnit lord)
    {
        var goals = State.Map.Goals;
        bool escaping = goals.Victory == VictoryKind.Escape && goals.EscapeKeys.Any(k => State.ByKey(k) == lord || lord.IsLord);
        var d = AiController.Decide(State, lord, LordWeights);
        // On an escape map the units who must leave just run; elsewhere they fight when it's safe.
        if (d.AttackScore is > 0 && !escaping)
            return d;
        // Seize/Escape maps: walk toward the objective (seize once the way is fairly clear; escape at once).
        if (escaping || goals.Victory == VictoryKind.Seize && State.LivingOf(Faction.Enemy).Count() <= 3)
        {
            var reach = State.Reach(lord);
            var goal = goals.Cells.OrderBy(c => c.DistanceTo(lord.Pos)).First();
            var path = Pathfinder.FindPath(State.Grid, lord.Pos, goal, State.MoveType(lord));
            if (path is not null)
                for (int i = path.Count - 1; i > 0; i--)
                    if (reach.CanStop(path[i]))
                        return new AiDecision(lord, new ActCommand(lord.Id, path[i], new WaitAction()), null);
        }
        return new AiDecision(lord, new ActCommand(lord.Id, lord.Pos, new WaitAction()), null);
    }

    private static readonly AiWeights AutoplayWeights = new(CounterDamage: 1.2, SelfDeath: 200.0, Danger: 3.0);
    private static readonly AiWeights LordWeights = new(CounterDamage: 2.0, SelfDeath: 500.0, Danger: 12.0);

    private async Task EndBattle()
    {
        var node = GameSessionNode.Instance;
        AudioDirector.PlayMusic(State.Outcome == BattleOutcome.Victory ? "victory" : "defeat", loop: false);
        if (IsCampaign && State.Outcome == BattleOutcome.Victory)
        {
            await Hud.ShowBanner("Victory!", UiKit.Accent, 1.2f);
            CampaignFlow.BattleWon(State);
            return;
        }
        var buttons = new List<(string, Action)>();
        if (IsCampaign)
        {
            buttons.Add(("Retry Battle", () =>
            {
                var runner = node.Runner!;
                // A new seed per attempt, so a retry isn't a replay of the same rolls.
                runner.Session.BattleCount++;
                node.PendingBattle = runner.Launch(node.PendingBattleStep!, node.PendingBattle!.Deployed);
                GetTree().ReloadCurrentScene();
            }));
        }
        else
        {
            buttons.Add(("Play Again", () => GetTree().ReloadCurrentScene()));
        }
        buttons.Add(("Title Screen", () =>
        {
            node.EndSession();
            node.SkirmishMapId = null;
            node.GoTo(GameSessionNode.TitleScene);
        }));
        Hud.ShowResult(State.Outcome, buttons);
        if (DevTools.Autoplay)
        {
            GD.Print($"AUTOPLAY RESULT: {State.Outcome} on turn {State.Turn}");
            if (DevTools.AutoAdvance)
                GetTree().CreateTimer(0.5).Timeout += buttons[0].Item2;
        }
    }

    private string Describe(BattleEvent e) => e switch
    {
        UnitMoved m => $"{NameOf(m.UnitId)} moves {m.Path[0]} -> {m.Path[^1]}",
        CombatResolved c => $"{NameOf(c.AttackerId)} attacks {NameOf(c.DefenderId)}: " + string.Join(" ", c.Result.Strikes.Select(x =>
            $"{(x.ByAttacker ? "A" : "D")}:{(x.Hit ? (x.Crit ? "CRIT" : "hit") : "miss")}({x.Damage})")) + $" -> HP {c.Result.AttackerHpAfter}/{c.Result.DefenderHpAfter}",
        UnitDefeated d => $"{NameOf(d.UnitId)} is defeated",
        ExpGained x => $"{NameOf(x.UnitId)} +{x.Amount} EXP" + string.Concat(x.LevelUps.Select(l => $" -> Lv {l.Level}")),
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
