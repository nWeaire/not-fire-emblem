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
    /// <summary>rout | boss KEY | seize X,Y | survive N | escape CELLS [KEY...]  (see MapScript).</summary>
    [Export] public string Victory { get; set; } = "rout";
    /// <summary>Extra loss conditions: "die KEY", "protect TAG N", "turns N". Losing the lord always loses.</summary>
    [Export] public string[] Defeat { get; set; } = Array.Empty<string>();
    /// <summary>Scripted events, one per line: "CONDITION => EFFECT; EFFECT" (see MapScript).</summary>
    [Export] public string[] Triggers { get; set; } = Array.Empty<string>();
    [Export] public bool Fog { get; set; }
    [Export] public int VisionBonus { get; set; }
    /// <summary>Cells for deployed units without a fixed spawn, e.g. "1,10 2,10 1,11".</summary>
    [Export] public string DeploySlots { get; set; } = "";
    /// <summary>Battle-screen backdrop for open ground on this map (forest and water keep their own), e.g. "greyfen".</summary>
    [Export] public string Background { get; set; } = "";
    /// <summary>Flat-map art variant: "farm" draws walls as hedges and houses as farmhouses.</summary>
    [Export] public string ArtSet { get; set; } = "";
    /// <summary>"night" tints the map and the battle screen for a night battle.</summary>
    [Export] public string Lighting { get; set; } = "";

    public MapDef ToDef()
    {
        try
        {
            return new MapDef(Id, Name, TerrainRows.ToArray(), HeightRows.ToArray(), Spawns.Select(s => s.ToDef()).ToArray(),
                MapScript.ParseObjectives(Victory, Defeat), MapScript.ParseTriggers(Triggers), Fog, VisionBonus,
                string.IsNullOrWhiteSpace(DeploySlots) ? null : MapScript.Cells(DeploySlots.Split(' ', StringSplitOptions.RemoveEmptyEntries), DeploySlots),
                string.IsNullOrWhiteSpace(Background) ? null : Background,
                string.IsNullOrWhiteSpace(ArtSet) ? null : ArtSet,
                string.IsNullOrWhiteSpace(Lighting) ? null : Lighting);
        }
        catch (FormatException e)
        {
            throw new FormatException($"Map '{Id}': {e.Message}");
        }
    }
}
