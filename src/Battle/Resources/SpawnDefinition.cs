using System;
using System.Linq;
using Godot;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

[GlobalClass]
public partial class SpawnDefinition : Resource
{
    /// <summary>A named unit's id. Leave empty for a generic unit built from ClassId + Level + Inventory.</summary>
    [Export] public string UnitId { get; set; } = "";
    [Export] public string ClassId { get; set; } = "";
    /// <summary>Display name for generic units (defaults to the class name).</summary>
    [Export] public string Name { get; set; } = "";
    [Export] public int Level { get; set; } = 1;
    [Export] public string[] Inventory { get; set; } = Array.Empty<string>();
    [Export] public Faction Faction { get; set; } = Faction.Enemy;
    [Export] public Vector2I Position { get; set; }
    [Export] public AiProfile Ai { get; set; } = AiProfile.Aggressive;
    [Export] public bool IsBoss { get; set; }
    /// <summary>Art set id under assets/sprites/. Empty = derived from UnitId or "enemy_&lt;ClassId&gt;".</summary>
    [Export] public string SpriteId { get; set; } = "";
    /// <summary>Body armour for generic units (empty = none).</summary>
    [Export] public string Armor { get; set; } = "";
    /// <summary>Key for objectives and triggers (defaults to the UnitId). Also the Defend tag, e.g. "villager".</summary>
    [Export] public string Tag { get; set; } = "";
    /// <summary>Reinforcement group: spawned by a "spawn GROUP" trigger instead of at the start.</summary>
    [Export] public string Group { get; set; } = "";
    /// <summary>Key of the crest-bearer whose Dominion holds this unit.</summary>
    [Export] public string DominatedBy { get; set; } = "";
    /// <summary>Projects Dominion over the units it dominates; its death severs them.</summary>
    [Export] public bool CrestBearer { get; set; }
    /// <summary>Only placed on Hard difficulty.</summary>
    [Export] public bool HardOnly { get; set; }
    /// <summary>Boss AI: how far it may step from its start to attack (0 = the default, AiController.BossLeash).</summary>
    [Export] public int Leash { get; set; }
    /// <summary>A tag the AI never goes after (e.g. "civilian": Ch 8's Veiled leave the townsfolk to the mob).</summary>
    [Export] public string Ignores { get; set; } = "";

    public SpawnDef ToDef() => new(
        string.IsNullOrEmpty(UnitId) ? null : UnitId,
        string.IsNullOrEmpty(ClassId) ? null : ClassId,
        string.IsNullOrEmpty(Name) ? null : Name,
        Level,
        Inventory.ToArray(),
        Faction,
        new GridPos(Position.X, Position.Y),
        Ai,
        IsBoss,
        string.IsNullOrEmpty(SpriteId) ? null : SpriteId,
        string.IsNullOrEmpty(Armor) ? null : Armor,
        string.IsNullOrEmpty(Tag) ? null : Tag,
        string.IsNullOrEmpty(Group) ? null : Group,
        string.IsNullOrEmpty(DominatedBy) ? null : DominatedBy,
        CrestBearer,
        HardOnly,
        Leash,
        string.IsNullOrEmpty(Ignores) ? null : Ignores);
}
