using Godot;

namespace NotFireEmblem.Grid;

/// <summary>Converts between grid cells and world space. Grid X → world +X, grid Y → world +Z.</summary>
public static class GridMetrics
{
    public const float CellSize = 1f;
    public const float StepHeight = 0.5f;
    public const float WaterDrop = 0.2f;

    /// <summary>
    /// The flat 2D map experiment (session 5, parked): seen straight down, every cell on one plane, ground painted
    /// by FlatTerrain. Off: the game uses the 2.5D blocks. Its art is parked in tools/art/takes/flat_parked/
    /// (move it back under assets/ to try it again).
    /// </summary>
    public static bool Flat = false;

    public static float TopY(int height) => height * StepHeight;

    /// <summary>World Y of the walkable surface of a cell.</summary>
    public static float SurfaceY(BattleGrid grid, GridPos p)
    {
        if (Flat)
            return 0f;
        var cell = grid[p];
        return TopY(cell.Height) - (cell.Terrain.Id == "water" ? WaterDrop : 0f);
    }

    public static Vector3 CellCenter(BattleGrid grid, GridPos p) => new(p.X * CellSize, SurfaceY(grid, p), p.Y * CellSize);

    public static Vector3 MapCenter(BattleGrid grid) => new((grid.Width - 1) * CellSize / 2f, 0f, (grid.Height - 1) * CellSize / 2f);
}
