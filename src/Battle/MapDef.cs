using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Core;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>
/// A unit placed at battle start. Either a named unit (<see cref="UnitId"/>) or a generic one built from
/// <see cref="ClassId"/> + <see cref="Level"/> + <see cref="Inventory"/>.
/// </summary>
public sealed record SpawnDef(
    string? UnitId,
    string? ClassId,
    string? Name,
    int Level,
    IReadOnlyList<string> Inventory,
    Faction Faction,
    GridPos Pos,
    AiProfile Ai = AiProfile.Aggressive,
    bool IsBoss = false,
    string? SpriteId = null,
    string? Armor = null,         // body armour for generic units (named units bring their own)
    string? Tag = null,           // key for objectives and triggers (defaults to the UnitId)
    string? Group = null,         // reinforcement group: spawned by a trigger instead of at the start
    string? DominatedBy = null,   // key of the crest-bearer whose Dominion holds this unit
    bool CrestBearer = false,     // projects Dominion; its death severs the units it dominates
    bool HardOnly = false,        // only placed on Hard difficulty
    int Leash = 0,                // Boss AI: how far it may step from its start to attack (0 = AiController.BossLeash)
    string? Ignores = null,       // keys ("civilian", "gull|halde") the AI leaves alone unless the player commands them
    int DominionRadius = 0,       // how far this bearer's Dominion reaches (0 = BattleState.DominionRadius, 3)
    bool NonLethal = false,       // at 0 HP the unit is subdued, not killed (Ch 14's Empress)
    bool Invulnerable = false)    // takes no damage at all (Ch 15's Morvane)
{
    /// <summary>How objectives and triggers refer to this unit.</summary>
    public string? Key => Tag ?? UnitId;

    /// <summary>
    /// The art set this unit uses: an explicit SpriteId, else the named unit's id, else "enemy_&lt;classId&gt;"
    /// (or "ally_"/"player_" for other factions).
    /// </summary>
    public string ResolveSpriteId() =>
        SpriteId ?? UnitId ?? $"{Faction.ToString().ToLowerInvariant()}_{ClassId}";
}

