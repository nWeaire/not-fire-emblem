using System;
using System.Collections.Generic;

namespace NotFireEmblem.Grid;

/// <summary>A cell coordinate. X grows to the right (world +X), Y grows down the map (world +Z).</summary>
public readonly record struct GridPos(int X, int Y)
{
    public static readonly GridPos Up = new(0, -1);
    public static readonly GridPos Right = new(1, 0);
    public static readonly GridPos Down = new(0, 1);
    public static readonly GridPos Left = new(-1, 0);

    /// <summary>The four orthogonal directions, clockwise from Up.</summary>
    public static readonly GridPos[] Directions = { Up, Right, Down, Left };

    public IEnumerable<GridPos> Neighbors()
    {
        foreach (var d in Directions)
            yield return this + d;
    }

    public int DistanceTo(GridPos other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    public static GridPos operator +(GridPos a, GridPos b) => new(a.X + b.X, a.Y + b.Y);
    public static GridPos operator -(GridPos a, GridPos b) => new(a.X - b.X, a.Y - b.Y);

    public override string ToString() => $"({X},{Y})";
}
