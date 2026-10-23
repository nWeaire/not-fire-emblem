using System.Collections.Generic;
using NotFireEmblem.Grid;

namespace NotFireEmblem.Units;

/// <summary>Why a unit left the map without dying.</summary>
public enum MapExit { None, Escaped, Surrendered, Departed, Subdued, Retreated }

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
    /// <summary>Boss AI: how far it may step from <see cref="Home"/> to attack (0 = the default leash).</summary>
    public int Leash { get; init; }
    public AiProfile AiProfile { get; set; } = AiProfile.Aggressive;
    public bool IsDefeated { get; set; }
    public MapExit Exit { get; set; }
    /// <summary>Which art set represents this unit (see SpawnDef.ResolveSpriteId).</summary>
    public string SpriteId { get; set; } = "";
    /// <summary>A name given mid-battle (Ch 8's calmed rioters become Townsfolk); null keeps the definition's.</summary>
    public string? Renamed { get; set; }

    /// <summary>How objectives and triggers refer to this unit (spawn tag, else unit id).</summary>
    public string? Key { get; init; }
    /// <summary>Spawn tag (e.g. "villager" for Defend objectives).</summary>
    public string? Tag { get; init; }
    /// <summary>Key of the crest-bearer whose Dominion holds this unit.</summary>
    public string? DominatedBy { get; set; }
    public bool CrestBearer { get; init; }
    /// <summary>Takes no damage at all (Ch 15's Morvane): every forecast against it shows "—".</summary>
    public bool Invulnerable { get; init; }
    /// <summary>Beaten, not killed: at 0 HP the unit is subdued and leaves the map alive (Ch 14's Empress).</summary>
    public bool NonLethal { get; init; }
    /// <summary>How far this unit's Dominion reaches; 0 = the usual (BattleState.DominionRadius).</summary>
    public int DominionRadius { get; init; }
    /// <summary>Keys the AI leaves alone, "|"-separated (see SpawnDef.Ignores).</summary>
    public string? Ignores { get; init; }
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

    public string Name => Renamed ?? State.Def.Name;
    public bool IsLord => State.Def.IsLord;

    /// <summary>Earlier classes, nearest first (see Outfits.Earlier): their outfits stand in for art not drawn yet.</summary>
    public IReadOnlyList<string> EarlierClasses { get; init; } = System.Array.Empty<string>();

    /// <summary>The lord's crest is awake: his sprite shows it ("_crest" art variants, when they exist).</summary>
    public bool ShowsCrest { get; set; }

    /// <summary>
    /// Stand-ins for art not drawn yet (docs/ART_NEEDED.md section 0). Data names the real sprite id; until that art
    /// exists the unit borrows this one, so the art drops in later with no data change.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> PlaceholderSprites = new Dictionary<string, string>
    {
        ["neutral_rioter"] = "ally_villager",
        ["ally_townsfolk"] = "ally_villager",
        ["enemy_thief"] = "enemy_myrmidon",
        ["enemy_rogue"] = "enemy_myrmidon",
        ["enemy_troubadour"] = "enemy_cavalier",
        ["enemy_valkyrie"] = "enemy_cavalier",
        ["enemy_wyvern_rider"] = "enemy_pegasus_knight",
        ["enemy_wyvern_lord"] = "enemy_pegasus_knight",
        ["enemy_general"] = "enemy_knight",
        ["enemy_swordmaster"] = "enemy_myrmidon",
        ["enemy_falcon_knight"] = "enemy_pegasus_knight",
        ["enemy_sniper"] = "enemy_archer",
        ["enemy_warrior"] = "enemy_fighter",
        ["enemy_sage"] = "enemy_mage",
        ["enemy_paladin"] = "enemy_cavalier",
        ["enemy_hollowed"] = "enemy_soldier",
    };

    /// <summary>
    /// Art ids to try, best first. Outfits follow the class, so "&lt;spriteId&gt;_&lt;classId&gt;" (e.g. rowan_lord)
    /// wins over the plain "&lt;spriteId&gt;"; promoting a unit swaps its art with no extra data. Units whose art isn't
    /// drawn yet fall back to a stand-in, then to the generic look for their class ("enemy_&lt;classId&gt;").
    /// </summary>
    public IEnumerable<string> SpriteCandidates()
    {
        if (ShowsCrest)
        {
            yield return $"{SpriteId}_{State.ClassId}_crest";
            yield return $"{SpriteId}_crest";
        }
        yield return $"{SpriteId}_{State.ClassId}";
        foreach (var earlier in EarlierClasses)
            yield return $"{SpriteId}_{earlier}";
        yield return SpriteId;
        if (PlaceholderSprites.TryGetValue(SpriteId, out var stand))
            yield return stand;
        string generic = $"enemy_{State.ClassId}";
        if (generic == SpriteId)
            yield break;
        yield return generic;
        if (PlaceholderSprites.TryGetValue(generic, out var genericStand))
            yield return genericStand;
    }

    public override string ToString() => $"{Name}[{Id}]@{Pos}";
}