public sealed record MapDef(
    string Id,
    string Name,
    IReadOnlyList<string> TerrainRows,
    IReadOnlyList<string> HeightRows,
    IReadOnlyList<SpawnDef> Spawns,
    Objectives? Objectives = null,
    IReadOnlyList<TriggerDef>? Triggers = null,
    bool Fog = false,
    int VisionBonus = 0,
    IReadOnlyList<GridPos>? DeploySlots = null, // where deployed roster units without a fixed spawn start
    string? Background = null,                  // the battle screen's backdrop for open ground (assets/backgrounds/)
    string? ArtSet = null,                      // art variant: "farm" = no stone (hedgerow walls, dirt yards and squares)
    string? Lighting = null,                    // "night" darkens the map and battle screen; "snow" chills it, with snowfall
    IReadOnlyList<MapHighlight>? Highlights = null) // points of interest shown with the objectives before turn 1
{
    public IReadOnlyList<GridPos> Slots => DeploySlots ?? System.Array.Empty<GridPos>();

    public Objectives Goals => Objectives ?? Battle.Objectives.Default;
    public IReadOnlyList<TriggerDef> AllTriggers => Triggers ?? System.Array.Empty<TriggerDef>();
    public IReadOnlyList<MapHighlight> AllHighlights => Highlights ?? System.Array.Empty<MapHighlight>();

    public BattleGrid BuildGrid(ContentDatabase db) => BattleGrid.FromRows(TerrainRows, HeightRows, db.TerrainBySymbol);

    public IEnumerable<string> Validate(ContentDatabase db)
    {
        var errors = new List<string>();
        BattleGrid? grid = null;
        try
        {
            grid = BuildGrid(db);
        }
        catch (System.Exception e)
        {
            errors.Add($"Map '{Id}': {e.Message}");
        }

        foreach (var spawn in Spawns)
        {
            string where = $"Map '{Id}' spawn at {spawn.Pos}";
            if (spawn.UnitId is null && spawn.ClassId is null)
                errors.Add($"{where} needs a UnitId or a ClassId.");
            if (spawn.UnitId is not null && !db.Units.ContainsKey(spawn.UnitId))
                errors.Add($"{where} has unknown unit '{spawn.UnitId}'.");
            if (spawn.ClassId is not null && !db.Classes.ContainsKey(spawn.ClassId))
                errors.Add($"{where} has unknown class '{spawn.ClassId}'.");
            errors.AddRange(spawn.Inventory.Where(i => !db.IsItem(i)).Select(i => $"{where} carries unknown item '{i}'."));
            if (spawn.Armor is not null && db.TryArmor(spawn.Armor) is not { Slot: ArmorSlot.Body })
                errors.Add($"{where} wears unknown body armour '{spawn.Armor}'.");
            if (grid is not null && !grid.InBounds(spawn.Pos))
                errors.Add($"{where} is outside the map.");
        }
        // Reinforcements may share a cell with a starting unit: they are displaced when they arrive.
        foreach (var dup in Spawns.Where(s => s.Group is null).GroupBy(s => s.Pos).Where(g => g.Count() > 1))
            errors.Add($"Map '{Id}' has {dup.Count()} spawns on {dup.Key}.");

        var keys = Spawns.Select(s => s.Key).OfType<string>().ToHashSet();
        var groups = Spawns.Select(s => s.Group).OfType<string>().ToHashSet();
        void Key(string key, string what)
        {
            // Deployed roster units have no spawn on the map but are keyed by their unit id.
            if (key is not ("actor" or "any" or "all" or "player" or "enemy" or "ally" or "neutral") && !keys.Contains(key) && !db.Units.ContainsKey(key))
                errors.Add($"Map '{Id}' {what} refers to unknown unit key '{key}'.");
        }
        foreach (var spawn in Spawns.Where(s => s.DominatedBy is not null))
            Key(spawn.DominatedBy!, $"spawn at {spawn.Pos} DominatedBy");
        var goals = Goals;
        if (goals.BossKey is not null) Key(goals.BossKey, "victory");
        foreach (var k in goals.EscapeKeys.Concat(goals.MustSurvive)) Key(k, "objective");
        foreach (var rule in goals.Protect.Where(r => !Spawns.Any(s => s.Tag == r.Tag)))
            errors.Add($"Map '{Id}' protects tag '{rule.Tag}' but no spawn has it.");
        foreach (var highlight in AllHighlights.Where(h => h.Key is not null))
            Key(highlight.Key!, "highlight");
        if (goals.Victory is VictoryKind.Seize or VictoryKind.Escape && goals.Cells.Count == 0)
            errors.Add($"Map '{Id}' {goals.Victory} victory needs cells.");
        foreach (var trigger in AllTriggers)
        {
            switch (trigger.Condition)
            {
                case DefeatedCondition c: Key(c.Key, "trigger"); break;
                case SubduedCondition c: Key(c.Key, "trigger"); break;
                case EnterCondition c: Key(c.Who, "trigger"); break;
                case AdjacentCondition c: Key(c.A, "trigger"); Key(c.B, "trigger"); break;
                case HpBelowCondition c: Key(c.Key, "trigger"); break;
                case TalkCondition c:
                    foreach (var actor in c.Actor.Split('|'))
                        Key(actor, "trigger");
                    Key(c.Target, "trigger");
                    break;
                case CombatCondition c: Key(c.Key, "trigger"); break;
            }
            foreach (var effect in trigger.Effects)
            {
                switch (effect)
                {
                    case SceneEffect e when !db.Scenes.ContainsKey(e.SceneId):
                        errors.Add($"Map '{Id}' trigger plays unknown scene '{e.SceneId}'."); break;
                    case SpawnEffect e when !groups.Contains(e.Group):
                        errors.Add($"Map '{Id}' trigger spawns unknown group '{e.Group}'."); break;
                    case FactionEffect e: Key(e.Key, "trigger"); break;
                    case DamageEffect e: Key(e.Key, "trigger"); break;
                    case LeaveEffect e: Key(e.Key, "trigger"); break;
                    case GiveEffect e:
                        Key(e.Key, "trigger");
                        if (!db.IsItem(e.ItemId))
                            errors.Add($"Map '{Id}' trigger gives unknown item '{e.ItemId}'.");
                        break;
                    case TerrainEffect e when db.Terrains.Values.All(t => t.Symbol != e.Symbol):
                        errors.Add($"Map '{Id}' trigger sets unknown terrain symbol '{e.Symbol}'."); break;
                }
            }
        }
        return errors;
    }
}
