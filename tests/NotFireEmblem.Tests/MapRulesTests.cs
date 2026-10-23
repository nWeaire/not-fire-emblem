using NotFireEmblem.AI;
using NotFireEmblem.Battle;
using NotFireEmblem.Combat;
using NotFireEmblem.Dialogue;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Tests;

/// <summary>M4: objectives, triggers, talk/visit/seize/escape, fog, Dominion and Severance, and the script parsers.</summary>
public class MapRulesTests
{
    private static (BattleState s, TurnManager tm) Start(TestBattle builder)
    {
        var s = builder.Build();
        var tm = new TurnManager(s);
        tm.StartBattle();
        return (s, tm);
    }

    private static BattleUnit ByKey(BattleState s, string key) => s.ByKey(key)!;

    private static void EndPhase(TurnManager tm) => Assert.True(tm.Submit(new EndPhaseCommand()).Ok);

    // ------------------------------------------------------------------ parsers

    [Fact]
    public void MapScript_ParsesHighlights()
    {
        var h = MapScript.ParseHighlights(new[] { "19,10-19,11 => The gate.", "hollin => Defeat him to end it early." });
        Assert.Equal(new[] { new GridPos(19, 10), new GridPos(19, 11) }, h[0].Cells);
        Assert.Null(h[0].Key);
        Assert.Equal("The gate.", h[0].Caption);
        Assert.Equal("hollin", h[1].Key);
        Assert.Throws<FormatException>(() => MapScript.ParseHighlights(new[] { "1,1 =>" }));
    }

    [Fact]
    public void ObjectiveTour_ListsGoalsUnitsAndHighlights_AndCountsDownSurvive()
    {
        var s = new TestBattle("..........")
            .Lord(0, 0, "sword")
            .Named("dorran", Faction.Player, "merc", 1, 0, "sword")
            .Unit(Faction.Enemy, "merc", 8, 0, "sword").With(sp => sp with { Tag = "hollin" })
            .Victory("survive 12").Defeat("die dorran")
            .Highlights("4,0-5,0 => The gate.", "hollin => The Binder.")
            .Build();
        var stops = ObjectiveTour.Stops(s);
        Assert.Equal(new[] { "Victory: survive 12 turns.", "Defeat if dorran falls.", "The gate.", "The Binder." },
            stops.Select(x => x.Caption));
        Assert.Empty(stops[0].Cells);
        Assert.Equal(new[] { new GridPos(1, 0) }, stops[1].Cells);
        Assert.Equal(new[] { new GridPos(4, 0), new GridPos(5, 0) }, stops[2].Cells);
        Assert.Equal(new[] { new GridPos(8, 0) }, stops[3].Cells);

        Assert.Equal("12 turns to survive", ObjectiveTour.Countdown(s));
        s.Turn = 12;
        Assert.Equal("Last turn!", ObjectiveTour.Countdown(s));
    }

    [Fact]
    public void MapScript_ParsesObjectivesAndTriggers()
    {
        var o = MapScript.ParseObjectives("escape 7,15-8,15 elise", new[] { "die aldric", "protect villager 3", "turns 20" });
        Assert.Equal(VictoryKind.Escape, o.Victory);
        Assert.Equal(new[] { new GridPos(7, 15), new GridPos(8, 15) }, o.Cells);
        Assert.Equal(new[] { "elise" }, o.EscapeKeys);
        Assert.Equal(new[] { "aldric" }, o.MustSurvive);
        Assert.Equal(new ProtectRule("villager", 3), o.Protect.Single());
        Assert.Equal(20, o.TurnLimit);

        var t = MapScript.ParseTriggers(new[] { "turn 4 enemy => spawn north; scene ch01_reinforcements; terrain 1,1-2,1 P", "talk any brann => join brann" });
        Assert.Equal(new TurnStartCondition(4, Faction.Enemy), t[0].Condition);
        Assert.Equal(3, t[0].Effects.Count);
        Assert.Equal(new[] { new GridPos(1, 1), new GridPos(2, 1) }, ((TerrainEffect)t[0].Effects[2]).Cells);
        Assert.Equal('P', ((TerrainEffect)t[0].Effects[2]).Symbol);
        Assert.Equal(new TalkCondition("any", "brann"), t[1].Condition);
        Assert.Throws<FormatException>(() => MapScript.ParseTriggers(new[] { "sometimes => win" }));
    }

