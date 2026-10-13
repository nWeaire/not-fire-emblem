using System.Collections.Generic;
using Godot;

namespace NotFireEmblem.Grid;

/// <summary>
/// Builds the visible terrain procedurally from a BattleGrid: one block per cell stepped by height,
/// simple props per terrain type, and one collider per cell for mouse picking.
/// </summary>
public partial class MapView : Node3D
{
    public const string CellMeta = "cell";
    private const float BlockBottom = -1.5f;

    private readonly Dictionary<string, StandardMaterial3D> _materials = new();

    public BattleGrid? Grid { get; private set; }

    public void Build(BattleGrid grid)
    {
        Grid = grid;
        foreach (var child in GetChildren())
            child.QueueFree();

        foreach (var p in grid.AllPositions())
        {
            var cell = grid[p];
            float top = GridMetrics.SurfaceY(grid, p);
            float height = top - BlockBottom;
            var center = new Vector3(p.X * GridMetrics.CellSize, BlockBottom + height / 2f, p.Y * GridMetrics.CellSize);
            bool alt = (p.X + p.Y) % 2 == 0;

            var block = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(GridMetrics.CellSize, height, GridMetrics.CellSize) },
                MaterialOverride = Material(cell.Terrain.ColorHex, alt ? 0.04f : 0f),
                Position = center,
                Name = $"Cell_{p.X}_{p.Y}",
            };
            AddChild(block);

            var body = new StaticBody3D { Position = center };
            body.SetMeta(CellMeta, new Vector2I(p.X, p.Y));
            body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(GridMetrics.CellSize, height, GridMetrics.CellSize) } });
            AddChild(body);

            AddProps(cell.Terrain.Id, new Vector3(center.X, top, center.Z), p);
        }
    }

    private void AddProps(string terrainId, Vector3 top, GridPos p)
    {
        // Small deterministic jitter so repeated props don't look stamped.
        float Jitter(int salt) => (((p.X * 73856093) ^ (p.Y * 19349663) ^ (salt * 83492791)) & 0xff) / 255f - 0.5f;

        switch (terrainId)
        {
            case "forest":
                for (int i = 0; i < 2; i++)
                {
                    var offset = new Vector3(Jitter(i) * 0.5f, 0f, Jitter(i + 7) * 0.5f);
                    AddChild(new MeshInstance3D
                    {
                        Mesh = new CylinderMesh { TopRadius = 0f, BottomRadius = 0.22f, Height = 0.7f },
                        MaterialOverride = Material("#2a5a2a", 0f),
                        Position = top + offset + new Vector3(0f, 0.45f, 0f),
                    });
                    AddChild(new MeshInstance3D
                    {
                        Mesh = new CylinderMesh { TopRadius = 0.04f, BottomRadius = 0.05f, Height = 0.15f },
                        MaterialOverride = Material("#5a3d22", 0f),
                        Position = top + offset + new Vector3(0f, 0.07f, 0f),
                    });
                }
                break;
            case "mountain":
                AddChild(new MeshInstance3D
                {
                    Mesh = new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.4f, Height = 0.45f, RadialSegments = 6 },
                    MaterialOverride = Material("#7a6c55", 0f),
                    Position = top + new Vector3(Jitter(1) * 0.2f, 0.22f, Jitter(2) * 0.2f),
                });
                break;
            case "fort":
                AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh { Size = new Vector3(0.5f, 0.3f, 0.5f) },
                    MaterialOverride = Material("#8a5f35", 0f),
                    Position = top + new Vector3(0f, 0.15f, 0f),
                });
                AddChild(new MeshInstance3D
                {
                    Mesh = new PrismMesh { Size = new Vector3(0.6f, 0.25f, 0.55f) },
                    MaterialOverride = Material("#a33a2a", 0f),
                    Position = top + new Vector3(0f, 0.42f, 0f),
                });
                break;
            case "bridge":
                for (int i = -1; i <= 1; i += 2)
                {
                    AddChild(new MeshInstance3D
                    {
                        Mesh = new BoxMesh { Size = new Vector3(0.06f, 0.15f, 1f) },
                        MaterialOverride = Material("#5a3d22", 0f),
                        Position = top + new Vector3(i * 0.45f, 0.07f, 0f),
                    });
                }
                break;
        }
    }

    private StandardMaterial3D Material(string hex, float lighten)
    {
        string key = $"{hex}:{lighten}";
        if (!_materials.TryGetValue(key, out var material))
        {
            material = new StandardMaterial3D { AlbedoColor = Color.FromHtml(hex).Lightened(lighten), Roughness = 0.9f };
            _materials[key] = material;
        }
        return material;
    }
}
