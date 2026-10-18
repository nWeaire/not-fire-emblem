using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Battle;
using NotFireEmblem.Core;
using NotFireEmblem.Units;

namespace NotFireEmblem.Campaign;

/// <summary>Everything a battle scene needs to start a campaign battle.</summary>
public sealed record BattleLaunch(
    MapDef Map,
    ulong Seed,
    Dictionary<string, UnitState> Roster,   // copies of the roster units; written back on victory
    IReadOnlyList<string> Deployed,
    BattleOptions Options);

/// <summary>What the results screen shows after a victory.</summary>
public sealed record BattleReport(
    int Gold,
    IReadOnlyList<string> Items,
    IReadOnlyList<(string Name, int Exp, int Level)> Units,
    IReadOnlyList<string> Recruited,
    IReadOnlyList<string> Fallen,
    IReadOnlyList<string> NewSupports);

/// <summary>
/// Walks the campaign script (IMPLEMENTATION §5.1 SceneRouter flow). Automatic steps (join, class, crest, gold,
/// items, flags, unlocks, chapter titles) are applied immediately; scenes, battles, the hub and the end are handed
/// to the UI, which calls <see cref="Complete"/> when the player is done with them.
/// </summary>
public sealed class CampaignRunner
{
    public ContentDatabase Db { get; }
    public GameSession Session { get; }

    public CampaignRunner(ContentDatabase db, GameSession session)
    {
        Db = db;
        Session = session;
    }

    private IReadOnlyList<CampaignStep> Steps => Db.Campaign?.Steps ?? System.Array.Empty<CampaignStep>();

    public CampaignStep? Current => Session.StepIndex < Steps.Count ? Steps[Session.StepIndex] : null;

    /// <summary>Applies automatic steps and returns the next step that needs the player (null at the very end).</summary>
    public CampaignStep? NextInteractive()
    {
        while (Current is { } step && ApplyAutomatic(step))
            Session.StepIndex++;
        return Current;
    }

    /// <summary>Finishes the current interactive step.</summary>
    public void Complete()
    {
        if (Current is SceneStep scene)
            SceneWatched(scene.SceneId);
        Session.StepIndex++;
    }

    /// <summary>Records a watched scene and sets its flags.</summary>
    public void SceneWatched(string sceneId)
    {
        Session.SeenScenes.Add(sceneId);
        if (Db.Scenes.TryGetValue(sceneId, out var scene))
            Session.Flags.UnionWith(scene.Flags);
    }

    private bool ApplyAutomatic(CampaignStep step)
    {
        switch (step)
        {
            case ChapterStep c:
                Session.Chapter = c.Number;
                Session.ChapterTitle = c.Title;
                return true;
            case JoinStep j:
                Session.Join(Db, j.UnitId);
                return true;
            case LeaveStep l:
                if (Session.Unit(l.UnitId) is { } leaving)
                    leaving.Status = UnitStatus.Dead;
                return true;
            case ClassStep c when Session.Unit(c.UnitId) is { } unit:
                unit.ClassId = c.ClassId;
                if (EquipmentService.RemoveInvalidArmor(Db, unit) is { } removed)
                    Session.AddToConvoy(removed);
                return true;
            case ClassStep:
                return true;
            case CrestStep c:
                Session.CrestTier = System.Math.Max(Session.CrestTier, c.Tier);
                return true;
            case GoldStep g:
                Session.Gold += g.Amount;
                return true;
            case ItemStep i:
                Session.AddToConvoy(i.ItemId);
                return true;
            case FlagStep f:
                Session.Flags.Add(f.Flag);
                return true;
            case UnlockStep u:
                Session.Unlocks.Add(u.Feature);
                return true;
            default:
                return false;
        }
    }

    // ------------------------------------------------------------------ battles

    /// <summary>Roster unit ids the map places itself (story-fixed spawns); they are always deployed.</summary>
    public IReadOnlyList<string> FixedUnits(BattleStep step) =>
        Db.Map(step.MapId).Spawns
            .Where(sp => sp.Faction == Faction.Player && sp.Group is null && sp.UnitId is { } id && Session.Unit(id) is { Status: not UnitStatus.Dead })
            .Select(sp => sp.UnitId!)
            .Concat(step.Forced.Where(id => Session.Unit(id) is { Status: not UnitStatus.Dead }))
            .Distinct()
            .ToList();

    /// <summary>How many units the player may deploy in total (fixed ones included).</summary>
    public int DeployLimit(BattleStep step)
    {
        var fixedUnits = FixedUnits(step);
        int slots = Db.Map(step.MapId).Slots.Count;
        return step.Deploy <= 0 ? fixedUnits.Count : System.Math.Min(step.Deploy, fixedUnits.Count + slots);
    }

