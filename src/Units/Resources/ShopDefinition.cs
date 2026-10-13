using System;
using System.Linq;
using Godot;

namespace NotFireEmblem.Units;

/// <summary>A regional shop. It opens once the story reaches <see cref="OpensAtChapter"/>.</summary>
[GlobalClass]
public partial class ShopDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Name { get; set; } = "";
    /// <summary>Story region this shop belongs to (thornvale, calder, dravenhold, solmere, ashen_reach).</summary>
    [Export] public string Region { get; set; } = "";
    /// <summary>Story order: 0 = prologue, 1 = Ch 1, ...</summary>
    [Export] public int OpensAtChapter { get; set; }
    /// <summary>Weapon and armour ids on sale.</summary>
    [Export] public string[] Items { get; set; } = Array.Empty<string>();

    public ShopDef ToDef() => new(Id, Name, Region, OpensAtChapter, Items.ToArray());
}
