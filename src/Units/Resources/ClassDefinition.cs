using System;
using System.Linq;
using Godot;

namespace NotFireEmblem.Units;

[GlobalClass]
public partial class ClassDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Name { get; set; } = "";
    [Export] public StatsDefinition? Bases { get; set; }
    [Export] public StatsDefinition? Growths { get; set; }
    [Export] public StatsDefinition? Caps { get; set; }
    [Export] public MovementType MovementType { get; set; } = MovementType.Infantry;
    /// <summary>WeaponType names, e.g. "Sword", "Lance".</summary>
    [Export] public string[] UsableWeapons { get; set; } = Array.Empty<string>();
    /// <summary>Class ids this class can promote into.</summary>
    [Export] public string[] PromotesTo { get; set; } = Array.Empty<string>();
    [Export] public bool IsPromoted { get; set; }
    /// <summary>Heaviest body armour the class can wear.</summary>
    [Export] public ArmorClass MaxArmor { get; set; } = ArmorClass.Medium;
    /// <summary>1 base, 2 advanced, 3 crestbound, 4 final. Promotion needs Rowan's crest tier ≥ this.</summary>
    [Export] public int Tier { get; set; } = 1;
    /// <summary>Class traits, e.g. "Beast" (effective damage targets, severed beasts are stunned).</summary>
    [Export] public string[] Traits { get; set; } = Array.Empty<string>();
    /// <summary>Combat art ids every unit of this class knows.</summary>
    [Export] public string[] Arts { get; set; } = Array.Empty<string>();
    /// <summary>Spell ids every unit of this class knows.</summary>
    [Export] public string[] Spells { get; set; } = Array.Empty<string>();

    public ClassDef ToDef() => new(
        Id,
        Name,
        StatsDefinition.ToBlock(Bases),
        StatsDefinition.ToBlock(Growths),
        StatsDefinition.ToBlock(Caps),
        MovementType,
        UsableWeapons.Select(w => Enum.Parse<WeaponType>(w, true)).ToArray(),
        PromotesTo.ToArray(),
        IsPromoted,
        MaxArmor,
        Tier,
        Traits.ToArray(),
        Arts.ToArray(),
        Spells.ToArray());
}