    /// <summary>Whether the player gets a prep screen to choose who to bring.</summary>
    public bool NeedsPrep(BattleStep step) =>
        step.Deploy > 0 && Session.ActiveRoster.Count() > FixedUnits(step).Count;

    /// <summary>The default deployment: fixed units, then the roster in join order up to the limit.</summary>
    public List<string> DefaultDeployment(BattleStep step)
    {
        var chosen = FixedUnits(step).ToList();
        foreach (var unit in Session.ActiveRoster)
        {
            if (chosen.Count >= DeployLimit(step))
                break;
            if (!chosen.Contains(unit.Def.Id))
                chosen.Add(unit.Def.Id);
        }
        return chosen;
    }

    public BattleLaunch Launch(BattleStep step, IReadOnlyList<string>? deployed = null)
    {
        deployed ??= DefaultDeployment(step);
        var roster = Session.ActiveRoster.ToDictionary(u => u.Def.Id, u => u.Clone());
        var options = new BattleOptions(Session.CrestTier, Session.Difficulty, Session.SupportRank);
        ulong seed = unchecked(Session.Seed * 6364136223846793005UL + (ulong)Session.BattleCount + 1);
        return new BattleLaunch(Db.Map(step.MapId), seed, roster, deployed, options);
    }

    /// <summary>
    /// Writes a won battle back into the session: roster units (including recruits) replace their roster copies,
    /// fallen units die (Classic) or recover (Casual), and gold, items, flags and support points are added.
    /// </summary>
    /// <summary>
    /// Gold for every victory, rising through the campaign (Prologue 150, Ch 1 200 … Ch 7 500): about one new item a
    /// battle (session 9 playtest: 500 + 150 a chapter left 1850 to spend after Ch 2, enough to re-equip everyone).
    /// </summary>
    public static int VictoryGold(int chapter) => 150 + 50 * chapter;

    /// <summary>EXP every deployed roster unit still standing at the end of a won battle receives.</summary>
    public const int SurvivorExp = 20;

    public BattleReport ApplyVictory(BattleState s)
    {
        var supportsBefore = Session.AvailableSupports(Db).Select(x => x.SceneId).ToHashSet();
        var units = new List<(string, int, int)>();
        var recruited = new List<string>();
        var fallen = new List<string>();
        var survivors = new List<string>();

        foreach (var battleUnit in s.Units.Where(u => u.IsRosterUnit))
        {
            var state = battleUnit.State;
            string id = state.Def.Id;
            bool isNew = Session.Unit(id) is null;
            if (isNew)
                recruited.Add(state.Def.Name);
            if (battleUnit.IsDefeated)
            {
                fallen.Add(state.Def.Name);
                state.Status = Session.Mode == GameMode.Classic ? UnitStatus.Dead : UnitStatus.Active;
            }
            else
            {
                state.Status = UnitStatus.Active;
                survivors.Add(id);
                BattleProgress.AwardExp(s, battleUnit, SurvivorExp, new List<BattleEvent>());
            }
            int index = Session.Roster.FindIndex(u => u.Def.Id == id);
            if (index >= 0)
                Session.Roster[index] = state;
            else
                Session.Roster.Add(state);
            units.Add((state.Def.Name, battleUnit.ExpEarned, state.Level));
        }

        int gold = s.GoldGained + VictoryGold(Session.Chapter);
        Session.Gold += gold;
        foreach (var item in s.ItemsToConvoy)
            Session.AddToConvoy(item);
        Session.Flags.UnionWith(s.Flags);
        foreach (var (pair, points) in s.SupportPoints)
            Session.SupportPoints[pair] = Session.SupportPoints.GetValueOrDefault(pair) + points;
        for (int i = 0; i < survivors.Count; i++)
        {
            for (int j = i + 1; j < survivors.Count; j++)
            {
                if (Supports.Find(Db, survivors[i], survivors[j]) is null)
                    continue;
                string pair = Supports.PairKey(survivors[i], survivors[j]);
                Session.SupportPoints[pair] = Session.SupportPoints.GetValueOrDefault(pair) + Supports.SurvivePoints;
            }
        }
        Session.BattleCount++;
        var newSupports = Session.AvailableSupports(Db).Where(x => !supportsBefore.Contains(x.SceneId))
            .Select(x => $"{Db.Unit(x.A).Name} & {Db.Unit(x.B).Name} ({Supports.RankNames[x.Rank]})").ToList();
        return new BattleReport(gold, s.ItemsToConvoy.ToList(), units, recruited, fallen, newSupports);
    }
}
