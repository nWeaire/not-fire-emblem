using System;
using System.Linq;
using Godot;

namespace NotFireEmblem.Units;

/// <summary>A combat art: a special attack with a weapon type, limited uses per map.</summary>
[GlobalClass]
public partial class ArtDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Name { get; set; } = "";
    [Export] public WeaponType Type { get; set; } = WeaponType.Sword;
    [Export] public int MightMod { get; set; }
    [Export] public int HitMod { get; set; }
    [Export] public int CritMod { get; set; }
    /// <summary>Added to the weapon's max range.</summary>
    [Export] public int RangeBonus { get; set; }
    [Export] public int Uses { get; set; } = 2;
    /// <summary>The target can't counter-attack.</summary>
    [Export] public bool NoCounter { get; set; }
    /// <summary>Movement types or class traits (e.g. "Beast") it deals effective damage against.</summary>
    [Export] public string[] EffectiveAgainst { get; set; } = Array.Empty<string>();

    public ArtDef ToDef() => new(Id, Name, Type, MightMod, HitMod, CritMod, RangeBonus, Uses, NoCounter, EffectiveAgainst.ToArray());
}
