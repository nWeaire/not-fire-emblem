using NotFireEmblem.AI;
using NotFireEmblem.Battle;
using NotFireEmblem.Campaign;
using NotFireEmblem.Combat;
using NotFireEmblem.Core;
using NotFireEmblem.Dialogue;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Tests;

/// <summary>M6: the campaign script, roster write-back, saves and suspends, and the hub's shop, smithy and promotion rules.</summary>
public class CampaignTests
{
    private static ContentDatabase Db()
    {
        var b = new TestBattle(".....", ".....")
            .Lord(0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 4, 1, "sword");
        var db = b.Db;
        db.Add(new UnitDef("partner", "Partner", "merc", 1, default, default, new[] { "sword" }, false,
            Supports: new[] { new SupportDef("lord", 2, 20, 30) }));
        db.Add(b.Map() with { DeploySlots = new[] { new GridPos(1, 0), new GridPos(0, 1) } });
        db.Add(new ShopDef("shop", "Shop", "thornvale", 0, new[] { "iron_sword", "vulnerary" }));
        db.Campaign = CampaignParser.Parse("""
            chapter 0 Prologue  # comments are ignored
            join lord
            gold 500
            scene scene_a
            battle test deploy 2
            join partner
            unlock smithy
            hub scene_a
            end The end.
            """);
        return db;
    }

    [Fact]
    public void CampaignParser_ReadsEverySteps()
    {
        var c = CampaignParser.Parse("battle ch2 deploy 5 force rowan,aldric\nclass rowan lord\ncrest 1\nitem vulnerary\nflag x\nleave aldric");
        Assert.Equal(("ch2", 5), (((BattleStep)c.Steps[0]).MapId, ((BattleStep)c.Steps[0]).Deploy));
        Assert.Equal(new[] { "rowan", "aldric" }, ((BattleStep)c.Steps[0]).Forced);
        Assert.Equal(new ClassStep("rowan", "lord"), c.Steps[1]);
        Assert.Equal(new CrestStep(1), c.Steps[2]);
        Assert.Throws<FormatException>(() => CampaignParser.Parse("dance"));
    }

    [Fact]
    public void SideMaps_AreOfferedFromTheirStep_UntilWonOrTheirChapter()
    {
        var db = Db();
        db.Campaign = CampaignParser.Parse("""
            chapter 1 One
            join lord
            side test deploy 2 force lord until 2 : An optional battle.
            hub
            chapter 2 Two
            hub
            end The end.
            """);
        var side = Assert.IsType<SideStep>(db.Campaign.Steps[2]);
        Assert.Equal(("test", 2, 2, "An optional battle."), (side.MapId, side.Deploy, side.Until, side.Blurb));
        Assert.Equal(new[] { "lord" }, side.Forced);

        var runner = new CampaignRunner(db, new GameSession());
        Assert.IsType<HubStep>(runner.NextInteractive());
        Assert.Equal(new[] { side }, runner.AvailableSideMaps());
        runner.Session.Flags.Add(CampaignRunner.SideWonFlag("test"));
        Assert.Empty(runner.AvailableSideMaps());
        runner.Session.Flags.Clear();
        runner.Session.Unit("lord")!.Status = UnitStatus.Dead; // needs the lord
        Assert.Empty(runner.AvailableSideMaps());
        runner.Session.Unit("lord")!.Status = UnitStatus.Active;
        runner.Complete();
        Assert.IsType<HubStep>(runner.NextInteractive());
        Assert.Empty(runner.AvailableSideMaps()); // chapter 2: gone
    }

    [Fact]
    public void Runner_AppliesAutomaticSteps_AndStopsAtScenesBattlesAndTheHub()
    {
        var db = Db();
        Assert.Empty(db.Validate());
        var session = new GameSession();
        var runner = new CampaignRunner(db, session);
        Assert.IsType<SceneStep>(runner.NextInteractive());
        Assert.Equal(500, session.Gold);
        Assert.Equal("Prologue", session.ChapterTitle);
        Assert.Equal("lord", Assert.Single(session.Roster).Def.Id);
        runner.Complete();
        Assert.Contains("met", session.Flags);
        Assert.IsType<BattleStep>(runner.NextInteractive());
    }

