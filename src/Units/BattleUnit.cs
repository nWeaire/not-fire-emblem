using System.Collections.Generic;
using NotFireEmblem.Grid;

namespace NotFireEmblem.Units;

/// <summary>Why a unit left the map without dying.</summary>
public enum MapExit { None, Escaped, Surrendered }

/// <summary>A unit's battle-only wrapper: position, faction, turn state, current HP and battle statuses.</summary>
public sealed class BattleUnit
{
    public required string Id { get; init; }
    public required UnitState State { get; init; }
    /// <summary>Changes when a unit joins (Talk, events).</summary>
    public required Faction Faction { get; set; }
    public GridPos Pos { get; set; }
    /// <summary>Where the unit started the battle (anchors Boss movement).</summary>
    public GridPos Home { get; init; }
    public ActionState ActionState { get; set; } = ActionState.Ready;
    public int CurrentHp { get; set; }
    public bool IsBoss { get; init; }
    public AiProfile AiProfile { get; set; } = AiProfile.Aggressive;
    public bool IsDefeated { get; set; }
    public MapExit Exit { get; set; }
    /// <summary>Which art set represents this unit (see SpawnDef.ResolveSpriteId).</summary>
    public string SpriteId { get; init; } = "";

    /// <summary>How objectives and triggers refer to this unit (spawn tag, else unit id).</summary>
    public string? Key { get; init; }
    /// <summary>Spawn tag (e.g. "villager" for Defend objectives).</summary>
    public string? Tag { get; init; }
    /// <summary>Key of the crest-bearer whose Dominion holds this unit.</summary>
    public string? DominatedBy { get; set; }
    public bool CrestBearer { get; init; }
    /// <summary>Own phases still to skip (severed beasts are stunned for one).</summary>
    public int StunnedPhases { get; set; }
    /// <summary>Crest skill buffs (Ward, Rally); cleared at the start of this unit's next phase.</summary>
    public StatBlock TempBonus { get; set; }
    public string? TempBonusSource { get; set; }
    /// <summary>Uses left this battle for arts, spells and crest skills (refilled every map).</summary>
    public Dictionary<string, int> UsesLeft { get; } = new();
    /// <summary>EXP earned this battle (for the results screen).</summary>
    public int ExpEarned { get; set; }
    /// <summary>True for roster units and units that joined during the battle: they go back to the roster.</summary>
    public bool IsRosterUnit => Faction == Faction.Player && State.Def.Id.Length > 0 && !State.Def.Id.StartsWith("generic_");

    public bool IsOnMap => !IsDefeated && Exit == MapExit.None;

    public string Name => State.Def.Name;
    public bool IsLord => State.Def.IsLord;

    /// <summary>
    /// Art ids to try, best first. Outfits follow the class, so "&lt;spriteId&gt;_&lt;classId&gt;" (e.g. rowan_lord)
    /// wins over the plain "&lt;spriteId&gt;"; promoting a unit swaps its art with no extra data.
    /// </summary>
    /// <summary>The lord's crest is awake: his sprite shows it ("_crest" art variants, when they exist).</summary>
    public bool ShowsCrest { get; set; }

    public IEnumerable<string> SpriteCandidates()
    {
        if (ShowsCrest)
        {
            yield return $"{SpriteId}_{State.ClassId}_crest";
            yield return $"{SpriteId}_crest";
        }
        yield return $"{SpriteId}_{State.ClassId}";
        yield return SpriteId;
    }

    public override string ToString() => $"{Name}[{Id}]@{Pos}";
}
