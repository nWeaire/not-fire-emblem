using NotFireEmblem.Battle;
using NotFireEmblem.Campaign;
using NotFireEmblem.Combat;
using NotFireEmblem.Core;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Tests;

/// <summary>M6: the campaign script, roster write-back, saves, and the hub's shop, smithy and promotion rules.</summary>
public class CampaignTests
{
    private static ContentDatabase Db()
    {
        var b = new TestBattle(".....", ".....")
            .Lord(0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 4, 1, "sword");
        var db = b.Db;
        db.Add(new UnitDef("partner", "Partner", "merc", 1, default, default, new[] { "sword" }, false,
            Supports: new[] { new SupportDef("lord", 5, 20, 30) }));
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
        var session = new GameSession();
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
        Assert.Equal(55, session.Unit("partner")!.Exp);
        Assert.Equal(800, session.Gold);
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
    public void SaveData_RoundTrips()
    {
        var db = Db();
        var session = new GameSession { Mode = GameMode.Casual, Difficulty = Difficulty.Hard, StepIndex = 4, Chapter = 2, Gold = 1234, CrestTier = 1 };
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
        Assert.Equal(GameMode.Casual, loaded.Mode);
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
}