    [Fact]
    public void Victory_WritesTheBattleBack_DefeatLeavesTheRosterUntouched()
    {
        var db = Db();
        var session = new GameSession { Mode = GameMode.IronMan };
        var runner = new CampaignRunner(db, session);
        session.Join(db, "partner");
        runner.NextInteractive();
        runner.Complete();
        var step = (BattleStep)runner.NextInteractive()!;
        Assert.True(runner.NeedsPrep(step) || runner.DefaultDeployment(step).Count == 2);
        var launch = runner.Launch(step, new[] { "lord", "partner" });

        var s = BattleSetup.Create(db, launch.Map, launch.Seed, launch.Roster, launch.Options, launch.Deployed);
        var partner = s.Units.Single(u => u.State.Def.Id == "partner");
        Assert.Equal(new GridPos(1, 0), partner.Pos);
        // Mutating the battle copy doesn't touch the roster (a lost battle can be retried cleanly).
        partner.State.Exp = 55;
        Assert.Equal(0, session.Unit("partner")!.Exp);

        s.Defeat(partner);
        s.GoldGained = 300;
        s.Flags.Add("won");
        var report = runner.ApplyVictory(s);
        Assert.Equal(new[] { "Partner" }, report.Fallen);
        Assert.Equal(UnitStatus.Dead, session.Unit("partner")!.Status);
        Assert.Equal(new[] { "partner" }, session.Fallen);
        Assert.Equal(55, session.Unit("partner")!.Exp);
        Assert.Equal(800 + CampaignRunner.VictoryGold(session.Chapter), session.Gold);
        Assert.Equal(CampaignRunner.SurvivorExp, session.Unit("lord")!.Exp);
        Assert.Contains("won", session.Flags);
    }

    [Fact]
    public void Casual_FallenUnitsComeBack()
    {
        var db = Db();
        var session = new GameSession { Mode = GameMode.Casual };
        var runner = new CampaignRunner(db, session);
        session.Join(db, "lord");
        session.Join(db, "partner");
        var launch = runner.Launch(new BattleStep("test", 2, Array.Empty<string>()), new[] { "lord", "partner" });
        var s = BattleSetup.Create(db, launch.Map, launch.Seed, launch.Roster, launch.Options, launch.Deployed);
        s.Defeat(s.Units.Single(u => u.State.Def.Id == "partner"));
        runner.ApplyVictory(s);
        Assert.Equal(UnitStatus.Active, session.Unit("partner")!.Status);
        Assert.Empty(session.Fallen);
    }

    [Fact]
    public void Victory_AddsSurvivalSupportPoints_AndUnlocksConversations()
    {
        var db = Db();
        var session = new GameSession();
        var runner = new CampaignRunner(db, session);
        session.Join(db, "lord");
        session.Join(db, "partner");
        var launch = runner.Launch(new BattleStep("test", 2, Array.Empty<string>()), new[] { "lord", "partner" });
        var s = BattleSetup.Create(db, launch.Map, launch.Seed, launch.Roster, launch.Options, launch.Deployed);
        var report = runner.ApplyVictory(s);
        Assert.Equal(Supports.SurvivePoints, session.SupportPointsOf("lord", "partner"));
        var available = Assert.Single(session.AvailableSupports(db));
        Assert.Equal("support_lord_partner_C", available.SceneId);
        Assert.Single(report.NewSupports);
        session.MarkSupportSeen("lord", "partner", 1);
        Assert.Equal(1, session.SupportRank("partner", "lord"));
        Assert.Empty(session.AvailableSupports(db));
    }

    [Fact]
    public void SupportStep_PlaysTheRanksNotYetWatched_UpToItsRank()
    {
        var db = Db();
        var step = Assert.IsType<SupportStep>(CampaignParser.Parse("support lord partner A").Steps[0]);
        Assert.Equal(new SupportStep("lord", "partner", 3), step);
        Assert.Throws<FormatException>(() => CampaignParser.Parse("support lord partner S"));

        var session = new GameSession();
        var runner = new CampaignRunner(db, session);
        session.Join(db, "lord");
        Assert.Null(runner.PendingSupportScene(step)); // partner not recruited
        session.Join(db, "partner");
        session.MarkSupportSeen("lord", "partner", 1); // C watched in the hub
        Assert.Equal(("lord", "partner", 2, "support_lord_partner_B"), runner.PendingSupportScene(step));
        session.MarkSupportSeen("lord", "partner", 2);
        Assert.Equal(3, runner.PendingSupportScene(step)!.Value.Rank);
        session.MarkSupportSeen("lord", "partner", 3);
        Assert.Null(runner.PendingSupportScene(step));
    }

