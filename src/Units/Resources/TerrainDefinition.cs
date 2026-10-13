using System.Collections.Generic;
using Godot;

namespace NotFireEmblem.Units;

[GlobalClass]
public partial class TerrainDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Name { get; set; } = "";
    /// <summary>The single character that stands for this terrain in map rows.</summary>
    [Export] public string Symbol { get; set; } = "";
    [ExportGroup("Move Cost (0 = impassable)")]
    [Export] public int CostInfantry { get; set; } = 1;
    [Export] public int CostArmored { get; set; } = 1;
    [Export] public int CostCavalry { get; set; } = 1;
    [Export] public int CostFlying { get; set; } = 1;
    [ExportGroup("Bonuses")]
    [Export] public int Avoid { get; set; }
    [Export] public int Def { get; set; }
    [Export] public int HealPercent { get; set; }
    [Export] public bool BlocksSight { get; set; }
    [ExportGroup("Visuals")]
    [Export] public Color Color { get; set; } = Colors.Green;

    public TerrainDef ToDef() => new(
        Id,
        Name,
        Symbol.Length == 1 ? Symbol[0] : throw new System.FormatException($"Terrain '{Id}' symbol must be one character."),
        new Dictionary<MovementType, int>
        {
            [MovementType.Infantry] = CostInfantry,
            [MovementType.Armored] = CostArmored,
            [MovementType.Cavalry] = CostCavalry,
            [MovementType.Flying] = CostFlying,
        },
        Avoid,
        Def,
        HealPercent,
        BlocksSight,
        Color.ToHtml(false));
}
