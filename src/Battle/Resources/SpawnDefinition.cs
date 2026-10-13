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
        string.IsNullOrEmpty(Armor) ? null : Armor);
}
