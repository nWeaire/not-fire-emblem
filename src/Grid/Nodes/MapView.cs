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
    /// <summary>
    /// World units per art pixel for everything on the map: the terrain tiles' density (32 px per cell), shared by
    /// props and unit sprites (UnitView.SpritePixelSize) so all the art reads as one set. Size props by their art,
    /// never by scaling, or their pixels stop matching.
    /// </summary>
    public const float ArtPixelSize = 1f / 32f;
    /// <summary>
    /// How far camera-facing sprites (props here, units in UnitView) are pulled along the line to the camera. They
    /// lean back over the cell behind them, so a raised block there would cut through their upper half; the pull
    /// keeps them in front without moving them on screen.
    /// </summary>
    public const float BillboardPull = 1.1f;
    /// <summary>
    /// Units are pulled a little further than props: enough to stay in front of a tree on raised ground one row back
    /// (it sits about 0.2 nearer the camera), but not so far that they draw over the trees in front of them.
    /// </summary>
    public const float UnitPull = 1.5f;
    /// <summary>Terrains whose block sides stay their flat colour (water and bridges aren't earth).</summary>
    private static readonly HashSet<string> NoEarthSides = new() { "water", "bridge" };

    /// <summary>Terrains whose tiles look the same turned any way round, so cells can rotate them to hide repeats.</summary>
    private static readonly HashSet<string> Rotatable = new() { "plain", "forest", "mountain", "water", "fort", "road", "meadow", "square", "shallows", "reeds" };

    private readonly Dictionary<string, StandardMaterial3D> _materials = new();
    private readonly Dictionary<string, List<Texture2D>> _tiles = new();
    private PlaneMesh? _topMesh;
    private string? _artSet;
    /// <summary>Every prop billboard and the ground point it stands on, re-pulled toward the camera as it moves.</summary>
    private readonly List<(Sprite3D Sprite, Vector3 Ground)> _billboards = new();
    private Transform3D _lastCamera;

    public BattleGrid? Grid { get; private set; }

    public void Build(BattleGrid grid, string? artSet = null)
    {
        _artSet = artSet;
        Grid = grid;
        foreach (var child in GetChildren())
            child.QueueFree();
        _billboards.Clear();
        _lastCamera = default;
        _topMesh = new PlaneMesh { Size = new Vector2(GridMetrics.CellSize, GridMetrics.CellSize) };
        if (GridMetrics.Flat && BuildFlat(grid))
            return;

        foreach (var p in grid.AllPositions())
        {
            var cell = grid[p];
            float top = GridMetrics.SurfaceY(grid, p);
            float height = top - BlockBottom;
            var center = new Vector3(p.X * GridMetrics.CellSize, BlockBottom + height / 2f, p.Y * GridMetrics.CellSize);
            bool alt = (p.X + p.Y) % 2 == 0;
            var tiles = Tiles(TileFor(cell.Terrain.Id));

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

    /// <summary>
    /// The tile set a terrain draws with. Farm maps (ArtSet "farm") have no stone: walls are hedgerows on grass,
    /// cottages stand on a dirt yard and squares are packed earth.
    /// </summary>
    private string TileFor(string terrainId) => _artSet == "farm"
        ? terrainId switch { "wall" => "plain", "fort" or "square" => "road", _ => terrainId }
        : terrainId;

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
                    AddBillboard(kind[(int)(Hash(p, 20 + i) % (uint)kind.Count)], top + offset);
                }
                return;
            case "mountain" when Props("peak").Count > 0:
                AddBillboard(Props("peak")[(int)(Hash(p, 30) % (uint)Props("peak").Count)],
                    top + new Vector3(Jitter(1) * 0.15f, 0f, Jitter(2) * 0.15f));
                return;
            case "fort" when Props("cottage").Count > 0:
                AddBillboard(Props("cottage")[(int)(Hash(p, 40) % (uint)Props("cottage").Count)], top);
                return;
            // Small ground decorations, standing up like the trees. They sit toward the cell's corners so the unit
            // in the middle stays clear, and are small enough to read as ground cover.
            case "stilt" when Props("stilt_house").Count > 0:
                AddBillboard(Props("stilt_house")[(int)(Hash(p, 80) % (uint)Props("stilt_house").Count)], top);
                return;
            case "wall" when _artSet == "farm" && ResourceLoader.Exists(HedgeTexture):
                AddHedge(grid, p, top);
                return;
            case "reeds":
                for (int i = 0; i < 2; i++)
                    AddDecor("reed", p, 81 + i, top + Corner(p, 83 + i, 0.33f));
                return;
            case "plain":
            {
                uint roll = Hash(p, 50) % 20;
                if (roll < 3)
                    AddDecor("bush", p, 51, top + Corner(p, 52, 0.36f));
                else if (roll < 11)
                    AddDecor("tuft", p, 53, top + Corner(p, 54, 0.36f));
                if (roll % 4 == 0)
                    AddDecor("tuft", p, 55, top + Corner(p, 56, 0.38f));
                return;
            }
            case "meadow":
                for (int i = 0; i < 2 + (int)(Hash(p, 60) % 2); i++)
                    AddDecor("flowers", p, 61 + i, top + Corner(p, 64 + i, 0.36f));
                return;
            case "field" when Props("wheat").Count > 0:
                // Two rows of wheat across the cell; neighbouring field cells line up into long rows.
                for (int i = 0; i < 2; i++)
                    AddDecor("wheat", p, 70 + i, top + new Vector3(Jitter(70 + i) * 0.1f, 0f, i == 0 ? -0.22f : 0.22f));
                return;
            case "field":
                // A planted 2×2 grid inside the cell (±0.2 keeps each cabbage clear of the cell's edges).
                for (int i = 0; i < 4; i++)
                    AddDecor("crop", p, 70 + i, top + new Vector3(i % 2 == 0 ? -0.2f : 0.2f, 0f, i < 2 ? -0.2f : 0.2f));
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

    /// <summary>
    /// The flat SNES-style map: one plane carrying the painted ground (FlatTerrain), plus a thin collider per cell
    /// for mouse picking. Returns false (and builds nothing) when no flat art is installed.
    /// </summary>
    private bool BuildFlat(BattleGrid grid)
    {
        if (FlatTerrain.Paint(grid, _artSet) is not { } image)
            return false;
        float cs = GridMetrics.CellSize;
        AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(grid.Width * cs, grid.Height * cs) },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoTexture = ImageTexture.CreateFromImage(image),
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
            // Cells are centred on whole numbers, so the map spans -0.5 .. size - 0.5.
            Position = new Vector3((grid.Width - 1) * cs / 2f, 0f, (grid.Height - 1) * cs / 2f),
            Name = "FlatGround",
        });
        foreach (var p in grid.AllPositions())
        {
            var body = new StaticBody3D { Position = new Vector3(p.X * cs, -0.05f, p.Y * cs) };
            body.SetMeta(CellMeta, new Vector2I(p.X, p.Y));
            body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(cs, 0.1f, cs) } });
            AddChild(body);
        }
        return true;
    }

    private const string HedgeTexture = "res://assets/textures/hedge_leaves.png";
    private const float HedgeHeight = 0.5f;
    private const float HedgeHalfWidth = 0.2f;

    /// <summary>
    /// A solid hedge that follows the grid: a block on the cell centre, run out to each neighbouring wall cell, so
    /// hedges join up, wrap round a yard and turn corners (a lone wall cell gets an east–west stretch). Real 3D boxes
    /// in a seamless leaf texture at the map's pixel scale. They are drawn pulled toward the camera like the props
    /// (BillboardPull), so units behind a hedge stay behind it and units in front stay in front.
    /// </summary>
    private void AddHedge(BattleGrid grid, GridPos p, Vector3 top)
    {
        bool IsWall(int dx, int dy)
        {
            var n = new GridPos(p.X + dx, p.Y + dy);
            return grid.InBounds(n) && grid[n].Terrain.Id == "wall";
        }
        bool east = IsWall(1, 0), west = IsWall(-1, 0), south = IsWall(0, 1), north = IsWall(0, -1);
        if (!(east || west || south || north))
            east = west = true;
        float h = HedgeHalfWidth, half = GridMetrics.CellSize / 2f;
        // One box along each axis that has neighbours (or the centre block alone on that axis).
        float x0 = west ? -half : -h, x1 = east ? half : h, z0 = north ? -half : -h, z1 = south ? half : h;
        AddHedgeBox(top, x0, x1, -h, h);
        if (north || south)
            AddHedgeBox(top, -h, h, z0, z1);
    }

    private void AddHedgeBox(Vector3 top, float x0, float x1, float z0, float z1)
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(x1 - x0, HedgeHeight, z1 - z0) },
            MaterialOverride = HedgeMaterial,
            Position = top + new Vector3((x0 + x1) / 2f, HedgeHeight / 2f, (z0 + z1) / 2f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
    }

    private ShaderMaterial? _hedgeMaterial;

    private ShaderMaterial HedgeMaterial => _hedgeMaterial ??= CreateHedgeMaterial();

    private static ShaderMaterial CreateHedgeMaterial()
    {
        var material = new ShaderMaterial
        {
            Shader = new Shader
            {
                Code = """
                    shader_type spatial;
                    uniform sampler2D leaves : source_color, filter_nearest, repeat_enable;
                    uniform float pull = 1.1;
                    uniform float texels_per_unit = 1.0;
                    varying vec3 world_pos;
                    varying vec3 world_normal;
                    void vertex() {
                        world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
                        world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
                        // Pull along the view ray, like the billboards: same place on screen, nearer in depth.
                        vec4 view = MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
                        view.xyz -= normalize(view.xyz) * pull;
                        POSITION = PROJECTION_MATRIX * view;
                    }
                    void fragment() {
                        // World-space (triplanar) UVs: one texture pixel per map art pixel on every face.
                        vec3 n = abs(world_normal);
                        vec2 uv = n.y > 0.5 ? world_pos.xz : (n.x > 0.5 ? world_pos.zy : world_pos.xy);
                        vec3 c = texture(leaves, uv * texels_per_unit).rgb;
                        // The top catches the light; the sides are a shade darker.
                        ALBEDO = c * (n.y > 0.5 ? 1.15 : 0.85);
                        ROUGHNESS = 1.0;
                    }
                    """,
            },
        };
        material.SetShaderParameter("leaves", ResourceLoader.Load<Texture2D>(HedgeTexture));
        material.SetShaderParameter("pull", BillboardPull);
        // The texture is 32 px, one cell's worth of art pixels: one repeat per world unit.
        material.SetShaderParameter("texels_per_unit", 1f / GridMetrics.CellSize);
        return material;
    }

    /// <summary>An upright pixel-art prop standing on <paramref name="ground"/>, facing the camera like the units.</summary>
    private void AddBillboard(Texture2D texture, Vector3 ground)
    {
        var sprite = new Sprite3D
        {
            Texture = texture,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, // like the units: FixedY squashed props under the high camera
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            Shaded = false,
            PixelSize = ArtPixelSize,
            Offset = new Vector2(0f, texture.GetHeight() / 2f), // the art is cropped, so this stands it on the ground
            Position = ground,
            // Pulled toward the camera (see _Process), its shadow would land away from its base.
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(sprite);
        _billboards.Add((sprite, ground));
    }

    public override void _Process(double delta)
    {
        // Pull every prop toward the camera (BillboardPull) so raised terrain behind it can't cut through it.
        // Only when the camera has moved: there can be a few hundred props.
        var camera = GetViewport()?.GetCamera3D();
        if (camera is null || camera.GlobalTransform.IsEqualApprox(_lastCamera))
            return;
        _lastCamera = camera.GlobalTransform;
        var eye = ToLocal(camera.GlobalPosition);
        foreach (var (sprite, ground) in _billboards)
            sprite.Position = ground + (eye - ground).Normalized() * BillboardPull;
    }

    /// <summary>One variant of a decoration prop (picked by the cell hash), if that prop has art.</summary>
    private void AddDecor(string id, GridPos p, int salt, Vector3 ground)
    {
        var art = Props(id);
        if (art.Count > 0)
            AddBillboard(art[(int)(Hash(p, salt) % (uint)art.Count)], ground);
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