    [Fact]
    public void MissedRecruits_GetTheirScenesRetold()
    {
        var db = Db();
        foreach (var scene in DialogueParser.Parse("""
            ### post
            YSRA: News.
            LORD: Thanks.
            ### post_no_ysra
            LORD: I read the letter.
            ### banter
            YSRA: Hm.
            LORD: Ha.
            """))
            db.Add(scene);
        var session = new GameSession();
        Assert.Equal(2, session.Scene(db, "post")!.Lines.Count);
        session.Flags.Add(CampaignRunner.MissedFlag("ysra"));
        Assert.Equal("I read the letter.", Assert.IsType<SpeakLine>(Assert.Single(session.Scene(db, "post")!.Lines)).Text);
        Assert.Equal("lord", Assert.IsType<SpeakLine>(Assert.Single(session.Scene(db, "banter")!.Lines)).Speaker);
        Assert.Null(session.Scene(db, "nothing"));
    }

    [Fact]
    public void FallenCompanions_LeaveScenes_AndStandInsTakeTheirLines()
    {
        var db = Db();
        foreach (var scene in DialogueParser.Parse("""
            ### briefing
            SABLE|PIP|KAI [smile]: I can get us in.
            DORRAN: Mind the shallows.
            LORD: Go.
            """))
            db.Add(scene);
        var session = new GameSession();
        var full = session.Scene(db, "briefing")!.Lines.Cast<SpeakLine>().ToList();
        Assert.Equal(new[] { "sable", "dorran", "lord" }, full.Select(l => l.Speaker));
        Assert.Contains("kai", session.Scene(db, "briefing")!.Speakers);

        session.Flags.Add(CampaignRunner.FellFlag("sable"));
        session.Flags.Add(CampaignRunner.FellFlag("dorran"));
        var told = session.Scene(db, "briefing")!.Lines.Cast<SpeakLine>().ToList();
        Assert.Equal(new[] { "pip", "lord" }, told.Select(l => l.Speaker));
        Assert.Equal("smile", told[0].Expression);
        Assert.Equal("I can get us in.", told[0].Text);

        // Battle scenes keep whoever is on the field, stand-ins included.
        var field = db.Scenes["briefing"].OnlySpeakers(new HashSet<string> { "kai", "lord" }).Lines.Cast<SpeakLine>();
        Assert.Equal(new[] { "kai", "lord" }, field.Select(l => l.Speaker));
    }

    [Fact]
    public void SaveData_RoundTrips()
    {
        var db = Db();
        var session = new GameSession { Mode = GameMode.IronMan, Difficulty = Difficulty.Hard, StepIndex = 4, Chapter = 2, Gold = 1234, CrestTier = 1 };
        var lord = session.Join(db, "lord");
        lord.Level = 5;
        lord.Exp = 42;
        lord.Gains = new StatBlock(1, 2, 0, 3, 0, 0, 1, 0, 0);
        lord.Inventory.Add("vulnerary");
        lord.ArmorId = null;
        session.Convoy.Add("iron_sword");
        session.Flags.Add("mira_fire");
        session.Unlocks.Add("smithy");
        session.SupportPoints["lord|partner"] = 12;
        session.SupportRanks["lord|partner"] = 1;

        var json = SaveData.FromSession(session, "Ch 2").ToJson();
        var loaded = SaveData.FromJson(json).ToSession(db);
        Assert.Equal(GameMode.IronMan, loaded.Mode);
        Assert.Equal(Difficulty.Hard, loaded.Difficulty);
        Assert.Equal(4, loaded.StepIndex);
        Assert.Equal(1234, loaded.Gold);
        var l = loaded.Unit("lord")!;
        Assert.Equal(5, l.Level);
        Assert.Equal(42, l.Exp);
        Assert.Equal(lord.Gains, l.Gains);
        Assert.Equal(new[] { "sword", "vulnerary" }, l.Inventory);
        Assert.Equal(new[] { "iron_sword" }, loaded.Convoy);
        Assert.Contains("mira_fire", loaded.Flags);
        Assert.Contains("smithy", loaded.Unlocks);
        Assert.Equal(1, loaded.SupportRank("partner", "lord"));
    }