    [Fact]
    public void DialogueParser_ReadsTheScriptFormat()
    {
        var scenes = DialogueParser.Parse("""
            # intro text is ignored
            ### pro_opening
            @bg farm_evening          ; background image
            ROWAN [smile]: You should be impressed; really.
            NARRATOR: The dogs had been quiet.
            ???: There you are.
            @wait 0.6
            @focus 7,4
            @flag aldric_revealed

            ### pro_binder_battle (boss battle quote)
            BINDER [cold]: The quiet is coming.
            """);
        Assert.Equal(new[] { "pro_opening", "pro_binder_battle" }, scenes.Select(s => s.Id));
        var lines = scenes[0].Lines;
        Assert.Equal(new BackgroundLine("farm_evening"), lines[0]);
        Assert.Equal(new SpeakLine("rowan", "smile", "You should be impressed; really."), lines[1]);
        Assert.True(((SpeakLine)lines[2]).IsNarrator);
        Assert.Equal("???", ((SpeakLine)lines[3]).Speaker);
        Assert.Equal(new WaitLine(0.6), lines[4]);
        Assert.Equal(new FocusLine(new GridPos(7, 4)), lines[5]);
        Assert.Equal(new[] { "aldric_revealed" }, scenes[0].Flags);
        Assert.Equal(new[] { "rowan" }, scenes[0].Speakers);
    }

    [Fact]
    public void Dialogue_OnlySpeakers_DropsAbsentCharactersButKeepsTheNarrator()
    {
        var scene = DialogueParser.Parse("""
            ### crest_blessing_2
            @sfx crest_rise
            NARRATOR: The crest climbed higher.
            KAI [laugh]: I could lift a horse.
            MIRA [smile]: It's stronger.
            ROWAN: Right.
            """)[0];
        var only = scene.OnlySpeakers(new HashSet<string> { "rowan", "mira" });
        Assert.Equal(new[] { "mira", "rowan" }, only.Speakers);
        Assert.IsType<SoundLine>(only.Lines[0]);
        Assert.True(((SpeakLine)only.Lines[1]).IsNarrator);
        Assert.Equal(4, only.Lines.Count);
    }

    // ------------------------------------------------------------------ objectives

    [Fact]
    public void DefeatBoss_WinsWhenTheBossFalls_EvenWithEnemiesLeft()
    {
        var (s, tm) = Start(new TestBattle(".....").Lord(0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 1, 0, "sword").With(sp => sp with { Tag = "boss" })
            .Unit(Faction.Enemy, "merc", 4, 0, "sword")
            .Victory("boss boss"));
        ByKey(s, "boss").CurrentHp = 5;
        var r = tm.Submit(new ActCommand("u0", new GridPos(0, 0), new AttackAction(ByKey(s, "boss").Id, new WeaponAttack("sword"))));
        Assert.True(r.Ok, r.Error);
        Assert.Equal(BattleOutcome.Victory, s.Outcome);
    }

    [Fact]
    public void SurviveOrBoss_FellingTheBossWinsEarly()
    {
        var goals = MapScript.ParseObjectives("survive 10 or boss commander", null);
        Assert.Equal((VictoryKind.Survive, 10, "commander"), (goals.Victory, goals.Turns, goals.BossKey));
        Assert.Throws<FormatException>(() => MapScript.ParseObjectives("survive 10 or rout", null));

        var (s, tm) = Start(new TestBattle(".....").Lord(0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 1, 0, "sword").With(sp => sp with { Tag = "commander" })
            .Unit(Faction.Enemy, "merc", 4, 0, "sword")
            .Victory("survive 10 or boss commander"));
        Assert.Contains("or defeat", ObjectiveEvaluator.Describe(s));
        ByKey(s, "commander").CurrentHp = 1;
        Assert.True(tm.Submit(new ActCommand("u0", new GridPos(0, 0), new AttackAction(ByKey(s, "commander").Id, new WeaponAttack("sword")))).Ok);
        Assert.Equal(BattleOutcome.Victory, s.Outcome);
    }

    [Fact]
    public void Talk_CanNameSeveralActors()
    {
        var (s, _) = Start(new TestBattle(".....").Lord(0, 0, "sword")
            .Named("seraphine", Faction.Player, "merc", 2, 0, "sword")
            .Named("kai", Faction.Player, "merc", 4, 0, "sword")
            .Named("ysra", Faction.Enemy, "merc", 3, 0, "sword")
            .Triggers("talk lord|seraphine ysra => join ysra"));
        Assert.Single(EventDirector.TalkTargets(s, s.Unit("u1"), new GridPos(2, 0)));
        Assert.Empty(EventDirector.TalkTargets(s, s.Unit("u2"), new GridPos(4, 0)));
    }

