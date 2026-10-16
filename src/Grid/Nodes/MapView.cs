using System.Collections.Generic;
using Godot;

namespace NotFireEmblem.Grid;

/// <summary>
/// Builds the visible terrain procedurally from a BattleGrid: one block per cell stepped by height, a pixel-art
/// tile on each block's top face (from assets/textures/terrain/&lt;terrainId&gt;[_N].png), pixel-art billboard props
/// (trees, peaks, cottages from assets/sprites/props/), earth-and-stone block sides, and one collider per cell for
/// mouse picking. Terrains without a tile fall back to their flat colour; props without art fall back to shapes.
/// </summary>
public partial class MapView : Node3D
{
    public const string CellMeta = "cell";
    public const string TerrainTextureRoot = "res://assets/textures/terrain";
    private const float BlockBottom = -1.5f;
    public const string PropRoot = "res://assets/sprites/props";
    public const string SideTexture = "res://assets/textures/cliff_side.png";
    /// <summary>World units per prop pixel: a little finer than units (0.025) so a tree stays smaller than a fighter.</summary>
    private const float PropPixelSize = 0.019f;
    /// <summary>Terrains whose block sides stay their flat colour (water and bridges aren't earth).</summary>
    private static readonly HashSet<string> NoEarthSides = new() { "water", "bridge" };

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
                    ? SideMaterial(cell.Terrain.Id, cell.Terrain.ColorHex)
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

        // Pixel-art props when they exist; the primitive shapes below are the fallback.
        switch (terrainId)
        {
            case "forest" when Props("pine").Count + Props("oak").Count > 0:
                for (int i = 0; i < 2; i++)
                {
                    var kind = Props(Hash(p, 10 + i) % 3 == 0 ? "oak" : "pine");
                    if (kind.Count == 0)
                        kind = Props("pine").Count > 0 ? Props("pine") : Props("oak");
                    var offset = new Vector3(Jitter(i) * 0.5f, 0f, Jitter(i + 7) * 0.5f);
                    AddBillboard(kind[(int)(Hash(p, 20 + i) % (uint)kind.Count)], top + offset, i == 0 ? 1f : 0.8f);
                }
                return;
            case "mountain" when Props("peak").Count > 0:
                AddBillboard(Props("peak")[(int)(Hash(p, 30) % (uint)Props("peak").Count)],
                    top + new Vector3(Jitter(1) * 0.15f, 0f, Jitter(2) * 0.15f), 1.1f);
                return;
            case "fort" when Props("cottage").Count > 0:
                AddBillboard(Props("cottage")[(int)(Hash(p, 40) % (uint)Props("cottage").Count)], top, 1f);
                return;
            // Small ground decorations, standing up like the trees. They sit toward the cell's corners so the unit
            // in the middle stays clear, and are small enough to read as ground cover.
            case "plain":
            {
                uint roll = Hash(p, 50) % 20;
                if (roll < 3)
                    AddDecor("bush", p, 51, top + Corner(p, 52, 0.3f), 0.75f);
                else if (roll < 11)
                    AddDecor("tuft", p, 53, top + Corner(p, 54, 0.3f), 0.55f);
                if (roll % 4 == 0)
                    AddDecor("tuft", p, 55, top + Corner(p, 56, 0.33f), 0.45f);
                return;
            }
            case "meadow":
                for (int i = 0; i < 2 + (int)(Hash(p, 60) % 2); i++)
                    AddDecor("flowers", p, 61 + i, top + Corner(p, 64 + i, 0.3f), 0.55f);
                return;
            case "field":
                // A planted 2×2 grid; the crops line up with the furrows on the field tile.
                for (int i = 0; i < 4; i++)
                    AddDecor("crop", p, 70 + i, top + new Vector3(i % 2 == 0 ? -0.24f : 0.24f, 0f, i < 2 ? -0.24f : 0.24f), 0.55f);
                return;
        }
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

    /// <summary>An upright pixel-art prop standing on <paramref name="ground"/>, facing the camera like the units.</summary>
    private void AddBillboard(Texture2D texture, Vector3 ground, float scale)
    {
        AddChild(new Sprite3D
        {
            Texture = texture,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, // like the units: FixedY squashed props under the high camera
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            Shaded = false,
            PixelSize = PropPixelSize * scale,
            Offset = new Vector2(0f, texture.GetHeight() / 2f), // the art is cropped, so this stands it on the ground
            Position = ground,
        });
    }

    /// <summary>One variant of a decoration prop (picked by the cell hash), if that prop has art.</summary>
    private void AddDecor(string id, GridPos p, int salt, Vector3 ground, float scale)
    {
        var art = Props(id);
        if (art.Count > 0)
            AddBillboard(art[(int)(Hash(p, salt) % (uint)art.Count)], ground, scale);
    }

    /// <summary>A point toward one of the cell's four corners (chosen by the hash), jittered a little.</summary>
    private static Vector3 Corner(GridPos p, int salt, float reach)
    {
        uint h = Hash(p, salt);
        float x = (h & 1) == 0 ? -reach : reach;
        float z = (h & 2) == 0 ? -reach : reach;
        float jx = (h >> 4 & 0xff) / 255f * 0.1f - 0.05f, jz = (h >> 12 & 0xff) / 255f * 0.1f - 0.05f;
        return new Vector3(x + jx, 0f, z + jz);
    }

    private readonly Dictionary<string, List<Texture2D>> _props = new();

    /// <summary>The prop's art: &lt;id&gt;_0.png, &lt;id&gt;_1.png… while they exist.</summary>
    private List<Texture2D> Props(string id)
    {
        if (_props.TryGetValue(id, out var cached))
            return cached;
        var list = new List<Texture2D>();
        for (int i = 0; ResourceLoader.Exists($"{PropRoot}/{id}_{i}.png"); i++)
            list.Add(ResourceLoader.Load<Texture2D>($"{PropRoot}/{id}_{i}.png"));
        _props[id] = list;
        return list;
    }

    /// <summary>Block sides: the earth-and-stone texture wrapped round the block (triplanar), shaded to read as sides.</summary>
    private StandardMaterial3D SideMaterial(string terrainId, string colorHex)
    {
        if (NoEarthSides.Contains(terrainId) || !ResourceLoader.Exists(SideTexture))
            return Material(colorHex, -0.35f);
        if (!_materials.TryGetValue("side", out var material))
        {
            material = new StandardMaterial3D
            {
                AlbedoTexture = ResourceLoader.Load<Texture2D>(SideTexture),
                AlbedoColor = new Color(0.72f, 0.72f, 0.72f),
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                Uv1Triplanar = true,
                Uv1Scale = Vector3.One,
                Roughness = 0.95f,
            };
            _materials["side"] = material;
        }
        return material;
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
