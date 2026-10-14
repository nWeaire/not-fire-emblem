using System;
using System.Collections.Generic;
using NotFireEmblem.Units;

namespace NotFireEmblem.Grid;

public readonly record struct Cell(TerrainDef Terrain, int Height);

/// <summary>The battle map terrain and heights. Occupancy lives in BattleState; terrain changes only via map events.</summary>
public sealed class BattleGrid
{
    public const int MaxHeight = 9;

    private readonly Cell[,] _cells;

    public int Width { get; }
    public int Height { get; }

    public BattleGrid(Cell[,] cells)
    {
        _cells = cells;
        Width = cells.GetLength(0);
        Height = cells.GetLength(1);
    }

    public Cell this[GridPos p] => _cells[p.X, p.Y];

    /// <summary>Terrain only changes through map events (e.g. burning houses clearing).</summary>
    public void SetTerrain(GridPos p, TerrainDef terrain) => _cells[p.X, p.Y] = _cells[p.X, p.Y] with { Terrain = terrain };

    public bool InBounds(GridPos p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

    public IEnumerable<GridPos> AllPositions()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                yield return new GridPos(x, y);
    }

    /// <summary>
    /// Builds a grid from text rows: one terrain symbol per cell, and optionally one height digit (0–9) per cell.
    /// An empty <paramref name="heightRows"/> means a flat map.
    /// </summary>
    public static BattleGrid FromRows(IReadOnlyList<string> terrainRows, IReadOnlyList<string> heightRows, Func<char, TerrainDef> terrainBySymbol)
    {
        if (terrainRows.Count == 0)
            throw new FormatException("Map has no terrain rows.");
        int width = terrainRows[0].Length;
        int height = terrainRows.Count;
        if (heightRows.Count != 0 && heightRows.Count != height)
            throw new FormatException($"Map has {height} terrain rows but {heightRows.Count} height rows.");

        var cells = new Cell[width, height];
        for (int y = 0; y < height; y++)
        {
            if (terrainRows[y].Length != width)
                throw new FormatException($"Terrain row {y} has length {terrainRows[y].Length}, expected {width}.");
            if (heightRows.Count != 0 && heightRows[y].Length != width)
                throw new FormatException($"Height row {y} has length {heightRows[y].Length}, expected {width}.");
            for (int x = 0; x < width; x++)
            {
                int h = 0;
                if (heightRows.Count != 0)
                {
                    char c = heightRows[y][x];
                    if (c < '0' || c > '9')
                        throw new FormatException($"Height at ({x},{y}) is '{c}', expected a digit 0–9.");
                    h = c - '0';
                }
                cells[x, y] = new Cell(terrainBySymbol(terrainRows[y][x]), h);
            }
        }
        return new BattleGrid(cells);
    }
}