    [Fact]
    public void EscapeAll_NeedsEveryLivingUnitOut()
    {
        var (s, tm) = Start(new TestBattle(".....", ".....").Lord(3, 0, "sword")
            .Unit(Faction.Player, "merc", 3, 1, "sword")
            .Unit(Faction.Enemy, "merc", 0, 0, "sword").WithAi(AiProfile.Stationary)
            .Victory("escape 4,0 4,1 all"));
        Assert.True(s.Map.Goals.EscapeAll);
        Assert.True(tm.Submit(new ActCommand("u0", new GridPos(4, 0), new EscapeAction())).Ok);
        Assert.Equal(BattleOutcome.InProgress, s.Outcome);
        Assert.True(tm.Submit(new ActCommand("u1", new GridPos(4, 1), new EscapeAction())).Ok);
        Assert.Equal(BattleOutcome.Victory, s.Outcome);
    }

    [Fact]
    public void EscapeAll_RetreatedUnitsNeedNotEscape()
    {
        var (s, tm) = Start(new TestBattle(".....", ".....").Lord(3, 0, "sword")
            .Unit(Faction.Player, "merc", 3, 1, "sword")
            .Unit(Faction.Enemy, "merc", 0, 0, "sword").WithAi(AiProfile.Stationary)
            .Victory("escape 4,0 4,1 all"));
        s.RemoveFromMap(s.Unit("u1"), MapExit.Retreated);
        Assert.True(tm.Submit(new ActCommand("u0", new GridPos(4, 0), new EscapeAction())).Ok);
        Assert.Equal(BattleOutcome.Victory, s.Outcome);
    }

    [Fact]
    public void Seize_OnlyTheLordOnTheMarkedTile()
    {
        var (s, tm) = Start(new TestBattle(".....", ".....").Lord(0, 0, "sword")
            .Unit(Faction.Player, "merc", 0, 1, "sword")
            .Unit(Faction.Enemy, "merc", 4, 1, "sword")
            .Victory("seize 2,0"));
        Assert.False(tm.Submit(new ActCommand("u1", new GridPos(1, 1), new SeizeAction())).Ok);
        Assert.False(tm.Submit(new ActCommand("u0", new GridPos(1, 0), new SeizeAction())).Ok);
        Assert.True(tm.Submit(new ActCommand("u0", new GridPos(2, 0), new SeizeAction())).Ok);
        Assert.Equal(BattleOutcome.Victory, s.Outcome);
    }

    [Fact]
    public void Survive_WinsWhenThePlayerPhaseAfterTheLastTurnBegins()
    {
        var (s, tm) = Start(new TestBattle("........").Lord(0, 0, "sword").Unit(Faction.Enemy, "merc", 7, 0).Victory("survive 2"));
        EndPhase(tm); // T1 player
        EndPhase(tm); // T1 enemy
        Assert.Equal(BattleOutcome.InProgress, s.Outcome);
        EndPhase(tm); // T2 player
        EndPhase(tm); // T2 enemy -> T3 player begins
        Assert.Equal(BattleOutcome.Victory, s.Outcome);
    }

    [Fact]
    public void Escape_WinsWhenTheLordLeaves()
    {
        var (s, tm) = Start(new TestBattle(".....").Lord(1, 0, "sword").Unit(Faction.Enemy, "merc", 4, 0).Victory("escape 3,0"));
        Assert.True(tm.Submit(new ActCommand("u0", new GridPos(3, 0), new EscapeAction())).Ok);
        Assert.Equal(MapExit.Escaped, s.Unit("u0").Exit);
        Assert.Equal(BattleOutcome.Victory, s.Outcome);
    }

    [Fact]
    public void Protect_LosesWhenTooManyTaggedUnitsFall()
    {
        var (s, tm) = Start(new TestBattle(".....").Lord(4, 0, "sword")
            .Unit(Faction.Ally, "merc", 1, 0).With(sp => sp with { Tag = "villager" })
            .Unit(Faction.Enemy, "merc", 2, 0, "sword")
            .Defeat("protect villager 1"));
        ByKey(s, "villager").CurrentHp = 1;
        EndPhase(tm);
        var r = tm.Submit(new ActCommand("u2", new GridPos(2, 0), new AttackAction("u1", new WeaponAttack("sword"))));
        Assert.True(r.Ok, r.Error);
        Assert.Equal(BattleOutcome.Defeat, s.Outcome);
    }

