using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Core;
using NotFireEmblem.Units;

namespace NotFireEmblem.Campaign;

/// <summary>
/// The whole campaign's persistent state (IMPLEMENTATION §5.1): roster, convoy, gold, story position, flags,
/// crest tier and supports. Battles work on copies of roster units and write them back only on victory.
/// </summary>
public sealed class GameSession
{
    public const int ConvoyCapacity = 100;

    public GameMode Mode { get; set; } = GameMode.Casual;
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;

    /// <summary>Index of the current step in the campaign script.</summary>
    public int StepIndex { get; set; }
    /// <summary>Story chapter number (0 = Prologue); shops open by chapter.</summary>
    public int Chapter { get; set; }
    public string ChapterTitle { get; set; } = "";
    /// <summary>Battles fought, used to derive each battle's RNG seed.</summary>
    public int BattleCount { get; set; }
    public ulong Seed { get; set; } = 1;

    /// <summary>Roster units in join order.</summary>
    public List<UnitState> Roster { get; } = new();
    public List<string> Convoy { get; } = new();
    public int Gold { get; set; }
    /// <summary>Rowan's crest tier (STORY §4.2): 0 Unmarked … 4 Sovereign.</summary>
    public int CrestTier { get; set; }

    public HashSet<string> Flags { get; } = new();
    /// <summary>Hub features unlocked by the story ("smithy", "promotion", "supports", "shop").</summary>
    public HashSet<string> Unlocks { get; } = new();
    public Dictionary<string, int> SupportPoints { get; } = new();
    public Dictionary<string, int> SupportRanks { get; } = new();
    public HashSet<string> SeenScenes { get; } = new();

    public UnitState? Unit(string unitId) => Roster.FirstOrDefault(u => u.Def.Id == unitId);

    public IEnumerable<UnitState> ActiveRoster => Roster.Where(u => u.Status != UnitStatus.Dead);

    public int SupportRank(string a, string b) => SupportRanks.GetValueOrDefault(Supports.PairKey(a, b));

    public int SupportPointsOf(string a, string b) => SupportPoints.GetValueOrDefault(Supports.PairKey(a, b));

    /// <summary>Adds a unit to the roster (fresh from its definition), unless it's already there.</summary>
    public UnitState Join(ContentDatabase db, string unitId)
    {
        if (Unit(unitId) is { } existing)
        {
            if (existing.Status == UnitStatus.Retreated)
                existing.Status = UnitStatus.Active;
            return existing;
        }
        var unit = UnitState.FromDef(db.Unit(unitId));
        Roster.Add(unit);
        return unit;
    }

    public void AddToConvoy(string itemId)
    {
        if (Convoy.Count < ConvoyCapacity)
            Convoy.Add(itemId);
    }

    /// <summary>Support conversations ready to watch in the hub: (unit a, unit b, rank, scene id).</summary>
    public IEnumerable<(string A, string B, int Rank, string SceneId)> AvailableSupports(ContentDatabase db)
    {
        var active = ActiveRoster.Select(u => u.Def.Id).ToList();
        for (int i = 0; i < active.Count; i++)
        {
            for (int j = i + 1; j < active.Count; j++)
            {
                string a = active[i], b = active[j];
                if (Supports.Unlocked(db, a, b, SupportPointsOf(a, b), SupportRank(a, b)) is not { } rank)
                    continue;
                string scene = Supports.SceneId(a, b, rank);
                if (db.Scenes.ContainsKey(scene))
                    yield return (a, b, rank, scene);
            }
        }
    }

    /// <summary>Watching a support conversation raises the pair's rank (GDD §5).</summary>
    public void MarkSupportSeen(string a, string b, int rank)
    {
        SupportRanks[Supports.PairKey(a, b)] = rank;
        SeenScenes.Add(Supports.SceneId(a, b, rank));
    }
}