    [Fact]
    public void Commands_RoundTripThroughJson()
    {
        var commands = new List<IBattleCommand>
        {
            new ActCommand("u0", new GridPos(2, 3), new WaitAction()),
            new ActCommand("u0", new GridPos(1, 1), new AttackAction("u1", new WeaponAttack("sword"))),
            new ActCommand("u0", new GridPos(1, 1), new AttackAction("u1", new ArtAttack("smash", "sword"))),
            new ActCommand("u2", new GridPos(0, 0), new HealAction("u0", "heal")),
            new ActCommand("u0", new GridPos(0, 1), new TalkAction("u3")),
            new ActCommand("u0", new GridPos(0, 1), new VisitAction()),
            new ActCommand("u0", new GridPos(0, 1), new SeizeAction()),
            new ActCommand("u0", new GridPos(0, 1), new EscapeAction()),
            new ActCommand("u0", new GridPos(0, 1), new UseItemAction("vulnerary")),
            new ActCommand("u0", new GridPos(0, 1), new CrestAction(CrestSkill.Ward, "u2")),
            new ActCommand("u0", new GridPos(0, 1), new CrestAction(CrestSkill.Rally, null)),
            new TradeCommand("u0", new GridPos(1, 0), "u2", new[] { "sword" }, new[] { "vulnerary", "heal" }),
            new EndPhaseCommand(),
        };
        var data = new SuspendData(SuspendData.CurrentVersion, "x", SaveData.FromSession(new GameSession(), "x"), 7,
            new List<string> { "lord" }, commands);
        var loaded = SuspendData.FromJson(data.ToJson());
        Assert.Equal(commands.Count, loaded.Commands.Count);
        Assert.Equal(data.ToJson(), loaded.ToJson());
        Assert.Equal(commands[2], loaded.Commands[2]);
        Assert.IsType<EndPhaseCommand>(loaded.Commands[^1]);
    }

    [Fact]
    public void Suspend_ReplaysToTheSameBattle()
    {
        var db = Db();
        var session = new GameSession { Seed = 99 };
        var runner = new CampaignRunner(db, session);
        session.Join(db, "partner");
        runner.NextInteractive();
        runner.Complete();
        var step = (BattleStep)runner.NextInteractive()!;
        var launch = runner.Launch(step, new[] { "lord", "partner" });

        // Both sides played by the AI, suspended halfway through.
        var s = BattleSetup.Create(db, launch.Map, launch.Seed, launch.Roster, launch.Options, launch.Deployed);
        var turns = new TurnManager(s);
        turns.StartBattle();
        for (int i = 0; i < 12 && s.Outcome == BattleOutcome.InProgress; i++)
            Assert.True(turns.Submit(AiController.NextDecision(s)?.Command ?? new EndPhaseCommand()).Ok);
        Assert.NotEmpty(s.CommandLog);
        var json = SuspendData.FromBattle(session, launch, s).ToJson();

        var data = SuspendData.FromJson(json);
        var resumedSession = data.Session.ToSession(db);
        var resumedRunner = new CampaignRunner(db, resumedSession);
        var again = resumedRunner.Launch((BattleStep)resumedRunner.Current!, data.Deployed) with { Seed = data.Seed };
        var r = BattleSetup.Create(db, again.Map, again.Seed, again.Roster, again.Options, again.Deployed);
        Assert.True(new TurnManager(r).Replay(data.Commands));

        string Fingerprint(BattleState b) => $"T{b.Turn} {b.Phase} {b.Outcome} rng {((Rng)b.Rng).State} " + string.Join("; ",
            b.Units.Select(u => $"{u.Id} {u.Pos} hp {u.CurrentHp} {u.ActionState} lv {u.State.Level} exp {u.State.Exp} {u.IsDefeated}"));
        Assert.Equal(Fingerprint(s), Fingerprint(r));
    }

    [Fact]
    public void Shop_BuysAndSells()
    {
        var db = Db();
        var s = new GameSession { Gold = 500 };
        Assert.Null(HubService.Buy(db, s, "iron_sword"));
        Assert.Equal(100, s.Gold);
        Assert.Equal("Not enough gold.", HubService.Buy(db, s, "iron_sword"));
        Assert.Equal("Not sold here.", HubService.Buy(db, s, "killer"));
        Assert.Null(HubService.Sell(db, s, "iron_sword"));
        Assert.Equal(300, s.Gold);
        Assert.Empty(s.Convoy);
    }

    [Fact]
    public void Shop_BuysForAUnitIntoItsPackElseTheConvoy()
    {
        var db = Db();
        var s = new GameSession { Gold = 1000 };
        var lord = s.Join(db, "lord");
        Assert.Null(HubService.BuyFor(db, s, lord, "iron_sword"));
        Assert.Equal(new[] { "sword", "iron_sword" }, lord.Inventory);
        Assert.Empty(s.Convoy);
        Assert.Equal(600, s.Gold);
        while (lord.Inventory.Count < UnitState.MaxInventory)
            lord.Inventory.Add("vulnerary");
        Assert.Null(HubService.BuyFor(db, s, lord, "vulnerary")); // full pack: to the convoy
        Assert.Equal(new[] { "vulnerary" }, s.Convoy);
    }