    [Fact]
    public void MustSurvive_LosesWhenThatUnitDies()
    {
        var (s, tm) = Start(new TestBattle(".....").Lord(4, 0, "sword")
            .Named("aldric", Faction.Player, "merc", 1, 0)
            .Unit(Faction.Enemy, "merc", 2, 0, "sword")
            .Defeat("die aldric"));
        ByKey(s, "aldric").CurrentHp = 1;
        EndPhase(tm);
        tm.Submit(new ActCommand("u2", new GridPos(2, 0), new AttackAction("u1", new WeaponAttack("sword"))));
        Assert.Equal(BattleOutcome.Defeat, s.Outcome);
    }

    // ------------------------------------------------------------------ triggers

    [Fact]
    public void LeaveTrigger_TakesEveryUnitWithTheKeyOffTheMap_WithoutCountingAsDefeated()
    {
        var (s, tm) = Start(new TestBattle(".....", ".....").Lord(0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 4, 1, "sword")
            .Unit(Faction.Enemy, "flier", 3, 0, "lance").With(sp => sp with { Group = "scouts", Tag = "scouts" })
            .Unit(Faction.Enemy, "flier", 4, 0, "lance").With(sp => sp with { Group = "scouts", Tag = "scouts" })
            .Triggers("turn 2 => spawn scouts; leave scouts", "defeated scouts => flag shot_down"));
        EndPhase(tm);
        var r = tm.Submit(new EndPhaseCommand());
        Assert.Equal(2, r.Events.OfType<UnitDeparted>().Count());
        Assert.All(s.Units.Where(u => u.Key == "scouts"), u => Assert.Equal(MapExit.Departed, u.Exit));
        Assert.DoesNotContain("shot_down", s.Flags);
        Assert.Equal(BattleOutcome.InProgress, ObjectiveEvaluator.Evaluate(s));
    }

    [Fact]
    public void TurnTrigger_SpawnsReinforcementsOnTheNearestFreeCell()
    {
        var (s, tm) = Start(new TestBattle(".....", ".....").Lord(0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 4, 1, "sword")
            .Unit(Faction.Enemy, "merc", 0, 0, "sword").With(sp => sp with { Group = "reinf" })
            .Triggers("turn 2 => spawn reinf; flag arrived"));
        Assert.Equal(2, s.Units.Count);
        EndPhase(tm);
        var r = tm.Submit(new EndPhaseCommand());
        Assert.Contains(r.Events, e => e is UnitSpawned);
        Assert.Contains("arrived", s.Flags);
        var spawned = s.Units[^1];
        Assert.NotEqual(new GridPos(0, 0), spawned.Pos);
        Assert.Equal(1, spawned.Pos.DistanceTo(new GridPos(0, 0)));
    }

    [Fact]
    public void UnlessGuard_HoldsTheTriggerBackOnceTheUnitHasJoined()
    {
        var parsed = MapScript.ParseTriggers(new[] { "turn 2 enemy unless joined kai unless flag done => flag pressed" });
        Assert.Equal(new[] { new TriggerGuard("joined", "kai"), new TriggerGuard("flag", "done") }, parsed[0].Unless);
        Assert.Throws<FormatException>(() => MapScript.ParseTriggers(new[] { "turn 2 unless maybe => win" }));

        var (s, tm) = Start(new TestBattle(".....").Lord(0, 0, "sword")
            .Named("kai", Faction.Ally, "soldier", 1, 0, "lance")
            .Unit(Faction.Enemy, "merc", 4, 0)
            .Triggers("talk lord kai => join kai", "turn 1 enemy unless joined kai => flag pressed"));
        Assert.True(tm.Submit(new ActCommand("u0", new GridPos(0, 0), new TalkAction(ByKey(s, "kai").Id))).Ok);
        EndPhase(tm);
        Assert.DoesNotContain("pressed", s.Flags);
    }

    [Fact]
    public void Talk_RecruitsAndPlaysTheScene()
    {
        var (s, tm) = Start(new TestBattle(".....").Lord(0, 0, "sword")
            .Named("kai", Faction.Ally, "soldier", 1, 0, "lance")
            .Unit(Faction.Enemy, "merc", 4, 0)
            .Triggers("talk lord kai => scene scene_a; join kai"));
        var lord = s.Unit("u0");
        var kai = ByKey(s, "kai");
        Assert.Single(EventDirector.TalkTargets(s, lord, lord.Pos));
        var r = tm.Submit(new ActCommand("u0", lord.Pos, new TalkAction(kai.Id)));
        Assert.True(r.Ok, r.Error);
        Assert.Equal(Faction.Player, kai.Faction);
        Assert.Contains(r.Events, e => e is DialogueRequested { SceneId: "scene_a" });
        Assert.Contains("met", s.Flags);
        Assert.Empty(EventDirector.TalkTargets(s, lord, lord.Pos));
    }

