using System;
using System.Linq;
using Godot;

namespace NotFireEmblem.Battle;

/// <summary>
/// A battle map as text rows: one terrain symbol per cell (see TerrainDefinition.Symbol) and one height digit (0–9) per cell.
/// </summary>
[GlobalClass]
public partial class MapDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Name { get; set; } = "";
    [Export] public string[] TerrainRows { get; set; } = Array.Empty<string>();
    [Export] public string[] HeightRows { get; set; } = Array.Empty<string>();
    [Export] public Godot.Collections.Array<SpawnDefinition> Spawns { get; set; } = new();

    public MapDef ToDef() => new(Id, Name, TerrainRows.ToArray(), HeightRows.ToArray(), Spawns.Select(s => s.ToDef()).ToArray());
}
