using System.Collections.Generic;
using Godot;

namespace NotFireEmblem.Grid;

/// <summary>
/// Builds the visible terrain procedurally from a BattleGrid: one block per cell stepped by height, a pixel-art
/// tile on each block's top face (from assets/textures/terrain/&lt;terrainId&gt;[_N].png), simple props per terrain
/// type, and one collider per cell for mouse picking. Terrains without a tile fall back to their flat colour.
/// </summary>
public partial class MapView : Node3D
{
    public const string CellMeta = "cell";
    public const string TerrainTextureRoot = "res://assets/textures/terrain";
    private const float BlockBottom = -1.5f;

    /// <summary>Terrains whose tiles look the same turned any way round, so cells can rotate them to hide repeats.</summary>
    private static readonly HashSet<string> Rotatable = new() { "plain", "forest", "mountain", "water", "fort", "road", "meadow", "square" };

    private readonly Dictionary<string, StandardMaterial3D> _materials = new();
    private readonly Dictionary<string, List<Texture2D>> _tiles = new();
    private PlaneMesh? _topMesh;

    public BattleGrid? Grid { get; private set; }

    public void Build(BattleGrid grid)
    {
        Grid = grid;
        foreach (var child in GetChildren())
            child.QueueFree();
        _topMesh = new PlaneMesh { Size = new Vector2(GridMetrics.CellSize, GridMetrics.CellSize) };

        foreach (var p in grid.AllPositions())
        {
            var cell = grid[p];
            float top = GridMetrics.SurfaceY(grid, p);
            float height = top - BlockBottom;
            var center = new Vector3(p.X * GridMetrics.CellSize, BlockBottom + height / 2f, p.Y * GridMetrics.CellSize);
            bool alt = (p.X + p.Y) % 2 == 0;
            var tiles = Tiles(cell.Terrain.Id);

            var block = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(GridMetrics.CellSize, height, GridMetrics.CellSize) },
                // With a tile on top, the block only shows its sides: darker, so the top face reads clearly.
                MaterialOverride = tiles.Count > 0
                    ? Material(cell.Terrain.ColorHex, -0.35f)
                    : Material(cell.Terrain.ColorHex, alt ? 0.04f : 0f),
                Position = center,
                Name = $"Cell_{p.X}_{p.Y}",
            };
            AddChild(block);
            if (tiles.Count > 0)
                AddTopTile(grid, p, cell.Terrain.Id, tiles, new Vector3(center.X, top, center.Z), alt);

            var body = new StaticBody3D { Position = center };
            body.SetMeta(CellMeta, new Vector2I(p.X, p.Y));
            body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(GridMetrics.CellSize, height, GridMetrics.CellSize) } });
            AddChild(body);

            AddProps(grid, cell.Terrain.Id, new Vector3(center.X, top, center.Z), p);
        }
    }

    private void AddTopTile(BattleGrid grid, GridPos p, string terrainId, List<Texture2D> tiles, Vector3 top, bool alt)
    {
        // The first tile is the main look; alternates appear on about one cell in five.
        uint h = Hash(p, 1);
        int variant = tiles.Count > 1 && h % 5 == 0 ? 1 + (int)(h / 5 % (uint)(tiles.Count - 1)) : 0;
        float yaw = Rotatable.Contains(terrainId) ? Hash(p, 2) % 4 * Mathf.Pi / 2f
            : terrainId == "bridge" && SpansEastWest(grid, p) ? Mathf.Pi / 2f
            : 0f;
        AddChild(new MeshInstance3D
        {
            Mesh = _topMesh,
            // A faint checkerboard keeps individual cells readable.
            MaterialOverride = TileMaterial(tiles[variant], alt ? 1f : 0.95f),
            Position = top + new Vector3(0f, 0.002f, 0f),
            Rotation = new Vector3(0f, yaw, 0f),
        });
    }

    /// <summary>The bridge tile's planks run north–south; turn it when the bridge crosses water to its north and south.</summary>
    private static bool SpansEastWest(BattleGrid grid, GridPos p)
    {
        bool IsWater(int dx, int dy)
        {
            var n = new GridPos(p.X + dx, p.Y + dy);
            return grid.InBounds(n) && grid[n].Terrain.Id == "water";
        }
        return IsWater(0, -1) || IsWater(0, 1);
    }

    private void AddProps(BattleGrid grid, string terrainId, Vector3 top, GridPos p)
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
                bool eastWest = SpansEastWest(grid, p);
                for (int i = -1; i <= 1; i += 2)
                {
                    AddChild(new MeshInstance3D
                    {
                        Mesh = new BoxMesh { Size = eastWest ? new Vector3(1f, 0.15f, 0.06f) : new Vector3(0.06f, 0.15f, 1f) },
                        MaterialOverride = Material("#5a3d22", 0f),
                        Position = top + (eastWest ? new Vector3(0f, 0.07f, i * 0.45f) : new Vector3(i * 0.45f, 0.07f, 0f)),
                    });
                }
                break;
        }
    }

    /// <summary>The terrain's tiles: &lt;id&gt;.png, then &lt;id&gt;_1.png, &lt;id&gt;_2.png… while they exist.</summary>
    private List<Texture2D> Tiles(string terrainId)
    {
        if (_tiles.TryGetValue(terrainId, out var cached))
            return cached;
        var tiles = new List<Texture2D>();
        for (int i = 0; ; i++)
        {
            string path = i == 0 ? $"{TerrainTextureRoot}/{terrainId}.png" : $"{TerrainTextureRoot}/{terrainId}_{i}.png";
            if (!ResourceLoader.Exists(path))
                break;
            tiles.Add(ResourceLoader.Load<Texture2D>(path));
        }
        _tiles[terrainId] = tiles;
        return tiles;
    }

    private static uint Hash(GridPos p, int salt) =>
        (uint)((p.X * 73856093) ^ (p.Y * 19349663) ^ (salt * 83492791)) * 2654435761u >> 8;

    private StandardMaterial3D TileMaterial(Texture2D texture, float shade)
    {
        string key = $"tile:{texture.ResourcePath}:{shade}";
        if (!_materials.TryGetValue(key, out var material))
        {
            material = new StandardMaterial3D
            {
                AlbedoTexture = texture,
                AlbedoColor = new Color(shade, shade, shade),
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                Roughness = 0.95f,
            };
            _materials[key] = material;
        }
        return material;
    }

    private StandardMaterial3D Material(string hex, float lighten)
    {
        string key = $"{hex}:{lighten}";
        if (!_materials.TryGetValue(key, out var material))
        {
            var color = Color.FromHtml(hex);
            material = new StandardMaterial3D { AlbedoColor = lighten < 0f ? color.Darkened(-lighten) : color.Lightened(lighten), Roughness = 0.9f };
            _materials[key] = material;
        }
        return material;
    }
}