    [Fact]
    public void Visit_GivesItemsAndGoldOnce()
    {
        var (s, tm) = Start(new TestBattle(".....", ".....").Lord(0, 0, "sword")
            .Unit(Faction.Player, "merc", 0, 1, "sword")
            .Unit(Faction.Enemy, "merc", 4, 1)
            .Triggers("visit 2,0 => give actor vulnerary; gold 100"));
        Assert.True(tm.Submit(new ActCommand("u0", new GridPos(2, 0), new VisitAction())).Ok);
        Assert.Contains("vulnerary", s.Unit("u0").State.Inventory);
        Assert.Equal(100, s.GoldGained);
        Assert.False(ActionRules.CanVisit(s, s.Unit("u1"), new GridPos(2, 0)));
    }

    [Fact]
    public void DefeatedTrigger_CanChangeTerrain_AndBossQuotesPlayBeforeTheFight()
    {
        var (s, tm) = Start(new TestBattle(".....", ".....").Lord(0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 1, 0, "sword").With(sp => sp with { Tag = "boss" })
            .Unit(Faction.Enemy, "merc", 4, 1)
            .Triggers("combat boss => scene scene_a", "defeated boss => terrain 3,0-3,1 #"));
        ByKey(s, "boss").CurrentHp = 1;
        var r = tm.Submit(new ActCommand("u0", new GridPos(0, 0), new AttackAction(ByKey(s, "boss").Id, new WeaponAttack("sword"))));
        var order = r.Events.Select(e => e.GetType().Name).ToList();
        Assert.True(order.IndexOf(nameof(DialogueRequested)) < order.IndexOf(nameof(CombatResolved)));
        Assert.Contains(r.Events, e => e is TerrainChanged);
        Assert.Equal("wall", s.Grid[new GridPos(3, 0)].Terrain.Id);
    }

    // ------------------------------------------------------------------ fog

    [Fact]
    public void Fog_HidesDistantUnits_AndTheAiOnlyAttacksWhatItSees()
    {
        var (s, tm) = Start(new TestBattle("...........").Lord(0, 0, "sword").Unit(Faction.Enemy, "merc", 6, 0, "sword").Fog());
        Assert.False(s.CanSee(Faction.Player, s.Unit("u1")));
        Assert.Contains(new GridPos(4, 0), s.VisibleCells(Faction.Player));
        Assert.DoesNotContain(new GridPos(5, 0), s.VisibleCells(Faction.Player));
        Assert.Empty(BattleQueries.DangerZone(s, Faction.Player));
        EndPhase(tm);
        var decision = AiController.Decide(s, s.Unit("u1"));
        Assert.Null(decision.AttackScore); // could reach the lord, but can't see him
    }

    // ------------------------------------------------------------------ Dominion and Severance

    [Fact]
    public void Dominion_BuffsHeldUnits_AndSeveranceStunsBeastsAndRemovesHumans()
    {
        var (s, tm) = Start(new TestBattle("........", "........").Lord(4, 1, "sword")
            .Unit(Faction.Enemy, "merc", 5, 1, "sword").With(sp => sp with { Tag = "binder", CrestBearer = true })
            .Unit(Faction.Enemy, "beast", 3, 0, "claws").With(sp => sp with { DominatedBy = "binder" })
            .Unit(Faction.Enemy, "merc", 7, 1, "sword").With(sp => sp with { DominatedBy = "binder" }));
        var beast = s.Unit("u2");
        var human = s.Unit("u3");
        Assert.Equal(12, s.Stats(beast).Str);
        Assert.Equal(7, s.Stats(beast).Def);

        s.Unit("u1").CurrentHp = 1;
        var r = tm.Submit(new ActCommand("u0", new GridPos(4, 1), new AttackAction("u1", new WeaponAttack("sword"))));
        var severed = r.Events.OfType<CrestSevered>().Single();
        Assert.Equal(new[] { "u2" }, severed.Stunned);
        Assert.Equal(new[] { "u3" }, severed.Surrendered);
        Assert.Equal(MapExit.Surrendered, human.Exit);
        Assert.Equal(10, s.Stats(beast).Str);

        // The lord was the last to act, so the enemy phase ran at once: the stunned beast lost it,
        // and play passed straight back to the player.
        Assert.Contains(r.Events, e => e is UnitStunnedSkip { UnitId: "u2" });
        Assert.Equal(Faction.Player, s.Phase);
        Assert.Equal(2, s.Turn);
        Assert.Equal(0, beast.StunnedPhases);
    }