    [Fact]
    public void Convoy_MovesItemsInAndOutOfInventories()
    {
        var db = Db();
        var s = new GameSession();
        var lord = s.Join(db, "lord");
        s.Convoy.Add("vulnerary");
        Assert.Null(HubService.GiveToUnit(db, s, lord, "vulnerary"));
        Assert.Equal(new[] { "sword", "vulnerary" }, lord.Inventory);
        Assert.Null(HubService.TakeFromUnit(db, s, lord, 0));
        Assert.Equal(new[] { "sword" }, s.Convoy);
        Assert.Null(lord.EquippedItemId);
    }

    [Fact]
    public void Smithy_ReforgesToTheNextTier()
    {
        var db = Db();
        // A cheaper tier-2 specialist must not become the iron sword's upgrade.
        db.Add(db.Weapon("steel_sword") with { Id = "armorslayer", Name = "Armorslayer", Price = 900, EffectiveAgainst = new[] { "Armored" } });
        var s = new GameSession { Gold = 1000 };
        var lord = s.Join(db, "lord");
        lord.Inventory.Add("iron_sword");
        var option = Assert.Single(HubService.ForgeOptions(db, s, lord.Inventory), o => o.FromId == "iron_sword");
        Assert.Equal("steel_sword", option.ToId);
        Assert.Equal(600, option.Cost);
        Assert.Equal("The smithy isn't open yet.", HubService.Forge(db, s, lord, option));
        s.Unlocks.Add("smithy");
        Assert.Null(HubService.Forge(db, s, lord, option));
        Assert.Contains("steel_sword", lord.Inventory);
        Assert.Equal(400, s.Gold);
    }

    [Fact]
    public void Smithy_WorksToSilverInActOne_BlackSteelInActTwo_MythrilInActThree()
    {
        Assert.Equal(3, HubService.MaxForgeTier(7));
        Assert.Equal(4, HubService.MaxForgeTier(8));
        Assert.Equal(4, HubService.MaxForgeTier(15));
        Assert.Equal(5, HubService.MaxForgeTier(16));
    }

    [Fact]
    public void Promotion_NeedsLevelAndCrestTier()
    {
        var db = Db();
        db.Add(new UnitDef("rookie", "Rookie", "merc_p", 1, default, default, Array.Empty<string>(), false));
        var s = new GameSession { CrestTier = 1 };
        s.Unlocks.Add("promotion");
        var rookie = s.Join(db, "rookie");
        Assert.Equal("Needs level 10.", HubService.CannotPromote(db, s, rookie, "hero"));
        rookie.Level = 10;
        Assert.Equal("Needs crest tier 2 (now 1).", HubService.CannotPromote(db, s, rookie, "hero"));
        s.CrestTier = 2;
        Assert.Null(HubService.Promote(db, s, rookie, "hero"));
        Assert.Equal("hero", rookie.ClassId);
        Assert.Equal(1, rookie.Level);
    }

    [Fact]
    public void Crestbound_IsEachUnitsOwnTierThree_AndItsArtFallsBack()
    {
        var db = Db();
        db.Add(db.Class("hero") with { Id = "champion", Name = "Champion", Tier = 3, PromotesFrom = "hero" });
        db.Add(db.Class("hero") with { Id = "blademaster", Name = "Blademaster", Tier = 3, PromotesFrom = "hero" });
        db.Add(new UnitDef("ace", "Ace", "merc_p", 1, default, default, Array.Empty<string>(), false, Crestbound: "champion"));
        db.Add(new UnitDef("rival", "Rival", "merc_p", 1, default, default, Array.Empty<string>(), false, Crestbound: "blademaster"));
        Assert.DoesNotContain(db.Validate(), e => e.Contains("Crestbound") || e.Contains("follows"));
        var s = new GameSession { CrestTier = 2 };
        s.Unlocks.Add("promotion");
        var ace = s.Join(db, "ace");
        var rival = s.Join(db, "rival");
        Assert.Equal(new[] { "hero" }, PromotionService.Targets(db, ace)); // tier 1: the class's own route
        foreach (var u in new[] { ace, rival })
        {
            u.Level = 10;
            Assert.Null(HubService.Promote(db, s, u, "hero"));
            u.Level = 10;
        }
        Assert.Equal(new[] { "champion" }, PromotionService.Targets(db, ace));
        Assert.Equal(new[] { "blademaster" }, PromotionService.Targets(db, rival));
        Assert.Equal("Needs crest tier 3 (now 2).", HubService.CannotPromote(db, s, ace, "champion"));
        Assert.Equal("hero can't become Blademaster.", HubService.CannotPromote(db, s, ace, "blademaster"));
        s.CrestTier = 3;
        Assert.Null(HubService.Promote(db, s, ace, "champion"));
        Assert.Equal(new[] { "hero", "merc_p" }, Outfits.Earlier(db, "champion"));
    }
}
