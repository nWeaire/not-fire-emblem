using Godot;

namespace NotFireEmblem.Grid;

/// <summary>Converts between grid cells and world space. Grid X → world +X, grid Y → world +Z.</summary>
public static class GridMetrics
{
    public const float CellSize = 1f;
    public const float StepHeight = 0.5f;
    public const float WaterDrop = 0.2f;

    public static float TopY(int height) => height * StepHeight;

    /// <summary>World Y of the walkable surface of a cell.</summary>
    public static float SurfaceY(BattleGrid grid, GridPos p)
    {
        var cell = grid[p];
        return TopY(cell.Height) - (cell.Terrain.Id == "water" ? WaterDrop : 0f);
    }

    public static Vector3 CellCenter(BattleGrid grid, GridPos p) => new(p.X * CellSize, SurfaceY(grid, p), p.Y * CellSize);

    public static Vector3 MapCenter(BattleGrid grid) => new((grid.Width - 1) * CellSize / 2f, 0f, (grid.Height - 1) * CellSize / 2f);
}