    // ------------------------------------------------------------------ neutrals (Ch 8's riot)

    [Fact]
    public void Neutrals_HaveTheirOwnPhase_AndOnlyPickOnAllies()
    {
        var (s, tm) = Start(new TestBattle("........", "........").Lord(2, 0, "sword")
            .Unit(Faction.Ally, "merc", 6, 0).With(sp => sp with { Tag = "civilian" })
            .Unit(Faction.Neutral, "merc", 3, 1, "sword")
            .Unit(Faction.Enemy, "merc", 7, 1, "sword").WithAi(AiProfile.Stationary));
        var rioter = s.Unit("u2");
        Assert.True(rioter.Faction.IsHostileTo(Faction.Player));
        Assert.False(rioter.Faction.IsHostileTo(Faction.Enemy));

        // The lord stands right beside the rioter, but the rioter goes for the civilian.
        var decision = AiController.Decide(s, rioter);
        var act = Assert.IsType<ActCommand>(decision.Command);
        Assert.Equal("u1", Assert.IsType<AttackAction>(act.Action).TargetId);

        // Player → Enemy → Neutral → Ally.
        Assert.True(tm.Submit(new EndPhaseCommand()).Ok);
        Assert.Equal(Faction.Enemy, s.Phase);
        Assert.True(tm.Submit(new EndPhaseCommand()).Ok);
        Assert.Equal(Faction.Neutral, s.Phase);
        Assert.True(tm.Submit(new EndPhaseCommand()).Ok);
        Assert.Equal(Faction.Ally, s.Phase);
    }

    [Fact]
    public void Ignores_KeepsTheAiOffThatTag()
    {
        var (s, _) = Start(new TestBattle("........").Lord(7, 0, "sword")
            .Unit(Faction.Ally, "merc", 1, 0).With(sp => sp with { Tag = "civilian" })
            .Unit(Faction.Enemy, "merc", 2, 0, "sword").With(sp => sp with { Ignores = "civilian" }));
        var thug = s.Unit("u2");
        Assert.False(AiController.Picks(thug, s.Unit("u1")));
        // It walks past the townsperson beside it to go for the lord.
        var act = Assert.IsType<ActCommand>(AiController.Decide(s, thug).Command);
        Assert.Equal("u0", Assert.IsType<AttackAction>(act.Action).TargetId);
    }

    [Fact]
    public void Agitators_ShareAKey_EachCalmsOnlyTheCrowdAroundHim()
    {
        var (s, tm) = Start(new TestBattle("..............", "..............").Lord(0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 1, 0, "sword").With(sp => sp with { Tag = "agitator" })
            .Unit(Faction.Neutral, "merc", 3, 1, "sword").With(sp => sp with { DominatedBy = "agitator" })
            .Unit(Faction.Enemy, "merc", 12, 0, "sword").With(sp => sp with { Tag = "agitator" })
            .Unit(Faction.Neutral, "merc", 11, 1, "sword").With(sp => sp with { DominatedBy = "agitator" })
            .Victory("boss agitator"));
        Assert.True(s.IsDominated(s.Unit("u2")));
        Assert.True(s.IsDominated(s.Unit("u4")));

        s.Unit("u1").CurrentHp = 1;
        var r = tm.Submit(new ActCommand("u0", new GridPos(0, 0), new AttackAction("u1", new WeaponAttack("sword"))));
        var calmed = r.Events.OfType<CrestSevered>().Single();
        Assert.Equal(new[] { "u2" }, calmed.Calmed);
        Assert.Empty(calmed.Surrendered);
        // The freed rioter stays, as one of the townsfolk: on the Ally side (green), unarmed.
        var townsfolk = s.Unit("u2");
        Assert.True(townsfolk.IsOnMap);
        Assert.Equal(Faction.Ally, townsfolk.Faction);
        Assert.Equal("Townsfolk", townsfolk.Name);
        Assert.Empty(townsfolk.State.Inventory);
        Assert.True(s.Unit("u4").IsOnMap);
        Assert.True(s.IsDominated(s.Unit("u4")));
        Assert.Equal(BattleOutcome.InProgress, s.Outcome);
        Assert.Contains("(1 left)", ObjectiveEvaluator.Describe(s));

        s.Defeat(s.Unit("u3"));
        Assert.Equal(BattleOutcome.Victory, ObjectiveEvaluator.Evaluate(s));
    }

    // ------------------------------------------------------------------ locks and thieves (Ch 9)

