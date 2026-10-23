using System.Collections.Generic;
using Godot;

namespace NotFireEmblem.Grid;

/// <summary>
/// One layer of fog of war (FogLooks): a single plane over the whole map that samples a one-pixel-per-cell mask with
/// linear filtering, so the fog's edge fades smoothly across a cell instead of stepping cell by cell.
/// </summary>
public partial class FogPlane : MeshInstance3D
{
    private Image _mask = null!;
    private ImageTexture _texture = null!;
    private BattleGrid _grid = null!;

    public void Setup(BattleGrid grid, FogLooks.Layer layer)
    {
        _grid = grid;
        Mesh = new PlaneMesh { Size = new Vector2(grid.Width, grid.Height) * GridMetrics.CellSize };
        Position = GridMetrics.MapCenter(grid) + new Vector3(0f, layer.Lift, 0f);
        CastShadow = ShadowCastingSetting.Off;
        _mask = Image.CreateEmpty(grid.Width, grid.Height, false, Image.Format.L8);
        _texture = ImageTexture.CreateFromImage(_mask);
        var material = FogLooks.Material(layer);
        material.SetShaderParameter("mask", _texture);
        material.SetShaderParameter("grid_size", new Vector2(grid.Width, grid.Height));
        MaterialOverride = material;
    }

    /// <summary>Fogs these cells and clears every other.</summary>
    public void ShowCells(IReadOnlyCollection<GridPos> hidden)
    {
        _mask.Fill(Colors.Black);
        foreach (var p in hidden)
            _mask.SetPixel(p.X, p.Y, Colors.White);
        _texture.Update(_mask);
    }
}
