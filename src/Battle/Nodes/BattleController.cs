using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using NotFireEmblem.AI;
using NotFireEmblem.Audio;
using NotFireEmblem.Campaign;
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

    /// <summary>
    /// Who single-map battles deploy when the map has deploy slots, in order (after the campaign's forced units for
    /// that map, e.g. Seraphine in Ch 11).
    /// </summary>
    private static readonly string[] SkirmishParty =
        { "rowan", "aldric", "kai", "mira", "brann", "tamsin", "elise", "dorran", "sable", "wren", "pip", "seraphine", "ysra", "halde" };

    /// <summary>
    /// What named units carry by Ch 2, which unit definitions don't: Mira's Fire (Ch 1) and, for testing, a Steel Sword
    /// for Rowan (the campaign gives an Iron Sword on Osmund's tour; a Steel Sword is affordable by Ch 3). Single
    /// battles after Ch 1 hand them out so they play like the campaign.
    /// </summary>
    private static readonly Dictionary<string, string> StoryItems = new() { ["mira"] = "fire", ["rowan"] = "steel_sword" };

    /// <summary>
    /// Single battles: the campaign's class changes that come before this map (Rowan becomes a Lord after Ch 1), so
    /// units have their story class, stats and look. Current HP follows the new maximum.
    /// </summary>
    private void ApplyStoryClasses(ContentDatabase db, string mapId)
    {
        var steps = db.Campaign?.Steps;
        int battle = steps?.ToList().FindIndex(st => st is BattleStep b && b.MapId == mapId || st is SideStep side && side.MapId == mapId) ?? -1;
        if (steps is null || battle < 0)
            return;
        foreach (var change in steps.Take(battle).OfType<ClassStep>())
            foreach (var unit in State.Units.Where(u => u.IsRosterUnit && u.State.Def.Id == change.UnitId))
            {
                unit.State.ClassId = change.ClassId;
                unit.CurrentHp = State.Stats(unit).HP;
            }
    }

    /// <summary>The crest tier the campaign has reached before this map (its "crest N" steps); 1 off the campaign.</summary>
    private static int StoryCrestTier(ContentDatabase db, string mapId)
    {
        var steps = db.Campaign?.Steps;
        int battle = steps?.ToList().FindIndex(st => st is BattleStep b && b.MapId == mapId || st is SideStep side && side.MapId == mapId) ?? -1;
        if (steps is null || battle < 0)
            return 1;
        return steps.Take(battle).OfType<CrestStep>().Select(c => c.Tier).DefaultIfEmpty(0).Max();
    }

    private void GiveStoryItems(ContentDatabase db)
    {
        foreach (var unit in State.Units.Where(u => u.IsRosterUnit && StoryItems.ContainsKey(u.State.Def.Id)))
        {
            var inventory = unit.State.Inventory;
            string item = StoryItems[unit.State.Def.Id];
            if (inventory.Contains(item) || inventory.Count >= UnitState.MaxInventory)
                continue;
            inventory.Insert(0, item);
            unit.State.EquippedIndex = 0; // the story item is the better weapon (Fire for the unarmed Mira, Iron over Old Sword)
        }
    }

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
            // Single maps: Rowan's crest as the campaign has it by then (Kindled after the Prologue, Knight's after
            // Ch 7, Lord's after Ch 13); maps outside the campaign get the Kindled Crest.
            var options = new BattleOptions(CrestTier: StoryCrestTier(db, mapId));
            var map = db.Map(mapId);
            // Maps that rely on deployment get a default party, fresh from the unit definitions.
            var spawned = map.Spawns.Select(sp => sp.UnitId).OfType<string>().ToHashSet();
            var step = db.Campaign?.Steps.OfType<BattleStep>().FirstOrDefault(b => b.MapId == mapId)
                       ?? db.Campaign?.Steps.OfType<SideStep>().FirstOrDefault(b => b.MapId == mapId)?.Battle;
            var forced = step?.Forced ?? Array.Empty<string>();
            // As many as the campaign lets this map deploy (never over CampaignRunner.MaxDeploy), the map's own player
            // spawns included.
            int fixedCount = map.Spawns.Count(sp => sp.Faction == Faction.Player && sp.Group is null && sp.UnitId is not null);
            int deploy = Math.Min(step is { Deploy: > 0 } ? step.Deploy : CampaignRunner.MaxDeploy, CampaignRunner.MaxDeploy);
            var party = forced.Concat(SkirmishParty).Distinct()
                .Where(id => db.Units.ContainsKey(id) && !spawned.Contains(id))
                .Take(Math.Min(map.Slots.Count, Math.Max(0, deploy - fixedCount))).ToList();
            var roster = party.ToDictionary(id => id, id => UnitState.FromDef(db.Unit(id)));
            State = BattleSetup.Create(db, map, seed, roster, options, party);
            ApplyStoryClasses(db, mapId);
            if (mapId is not ("prologue" or "ch1" or "test_ford"))
                GiveStoryItems(db);
            if (DevTools.PartyLevelFor(mapId) is { } level)
            {
                // Everyone on the player's side, recruits included, at a campaign-like level (not enemies).
                foreach (var unit in State.Units.Where(u => u.IsRosterUnit && u.Faction.IsPlayerSide()))
                {
                    LevelUpService.RaiseLikeCampaign(db, unit.State, level, State.Options.CrestTier);
                    BattleProgress.ReequipIfNeeded(State, unit);
                    BattleSetup.RefillUses(db, unit, State.Options.CrestTier);
                    unit.CurrentHp = State.Stats(unit).HP;
                }
                GD.Print($"Party raised to level {level}");
            }
            // Story classes and --level promotions change classes after setup: refresh the outfit fallbacks.
            foreach (var unit in State.Units.Where(u => u.IsRosterUnit))
                unit.EarlierClasses = Outfits.Earlier(db, unit.State.ClassId);
            GD.Print($"Battle '{mapId}' seed {seed}");
        }
        if (State.Options.Difficulty == Difficulty.Hard)
            _aiWeights = AiWeights.Aggressive;
        if (DevTools.StartExp is { } startExp)
            foreach (var unit in State.LivingOf(Faction.Player).Where(u => u.IsRosterUnit))
                unit.State.Exp = Math.Clamp(startExp, 0, ExpCalculator.ExpPerLevel - 1);
        Turns = new TurnManager(State);
        bool resumed = IsCampaign && node.PendingReplay is not null && Resume(node);

        MapView.Build(State.Grid, State.Map.ArtSet);
        MapView.Cursor = Cursor;
        if (DevTools.HideUnits)
        {
            UnitsRoot.Visible = false;
            Hud.Visible = false;
            Cursor.Visible = false;
        }
        if (State.Map.Lighting == "night")
            AddNightTint();
        else if (State.Map.Lighting == "snow")
            AddSnow();
        foreach (var overlay in new[] { RangeOverlay, PathOverlay, DangerOverlay, FogOverlay, MarkerOverlay })
            overlay.Init(State.Grid);
        if (State.Map.Fog)
            BuildFogLayers();
        Cursor.Init(State.Grid);
        UpdateCrestSprites(rebuild: false);
        foreach (var unit in State.Living)
            SpawnView(unit);

        var lord = State.LivingOf(Faction.Player).FirstOrDefault(u => u.IsLord) ?? State.LivingOf(Faction.Player).First();
        var start = DevTools.FocusCell ?? lord.Pos;
        Cursor.MoveTo(start, instant: true);
        Camera.Focus(CellWorld(start), instant: true);
        Hud.UpdateObjective(State);
        Hud.ShowTerrain(State, lord.Pos);
        PlayerInput.Init(this);
        RefreshMapState();

        Run(async () =>
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (resumed)
            {
                await ShowResumed();
            }
            else
            {
                await ShowObjectives(lord);
                await ShowCrestBlessing(lord);
                await EventPlayer.Play(Turns.StartBattle());
            }
            await ContinueUntilPlayerInput();
        });
    }

    /// <summary>
    /// A resumed suspend: replays its commands with no visuals, so the map and units are built as they stood. If the
    /// content has changed and a command no longer fits, the battle starts over from turn 1 instead.
    /// </summary>
    private bool Resume(GameSessionNode node)
    {
        var replay = node.PendingReplay!;
        node.PendingReplay = null;
        if (Turns.Replay(replay))
        {
            GD.Print($"Resumed '{State.Map.Id}' on turn {State.Turn} ({replay.Count} commands replayed)");
            return true;
        }
        GD.PushWarning("The suspended battle couldn't be replayed; it starts over.");
        var launch = node.Runner!.Launch(node.PendingBattleStep!, node.PendingBattle!.Deployed) with { Seed = node.PendingBattle.Seed };
        node.PendingBattle = launch;
        State = BattleSetup.Create(node.Content, launch.Map, launch.Seed, launch.Roster, launch.Options, launch.Deployed);
        Turns = new TurnManager(State);
        return false;
    }

    /// <summary>After a resume: the views catch up with what the replay left (who has acted, buffs), then the turn.</summary>
    private async Task ShowResumed()
    {
        foreach (var unit in State.Living)
        {
            ViewOf(unit.Id)?.SetDone(unit.Faction == State.Phase && unit.ActionState == ActionState.Done);
            ViewOf(unit.Id)?.SetStatus(BattleEventPlayer.StatusOf(unit));
            RefreshRecruit(unit);
        }
        AudioDirector.PlayMusic(AudioCues.PhaseMusic(State.Phase));
        await Hud.ShowBanner($"Turn {State.Turn} · {State.Phase} Phase", UnitView.ColorFor(State.Phase).Lightened(0.3f));
    }

    /// <summary>Seconds each objective-tour stop stays up unless the player presses confirm.</summary>
    private const float TourStopSeconds = 2.6f;

    private TaskCompletionSource? _tourNext;

    /// <summary>
    /// Before turn 1: the camera visits each objective and point of interest (ObjectiveTour) with a caption, its
    /// cells marked, then returns to the lord. Confirm skips to the next stop. Not shown in unattended dev runs.
    /// </summary>
    private async Task ShowObjectives(BattleUnit lord)
    {
        var stops = ObjectiveTour.Stops(State);
        if (stops.Count == 0 || DevTools.Autoplay || DevTools.SkipDialogue)
            return;
        foreach (var stop in stops)
        {
            if (stop.Cells.Count > 0)
            {
                var sum = stop.Cells.Aggregate(Vector3.Zero, (acc, c) => acc + CellWorld(c));
                Camera.Focus(sum / stop.Cells.Count);
            }
            MarkerOverlay.ShowCells(stop.Cells.Select(c => (c, CellOverlay.ObjectiveColor)).ToList());
            Hud.ShowCaption(stop.Caption);
            _tourNext = new TaskCompletionSource();
            var next = _tourNext;
            GetTree().CreateTimer(TourStopSeconds, processAlways: false).Timeout += () => next.TrySetResult();
            await next.Task;
        }
        _tourNext = null;
        Hud.ShowCaption(null);
        RefreshMapState();
        Camera.Focus(CellWorld(lord.Pos));
    }

    /// <summary>
    /// Before turn 1, once the crest has woken (session 13, user request): it rises over Rowan and its light washes over
    /// the army (a show only). The first battle at each crest tier in the campaign, the party says how it feels
    /// (crest_blessing_N, lines only from those deployed).
    /// </summary>
    private async Task ShowCrestBlessing(BattleUnit lord)
    {
        int tier = State.Options.CrestTier;
        if (tier <= 0 || !lord.IsLord || DevTools.Autoplay)
            return;
        await EventPlayer.PlayCrestBlessing(lord);
        string sceneId = $"crest_blessing_{tier}";
        if (!IsCampaign || GameSessionNode.Instance.Session is not { } session || session.SeenScenes.Contains(sceneId)
            || !State.Db.Scenes.TryGetValue(sceneId, out var scene))
            return;
        session.SeenScenes.Add(sceneId);
        var present = State.LivingOf(Faction.Player).Select(u => u.State.Def.Id).ToHashSet();
        await Hud.Dialogue.Play(scene.OnlySpeakers(present), State.Db);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed(InputSetup.Pause))
        {
            GetViewport().SetInputAsHandled();
            OpenPauseMenu();
            return;
        }
        if (_tourNext is null || !e.IsActionPressed(InputSetup.Confirm))
            return;
        GetViewport().SetInputAsHandled();
        _tourNext.TrySetResult();
    }

    private Modal? _pauseMenu;

    /// <summary>
    /// Esc: freezes the battle (enemy phase, animations and the battle screen included) under a menu to resume, open
    /// the settings, retry the battle, suspend it (campaign) or quit to the title screen. Not offered once the battle
    /// is over.
    /// </summary>
    private void OpenPauseMenu()
    {
        if (_pauseMenu is not null || State.Outcome != BattleOutcome.InProgress || Hud.Dialogue.IsPlaying)
            return;
        GetTree().Paused = true;
        var modal = new Modal("Paused") { ProcessMode = ProcessModeEnum.Always };
        _pauseMenu = modal;
        modal.Body.AddChild(UiKit.Button("Return to Battle", modal.Close, 280));
        modal.Body.AddChild(UiKit.Button("Settings", () => ShowPausePage(modal, "Settings", page =>
            page.Body.AddChild(new SettingsPanel())), 280));
        modal.Body.AddChild(UiKit.Button("Retry Battle", () => ShowPausePage(modal, "Retry this battle?", page =>
        {
            page.Body.AddChild(UiKit.Label("The battle starts over from turn 1.", 16));
            page.Body.AddChild(UiKit.Button("Retry Battle", () =>
            {
                GetTree().Paused = false;
                RetryBattle();
            }, 280));
        }), 280));
        if (IsCampaign)
            modal.Body.AddChild(UiKit.Button("Suspend", () => ShowPausePage(modal, "Suspend this battle?", page =>
            {
                page.Body.AddChild(UiKit.Label("The battle is saved as it stands. Resume it from the title screen.", 16));
                page.Body.AddChild(UiKit.Button("Suspend", SuspendBattle, 280));
            }), 280));
        modal.Body.AddChild(UiKit.Button("Quit to Title", () => ShowPausePage(modal, "Quit to the title screen?", page =>
        {
            page.Body.AddChild(UiKit.Label(IsCampaign ? "This battle's progress is lost. The campaign resumes from your last save."
                                                       : "This battle's progress is lost.", 16));
            page.Body.AddChild(UiKit.Button("Quit to Title", QuitToTitle, 280));
        }), 280));
        modal.Closed += () =>
        {
            _pauseMenu = null;
            GetTree().Paused = false;
        };
        Hud.AddChild(modal);
    }

    /// <summary>A page over the pause menu (settings, the quit check) with a Back button; the battle stays paused.</summary>
    private void ShowPausePage(Modal pause, string title, Action<Modal> fill)
    {
        pause.Visible = false;
        var page = new Modal(title) { ProcessMode = ProcessModeEnum.Always };
        fill(page);
        page.Body.AddChild(UiKit.Button("Back", page.Close, 280));
        page.Closed += () =>
        {
            if (!IsInstanceValid(pause))
                return;
            pause.Visible = true;
            pause.CallDeferred(Modal.MethodName.FocusFirst);
        };
        Hud.AddChild(page);
    }

    private void QuitToTitle()
    {
        if (IsCampaign)
            SaveService.ClearSuspend();
        LeaveToTitle();
    }

    /// <summary>Saves the battle (the session, seed, deployment and command log) and leaves for the title screen.</summary>
    private void SuspendBattle()
    {
        var node = GameSessionNode.Instance;
        if (SaveService.Suspend(SuspendData.FromBattle(node.Session!, node.PendingBattle!, State, node.PendingSide?.MapId)) is { } error)
        {
            GD.PushError(error);
            return;
        }
        GD.Print($"Suspended '{State.Map.Id}' on turn {State.Turn} ({State.CommandLog.Count} commands)");
        LeaveToTitle();
    }

    private void LeaveToTitle()
    {
        GetTree().Paused = false;
        var node = GameSessionNode.Instance;
        node.EndSession();
        node.SkirmishMapId = null;
        node.GoTo(GameSessionNode.TitleScene);
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

    /// <summary>
    /// Snow battles (Ch 14): the map washed toward a cold white (partly desaturated, so team colours still read) and
    /// snow drifting down over it; the HUD stays above. Stands in for snow terrain art (docs/ART_NEEDED.md).
    /// </summary>
    private void AddSnow()
    {
        var layer = new CanvasLayer { Layer = 0, Name = "Snow" };
        var shader = new Shader
        {
            Code = "shader_type canvas_item; uniform sampler2D screen : hint_screen_texture, filter_linear_mipmap;"
                   + " void fragment() { vec3 c = texture(screen, SCREEN_UV).rgb; float l = dot(c, vec3(0.3, 0.59, 0.11));"
                   + " vec3 cold = mix(vec3(l), vec3(1.0), 0.3) * vec3(0.93, 0.97, 1.06);"
                   + " COLOR = vec4(mix(c, cold, 0.5), 1.0); }",
        };
        var tint = new ColorRect { Material = new ShaderMaterial { Shader = shader }, MouseFilter = Control.MouseFilterEnum.Ignore };
        tint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(tint);
        var size = GetViewport().GetVisibleRect().Size;
        var flakes = new CpuParticles2D
        {
            Amount = 220,
            Lifetime = 9,
            Preprocess = 9,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
            EmissionRectExtents = new Vector2(size.X * 0.6f, 4),
            Position = new Vector2(size.X / 2, -10),
            Direction = new Vector2(0.15f, 1),
            Spread = 12,
            Gravity = new Vector2(0, 6),
            InitialVelocityMin = 28,
            InitialVelocityMax = 60,
            ScaleAmountMin = 1.5f,
            ScaleAmountMax = 3.5f,
            Color = new Color(1, 1, 1, 0.85f),
        };
        layer.AddChild(flakes);
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
        view.Setup(unit.Id, unit.Name, cls.Name, unit.Faction, unit.IsBoss, unit.CurrentHp, State.Stats(unit).HP, SpriteLibrary.Map(unit), SpriteLibrary.MapScale(unit));
        view.Position = CellWorld(unit.Pos);
        view.FaceToward(view.Position + (unit.Faction == Faction.Player ? Vector3.Forward : Vector3.Back));
        _views[unit.Id] = view;
        RefreshRecruit(unit);
        return view;
    }

    /// <summary>Marks an enemy that a talk would recruit (yellow-green, "Talk: Kai"), or clears the mark.</summary>
    public void RefreshRecruit(BattleUnit unit)
    {
        if (ViewOf(unit.Id) is not { } view)
            return;
        string hint = EventDirector.Recruiter(State, unit) switch
        {
            null => "",
            "any" => "Talk to recruit",
            var actors => "Talk: " + string.Join(" or ", actors.Split('|').Select(k => State.ByKey(k)?.Name ?? k)),
        };
        view.SetRecruitable(hint);
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

    /// <summary>The fog of war style in use (FogLooks.Styles; --fogstyle overrides).</summary>
    public const int DefaultFogStyle = 2; // "Eerie mist", the user's pick (session 12)

    private readonly List<FogPlane> _fogLayers = new();

    /// <summary>Fog styles past 0 draw drifting fog in layers over the hidden cells, instead of the flat squares.</summary>
    private void BuildFogLayers()
    {
        int style = Math.Clamp(DevTools.FogStyle ?? DefaultFogStyle, 0, FogLooks.Styles.Count - 1);
        foreach (var layer in FogLooks.Styles[style].Layers)
        {
            var plane = new FogPlane { Name = "FogLayer" };
            FogOverlay.GetParent().AddChild(plane);
            plane.Setup(State.Grid, layer);
            _fogLayers.Add(plane);
        }
    }

    /// <summary>Re-applies fog (hidden cells and enemy views) and the objective/visit markers.</summary>
    public void RefreshMapState()
    {
        UpdateCrestSprites(rebuild: true);
        if (State.Map.Fog)
        {
            var visible = State.VisibleCells(Faction.Player);
            var hidden = State.Grid.AllPositions().Where(p => !visible.Contains(p)).ToList();
            if (_fogLayers.Count == 0)
            {
                FogOverlay.ShowCells(hidden.Select(p => (p, CellOverlay.FogColor)).ToList());
            }
            else
            {
                foreach (var layer in _fogLayers)
                    layer.ShowCells(hidden);
            }
        }
        foreach (var unit in State.Living)
        {
            ViewOf(unit.Id)?.SetHidden(!State.CanSee(Faction.Player, unit));
            ViewOf(unit.Id)?.SetDominated(State.IsDominated(unit));
        }

        var markers = new List<(GridPos, Color)>();
        var goals = State.Map.Goals;
        if (goals.Victory is VictoryKind.Seize or VictoryKind.Escape)
            markers.AddRange(goals.Cells.Select(c => (c, CellOverlay.ObjectiveColor)));
        foreach (var trigger in State.Map.AllTriggers.Where(t => !State.FiredTriggers.Contains(t.Id)))
            if (trigger.Condition is VisitCondition v)
                markers.Add((v.Cell, CellOverlay.VisitColor));
            else if (trigger.Condition is ChestCondition c && State.Grid[c.Cell].Terrain.Id == ActionRules.ChestTerrain)
                markers.Add((c.Cell, CellOverlay.VisitColor));
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
                await ToSignal(GetTree().CreateTimer(0.3, processAlways: false), SceneTreeTimer.SignalName.Timeout);
            }
            IBattleCommand command = decision?.Command ?? new EndPhaseCommand();
            // Never stall the loop on a bad AI command: that unit waits (ending the phase outright left the rest of an
            // autoplay army idle every turn, session 17), and only if even that fails does the phase end.
            if (!await Execute(command)
                && (decision is null || !await Execute(new ActCommand(decision.Unit.Id, decision.Unit.Pos, new WaitAction()))))
                await Execute(new EndPhaseCommand());
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
                    && (!unit.IsLord || (State.Map.Goals.EscapeAll
                        ? State.LivingOf(Faction.Player).All(o => o == unit)
                        : State.Map.Goals.EscapeKeys.All(k => State.ByKey(k) is not { IsOnMap: true } other || other == unit))))
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
            var care = CarefulAi.Heal(State, unit, reach, safeOnly: IsVital(unit) || MustSurvive(unit))
                       ?? CarefulAi.Recover(State, unit, reach, IsVital(unit) || MustSurvive(unit) ? 0.5 : 0.35,
                           needsItem: !IsVital(unit) || LastFighter(unit) && !State.LivingOf(Faction.Player).Any(f => ActionRules.ReadySpells(State, f).Any()));
            if (care is not null && care.Validate(State).Ok)
                return new AiDecision(unit, care, null);
        }
        if (SeekRecruit() is { } seek)
            return seek;
        // Everyone else fights like the enemy AI; vital units (the lord, and anyone who must escape) are far more
        // careful. Units that only must survive still fight, but pull back sooner (above).
        var decisions = State.LivingOf(Faction.Player).Where(u => u.ActionState != ActionState.Done)
            .Select(u => IsVital(u) ? LordDecision(u)
                : !TimeToLeave(u) && CarefulAi.Shelter(State, u, State.Reach(u)) is { } shelter ? new AiDecision(u, shelter, null)
                : State.Map.Goals.EscapeAll ? FightOrLeave(u)
                : AiController.Decide(State, u, AutoplayWeights)).ToList();
        return decisions.Where(d => d.AttackScore is not null).OrderByDescending(d => d.AttackScore).FirstOrDefault()
               ?? decisions.FirstOrDefault(d => !IsVital(d.Unit)) ?? decisions.FirstOrDefault();
    }

    /// <summary>
    /// Escape-everyone maps (Ch 12): fight, or go after the enemies left, and head for the exit once none are. (Only the
    /// lord skirts danger on the way out: a stationary caster covering the gangplank stalled everyone else for 45 turns.)
    /// </summary>
    /// <summary>
    /// Escape-everyone maps: in a hurry when there's a turn limit and an exit is open (Ch 15's endless Hollowed and an
    /// unbeatable Morvane), else once no foe that can be hurt is left.
    /// </summary>
    private bool Hurry(BattleUnit unit) =>
        State.Map.Goals.EscapeAll && State.Map.Goals.TurnLimit > 0
        && State.Map.Goals.Cells.Any(c => State.Grid[c].Terrain.CostFor(State.MoveType(unit)) > 0);

    private bool TimeToLeave(BattleUnit unit) =>
        State.Map.Goals.EscapeAll && (Hurry(unit) || !State.LivingOf(Faction.Enemy).Any(e => !e.Invulnerable));

    private AiDecision FightOrLeave(BattleUnit unit)
    {
        var d = AiController.Decide(State, unit, AutoplayWeights);
        var goals = State.Map.Goals;
        bool hurry = Hurry(unit);
        if (!TimeToLeave(unit))
            return d;
        if (!hurry)
            return LordDecision(unit);
        // In a hurry, take the shortest way to the exit, danger or not (the lord's careful walk stalled in a doorway full
        // of Hollowed); escaping itself is offered first, by AutoplayDecision, whenever an exit is in reach.
        var reach = State.Reach(unit);
        var exit = goals.Cells.OrderBy(c => c.DistanceTo(unit.Pos)).First();
        var path = Pathfinder.FindPath(State.Grid, unit.Pos, exit, State.MoveType(unit));
        if (path is not null)
            for (int i = path.Count - 1; i > 0; i--)
                if (reach.CanStop(path[i]))
                    return new AiDecision(unit, new ActCommand(unit.Id, path[i], new WaitAction()), null);
        return d;
    }

    /// <summary>
    /// Recruitable enemies can't be attacked (<see cref="ActionRules.SparedRecruit"/>), so once they're the only foes
    /// left, walk a unit who can talk to one toward it; the talk itself is offered above once it's in reach.
    /// (Session 16: Ch 11's Ysra, stationary on her shelf, stalled the soak at turn 61.)
    /// </summary>
    private AiDecision? SeekRecruit()
    {
        var foes = State.LivingOf(Faction.Enemy).Where(e => e.IsOnMap && !e.Invulnerable).ToList();
        var recruits = foes.Where(e => EventDirector.Recruiter(State, e) is not null).ToList();
        if (recruits.Count == 0 || recruits.Count < foes.Count)
            return null;
        foreach (var recruit in recruits)
        {
            var actors = EventDirector.Recruiter(State, recruit)!;
            var talkers = State.LivingOf(Faction.Player)
                .Where(u => u.ActionState == ActionState.Ready
                            && (actors == "any" || actors.Split('|').Any(k => State.ByKey(k) == u)))
                .OrderBy(u => u.Pos.DistanceTo(recruit.Pos));
            foreach (var unit in talkers)
            {
                var reach = State.Reach(unit);
                foreach (var beside in recruit.Pos.Neighbors().Where(State.Grid.InBounds).OrderBy(c => c.DistanceTo(unit.Pos)))
                {
                    var path = Pathfinder.FindPath(State.Grid, unit.Pos, beside, State.MoveType(unit));
                    if (path is null)
                        continue;
                    for (int i = path.Count - 1; i > 0; i--)
                        if (reach.CanStop(path[i]))
                            return new AiDecision(unit, new ActCommand(unit.Id, path[i], new WaitAction()), null);
                }
            }
        }
        return null;
    }

    /// <summary>The lord, and units the map's escape objective names.</summary>
    private bool IsVital(BattleUnit unit) =>
        unit.IsLord || State.Map.Goals.EscapeKeys.Any(key => State.ByKey(key) == unit);

    /// <summary>
    /// On a Rout/Boss/Seize map, a vital unit with nobody else left to fight: it has to press on. (Pulling back to wait
    /// for help that won't come stalls the map: session 17, a hurt Rowan with no vulnerary stepped toward Ch 2's throne
    /// and back for 40 turns.)
    /// </summary>
    private bool LastFighter(BattleUnit unit) =>
        IsVital(unit) && State.Map.Goals.Victory is VictoryKind.Rout or VictoryKind.DefeatBoss or VictoryKind.Seize
        && !State.LivingOf(Faction.Player).Any(u => u != unit && !IsVital(u) && State.Db.TryWeapon(u.State.EquippedItemId) is not null);

    /// <summary>Units whose death loses the map ("die KEY").</summary>
    private bool MustSurvive(BattleUnit unit) => State.Map.Goals.MustSurvive.Any(key => State.ByKey(key) == unit);

    private AiDecision LordDecision(BattleUnit lord)
    {
        var goals = State.Map.Goals;
        bool escaping = goals.Victory == VictoryKind.Escape && (goals.EscapeAll || goals.EscapeKeys.Any(k => State.ByKey(k) == lord || lord.IsLord));
        var d = AiController.Decide(State, lord, LordWeights);
        // On an escape map the units who must leave just run; elsewhere they fight when it's safe.
        if (d.AttackScore is > 0 && !escaping)
            return d;
        // Seize/Escape maps: walk toward the objective (seize once the way is fairly clear; escape at once).
        if (escaping || goals.Victory == VictoryKind.Seize && State.LivingOf(Faction.Enemy).Count() <= 3)
        {
            var reach = State.Reach(lord);
            var goal = goals.Cells.OrderBy(c => c.DistanceTo(lord.Pos)).First();
            if (escaping)
            {
                // Toward the exit, but not through an ambush: each enemy that could reach a cell costs a few steps.
                var threats = AiController.ThreatCounts(State, lord.Faction);
                int? StepsLeft(GridPos c) => Pathfinder.FindPath(State.Grid, c, goal, State.MoveType(lord))?.Count;
                int now = StepsLeft(lord.Pos) ?? int.MaxValue;
                // Only cells that get him closer (or hold), so dodging danger never turns into circling the map. A lord
                // who must leave last waits beside the exit, not on it (Ch 15: he blocked one of the door's cells).
                bool waitForOthers = lord.IsLord && goals.EscapeAll && State.LivingOf(Faction.Player).Any(o => o != lord);
                var best = reach.StopCells
                    .Select(c => (Cell: c, Steps: StepsLeft(c)))
                    .Where(x => x.Steps is not null && (x.Steps <= now || waitForOthers && goals.Cells.Contains(lord.Pos)))
                    .Where(x => !waitForOthers || !goals.Cells.Contains(x.Cell))
                    .OrderBy(x => x.Steps + EscapeDangerSteps * threats.GetValueOrDefault(x.Cell))
                    .ThenBy(x => x.Cell.Y).ThenBy(x => x.Cell.X)
                    .Select(x => x.Cell).DefaultIfEmpty(lord.Pos).First();
                return new AiDecision(lord, new ActCommand(lord.Id, best, new WaitAction()), null);
            }
            var path = Pathfinder.FindPath(State.Grid, lord.Pos, goal, State.MoveType(lord));
            if (path is not null)
                for (int i = path.Count - 1; i > 0; i--)
                    if (reach.CanStop(path[i]))
                        return new AiDecision(lord, new ActCommand(lord.Id, path[i], new WaitAction()), null);
        }
        // Rout/Boss/Seize maps: once nobody else can fight, the lord must press on, or a boss who holds his post and a
        // lord who waits for a safe attack stare at each other until turn 60 (session 12: Ch 7's lord, alone, waited
        // for the throne room to empty while Halvric sat on the seize tile).
        if (LastFighter(lord))
        {
            GridPos? dest;
            bool onto = false; // walk onto the cell itself (an empty seize tile), not just up to it
            if (goals.Victory == VictoryKind.Seize)
            {
                var cell = goals.Cells.OrderBy(c => c.DistanceTo(lord.Pos)).First();
                onto = State.UnitAt(cell) is null;
                dest = cell;
            }
            else
            {
                dest = ((goals.Victory == VictoryKind.DefeatBoss && goals.BossKey is { } key
                            ? State.AllByKey(key).Where(u => u.IsOnMap).OrderBy(u => u.Pos.DistanceTo(lord.Pos)).FirstOrDefault() : null)
                        ?? State.LivingOf(Faction.Enemy).OrderBy(e => e.Pos.DistanceTo(lord.Pos)).FirstOrDefault())?.Pos;
            }
            var path = dest is { } to ? Pathfinder.FindPath(State.Grid, lord.Pos, to, State.MoveType(lord)) : null;
            if (path is not null)
            {
                // The furthest step along the way that at most one enemy can reach next phase.
                var reach = State.Reach(lord);
                var threats = AiController.ThreatCounts(State, lord.Faction);
                for (int i = path.Count - (onto ? 1 : 2); i > 0; i--)
                    if (reach.CanStop(path[i]) && threats.GetValueOrDefault(path[i]) <= 1)
                        return new AiDecision(lord, new ActCommand(lord.Id, path[i], new WaitAction()), null);
            }
        }
        return new AiDecision(lord, new ActCommand(lord.Id, lord.Pos, new WaitAction()), null);
    }

    /// <summary>Autoplay defeats in a row on the current battle (kept across retries, which reload the scene).</summary>
    private static int _autoplayLosses;
    private const int AutoplayMaxLosses = 10;

    private static readonly AiWeights AutoplayWeights = new(CounterDamage: 1.2, SelfDeath: 200.0, Danger: 3.0);
    /// <summary>How many steps out of the way an escaping lord will go to avoid each enemy that could reach him.</summary>
    private const int EscapeDangerSteps = 10;
    private static readonly AiWeights LordWeights = new(CounterDamage: 2.0, SelfDeath: 500.0, Danger: 12.0);

    /// <summary>Starts this battle over from turn 1 with the same deployment (from the result screen or the settings).</summary>
    public void RetryBattle()
    {
        if (IsCampaign)
        {
            SaveService.ClearSuspend();
            var node = GameSessionNode.Instance;
            var runner = node.Runner!;
            // A new seed per attempt, so a retry isn't a replay of the same rolls.
            runner.Session.BattleCount++;
            node.PendingBattle = runner.Launch(node.PendingBattleStep!, node.PendingBattle!.Deployed);
        }
        GetTree().ReloadCurrentScene();
    }

    private async Task EndBattle()
    {
        var node = GameSessionNode.Instance;
        if (IsCampaign)
            SaveService.ClearSuspend();
        AudioDirector.PlayMusic(State.Outcome == BattleOutcome.Victory ? "victory" : "defeat", loop: false);
        if (IsCampaign && State.Outcome == BattleOutcome.Victory)
        {
            _autoplayLosses = 0;
            await Hud.ShowBanner("Victory!", UiKit.Accent, 1.2f);
            CampaignFlow.BattleWon(State);
            return;
        }
        var buttons = new List<(string, Action)> { (IsCampaign ? "Retry Battle" : "Play Again", RetryBattle) };
        // A lost side map costs nothing: the army can go back to the hub and carry on with the story.
        if (IsCampaign && node.PendingSide is not null)
            buttons.Add(("Back to Camp", CampaignFlow.LeaveSideMap));
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
            // A single map tested on its own (--map=... --autoplay) ends there, so runs can be batched.
            if (!IsCampaign && !DevTools.AutoAdvance)
            {
                GetTree().Quit();
                return;
            }
            // The autoplay is deterministic enough that a map it can't win is lost the same way on every retry.
            if (++_autoplayLosses >= AutoplayMaxLosses)
            {
                GD.PrintErr($"AUTOPLAY STUCK on '{State.Map.Id}': lost {_autoplayLosses} times in a row");
                GetTree().Quit();
                return;
            }
            if (DevTools.AutoAdvance)
                GetTree().CreateTimer(0.5).Timeout += buttons[0].Item2;
        }
    }

    private string Describe(BattleEvent e) => e switch
    {
        UnitMoved m => $"{NameOf(m.UnitId)} moves {m.Path[0]} -> {m.Path[^1]}",
        CombatResolved c => $"{NameOf(c.AttackerId)} attacks {NameOf(c.DefenderId)} ({c.Forecast.Attacker.WeaponName}): " + string.Join(" ", c.Result.Strikes.Select(x =>
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