    [Fact]
    public void Doors_OpenWithLocktouchOrAKey_ThatIsUsedUp()
    {
        var (s, tm) = Start(new TestBattle("..D..", ".....").Lord(1, 0, "sword", "door_key")
            .Unit(Faction.Player, "thief", 1, 1, "sword")
            .Unit(Faction.Enemy, "merc", 4, 1, "sword").WithAi(AiProfile.Stationary));
        var door = new GridPos(2, 0);
        Assert.Equal(new[] { door }, ActionRules.Unlockable(s, s.Unit("u0"), new GridPos(1, 0)));
        Assert.Empty(ActionRules.Unlockable(s, s.Unit("u1"), new GridPos(1, 1))); // not beside it
        Assert.Equal(new[] { door }, ActionRules.Unlockable(s, s.Unit("u1"), new GridPos(2, 1)));

        var r = tm.Submit(new ActCommand("u0", new GridPos(1, 0), new UnlockAction(door)));
        Assert.True(r.Ok, r.Error);
        Assert.Equal("square", s.Grid[door].Terrain.Id);
        Assert.Equal(new[] { "sword" }, s.Unit("u0").State.Inventory);
        Assert.Equal("sword", s.Unit("u0").State.EquippedItemId);
        Assert.Contains(r.Events, e => e is LockOpened { Chest: false });
    }

    [Fact]
    public void Doors_BreakAfterThreeStrikes_FromAnyoneArmed()
    {
        var (s, tm) = Start(new TestBattle("..D..", ".....").Lord(1, 0, "sword")
            .Unit(Faction.Player, "merc", 3, 0, "sword")
            .Unit(Faction.Player, "merc", 2, 1, "sword")
            .Unit(Faction.Player, "merc", 0, 1)
            .Unit(Faction.Enemy, "merc", 4, 1, "sword").WithAi(AiProfile.Stationary));
        var door = new GridPos(2, 0);
        Assert.Empty(ActionRules.BreakableDoors(s, s.Unit("u3"), new GridPos(1, 0))); // unarmed
        Assert.Empty(ActionRules.BreakableDoors(s, s.Unit("u0"), new GridPos(0, 0))); // not beside it

        foreach (var (id, from, hits) in new[] { ("u0", new GridPos(1, 0), 1), ("u1", new GridPos(3, 0), 2) })
        {
            var r = tm.Submit(new ActCommand(id, from, new BreakDoorAction(door)));
            Assert.True(r.Ok, r.Error);
            Assert.Contains(r.Events, e => e is DoorStruck { Hits: var h, Needed: 3 } && h == hits);
            Assert.Equal("door", s.Grid[door].Terrain.Id);
        }
        var last = tm.Submit(new ActCommand("u2", new GridPos(2, 1), new BreakDoorAction(door)));
        Assert.True(last.Ok, last.Error);
        Assert.Equal("square", s.Grid[door].Terrain.Id);
        Assert.Contains(last.Events, e => e is TerrainChanged);
    }

    [Fact]
    public void Chests_GiveTheirContents_ToAnyoneInTheArmyWhoOpensThem()
    {
        var (s, tm) = Start(new TestBattle("C....").Lord(4, 0, "sword")
            .Unit(Faction.Player, "merc", 1, 0, "sword")
            .Unit(Faction.Enemy, "merc", 3, 0, "sword").WithAi(AiProfile.Stationary)
            .Triggers("chest 0,0 => give actor energy_drop; gold 500"));
        Assert.Empty(ActionRules.Unlockable(s, s.Unit("u0"), new GridPos(4, 0))); // not on it
        Assert.Empty(ActionRules.Unlockable(s, s.Unit("u2"), new GridPos(0, 0))); // enemies don't loot
        var r = tm.Submit(new ActCommand("u1", new GridPos(0, 0), new UnlockAction(new GridPos(0, 0))));
        Assert.True(r.Ok, r.Error);
        Assert.Contains("energy_drop", s.Unit("u1").State.Inventory);
        Assert.Equal(500, s.GoldGained);
        Assert.Equal("square", s.Grid[new GridPos(0, 0)].Terrain.Id);
    }

    [Fact]
    public void Steal_TakesWhatIsNotInHand_FromAnEnemyNoFaster()
    {
        var (s, tm) = Start(new TestBattle(".....").Lord(4, 0, "sword")
            .Unit(Faction.Player, "thief", 0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 1, 0, "sword", "vulnerary", "killer").WithAi(AiProfile.Stationary));
        var mark = s.Unit("u2");
        Assert.Equal(new[] { "vulnerary", "killer" }, ActionRules.Stealable(mark));
        Assert.Empty(ActionRules.StealTargets(s, s.Unit("u0"), new GridPos(3, 0))); // the lord isn't a thief

        var r = tm.Submit(new ActCommand("u1", new GridPos(0, 0), new StealAction("u2", "killer")));
        Assert.True(r.Ok, r.Error);
        Assert.Equal(new[] { "sword", "vulnerary" }, mark.State.Inventory);
        Assert.Equal("sword", mark.State.EquippedItemId);
        Assert.Contains("killer", s.Unit("u1").State.Inventory);
        Assert.False(tm.Submit(new ActCommand("u0", new GridPos(4, 0), new StealAction("u2", "sword"))).Ok);
    }

    // ------------------------------------------------------------------ non-lethal (Ch 14)

    [Fact]
    public void NonLethal_IsSubduedNotKilled_AndFreedWhenItsBearerFalls()
    {
        var (s, tm) = Start(new TestBattle("......", "......").Lord(0, 0, "sword")
            .Named("empress", Faction.Enemy, "merc", 1, 0, "sword").With(sp => sp with { DominatedBy = "vossk", NonLethal = true })
            .Unit(Faction.Enemy, "merc", 5, 1, "sword").With(sp => sp with { Tag = "vossk", CrestBearer = true })
            .Victory("boss vossk")
            .Triggers("subdued empress => flag beaten", "defeated empress => flag killed"));
        var empress = s.ByKey("empress")!;
        empress.CurrentHp = 1;
        var r = tm.Submit(new ActCommand("u0", new GridPos(0, 0), new AttackAction(empress.Id, new WeaponAttack("sword"))));
        Assert.True(r.Ok, r.Error);
        Assert.Contains(r.Events, e => e is UnitSubdued);
        Assert.False(empress.IsDefeated);
        Assert.Equal(MapExit.Subdued, empress.Exit);
        Assert.Contains("beaten", s.Flags);
        Assert.DoesNotContain("killed", s.Flags);
    }

    [Fact]
    public void Invulnerable_TakesNoDamage_AndTheForecastSaysSo()
    {
        var (s, tm) = Start(new TestBattle(".....").Lord(0, 0, "killer")
            .Unit(Faction.Enemy, "merc", 1, 0, "sword").With(sp => sp with { Tag = "morvane", Invulnerable = true })
            .Unit(Faction.Enemy, "merc", 4, 0, "sword").WithAi(AiProfile.Stationary));
        var morvane = s.ByKey("morvane")!;
        var forecast = CombatCalculator.Forecast(s, s.Unit("u0"), new GridPos(0, 0), morvane, new WeaponAttack("killer"));
        Assert.Equal((0, 0, true), (forecast.Attacker.Damage, forecast.Attacker.Crit, forecast.Attacker.Harmless));
        int hp = morvane.CurrentHp;
        Assert.True(tm.Submit(new ActCommand("u0", new GridPos(0, 0), new AttackAction(morvane.Id, new WeaponAttack("killer")))).Ok);
        Assert.Equal(hp, morvane.CurrentHp);
    }

    [Fact]
    public void StoryCriticalUnits_RetreatInsteadOfDying_UnlessTheyMustSurvive()
    {
        foreach (bool mustSurvive in new[] { false, true })
        {
            var b = new TestBattle(".....").Lord(4, 0, "sword")
                .Unit(Faction.Enemy, "merc", 1, 0, "killer").WithAi(AiProfile.Stationary);
            b.Db.Add(new UnitDef("father", "Father", "merc", 1, default, default, new[] { "sword" }, false, Retreats: true));
            b.With(sp => sp).Defeat(mustSurvive ? new[] { "die father" } : System.Array.Empty<string>());
            var map = b.Map() with { Spawns = b.Map().Spawns.Append(new SpawnDef("father", null, null, 1, System.Array.Empty<string>(), Faction.Player, new GridPos(0, 0))).ToList() };
            var s = BattleSetup.Create(b.Db, map, 1);
            var tm = new TurnManager(s);
            tm.StartBattle();
            var father = s.ByKey("father")!;
            father.CurrentHp = 1;
            Assert.True(tm.Submit(new ActCommand(father.Id, new GridPos(0, 0), new AttackAction("u1", new WeaponAttack("sword")))).Ok);
            if (mustSurvive)
            {
                Assert.True(father.IsDefeated);
                Assert.Equal(BattleOutcome.Defeat, s.Outcome);
            }
            else
            {
                Assert.False(father.IsDefeated);
                Assert.Equal(MapExit.Retreated, father.Exit);
            }
        }
    }
}
