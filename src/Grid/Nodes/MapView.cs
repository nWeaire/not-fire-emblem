using System.Collections.Generic;
using System.Linq;
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
    private static readonly HashSet<string> Rotatable = new() { "plain", "forest", "mountain", "fort", "road", "meadow", "square", "shallows", "reeds" };

    private readonly Dictionary<string, StandardMaterial3D> _materials = new();
    private readonly Dictionary<string, List<Texture2D>> _tiles = new();
    private PlaneMesh? _topMesh;
    private string? _artSet;
    /// <summary>Every prop billboard and the ground point it stands on, re-pulled toward the camera as it moves.</summary>
    private readonly List<(Sprite3D Sprite, Vector3 Ground)> _billboards = new();

    /// <summary>Water cells whose body of water is taller than it is wide (a north–south river): ripples run along it.</summary>
    private readonly HashSet<GridPos> _verticalWater = new();
    private Transform3D _lastCamera;

    public BattleGrid? Grid { get; private set; }

    /// <summary>
    /// When set, props round this cursor's cell and between it and the camera turn see-through, so the tile being
    /// picked stays visible under trees (session 9: Ch 3's forest hid the cursor).
    /// </summary>
    public GridCursor? Cursor { get; set; }

    private const float FadeAlpha = 0.35f;
    private const float FadeRadius = 1.3f;     // cells round the cursor
    private const float FadeTowardCamera = 3f; // cells in front of it, toward the camera
    private const float FadeHalfWidth = 1.2f;  // half the width of that strip
    private readonly List<Sprite3D> _faded = new();
    private GridPos? _fadedFor;

    public void Build(BattleGrid grid, string? artSet = null)
    {
        _artSet = artSet;
        Grid = grid;
        foreach (var child in GetChildren())
            child.QueueFree();
        _billboards.Clear();
        _faded.Clear();
        _fadedFor = null;
        FindVerticalWater(grid);
        _lastCamera = default;
        _topMesh = new PlaneMesh { Size = new Vector2(GridMetrics.CellSize, GridMetrics.CellSize) };

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
                    ? SideMaterial(cell.Terrain.Id, cell.Terrain.ColorHex, p)
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
            if (cell.Terrain.Id == "wall" && _artSet != "farm")
                AddWallDecor(grid, p, new Vector3(center.X, top, center.Z));
            if (cell.Terrain.Id == "bridge" && grid[p].Height > 0)
                AddBulwarks(grid, p, new Vector3(center.X, top, center.Z));
        }
        AddMasts(grid);
    }

    /// <summary>
    /// A mast (prop "mast") in the middle of each ship: every group of at least nine joined raised bridge cells
    /// (Ch 12's decks; gangplanks are too small to count). Looks only.
    /// </summary>
    private void AddMasts(BattleGrid grid)
    {
        var masts = Props("mast");
        if (masts.Count == 0)
            return;
        var seen = new HashSet<GridPos>();
        foreach (var start in grid.AllPositions())
        {
            if (grid[start].Terrain.Id != "bridge" || grid[start].Height == 0 || !seen.Add(start))
                continue;
            var deck = new List<GridPos> { start };
            for (int i = 0; i < deck.Count; i++)
                foreach (var n in deck[i].Neighbors())
                    if (grid.InBounds(n) && grid[n].Terrain.Id == "bridge" && grid[n].Height > 0 && seen.Add(n))
                        deck.Add(n);
            if (deck.Count < 9)
                continue;
            float cx = (float)deck.Average(c => c.X), cy = (float)deck.Average(c => c.Y);
            var middle = deck.OrderBy(c => (c.X - cx) * (c.X - cx) + (c.Y - cy) * (c.Y - cy)).ThenBy(c => c.Y).ThenBy(c => c.X).First();
            var top = new Vector3(middle.X * GridMetrics.CellSize, GridMetrics.SurfaceY(grid, middle), middle.Y * GridMetrics.CellSize);
            AddBillboard(masts[(int)(Hash(middle, 150) % (uint)masts.Count)], top);
        }
    }

    private void AddTopTile(BattleGrid grid, GridPos p, string terrainId, List<Texture2D> tiles, Vector3 top, bool alt)
    {
        // The first tile is the main look; alternates appear on about one cell in five. Bridges always use the main
        // tile, so a bridge's cells match.
        uint h = Hash(p, 1);
        int variant = tiles.Count > 1 && h % 5 == 0 && terrainId != "bridge" ? 1 + (int)(h / 5 % (uint)(tiles.Count - 1)) : 0;
        float yaw = Rotatable.Contains(terrainId) ? Hash(p, 2) % 4 * Mathf.Pi / 2f
            : terrainId == "water" && _verticalWater.Contains(p) ? Mathf.Pi / 2f
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
    /// cottages stand on a dirt yard and squares are packed earth. Other art sets use their own "&lt;set&gt;_&lt;tile&gt;"
    /// tiles where they exist (session 17: "snow" for Ch 14's citadel), else the usual ones.
    /// </summary>
    private string TileFor(string terrainId)
    {
        string tile = _artSet == "farm"
            ? terrainId switch { "wall" => "plain", "fort" or "square" => "road", _ => terrainId }
            : terrainId is "throne" or "barricade" or "rubble" or "gate" or "door" or "chest" ? "square" : terrainId;
        return _artSet is { } set && set != "farm" && Tiles($"{set}_{tile}").Count > 0 ? $"{set}_{tile}" : tile;
    }

    /// <summary>
    /// Water is never turned at random (session 9: odd vertical tiles in a horizontal river). Each connected body of
    /// water takes one direction from its extent: wider than tall (ponds, Ch 2's river) keeps the tile's horizontal
    /// ripples, taller than wide (Ch 1's river, Ch 6's channel) turns them to run north–south.
    /// </summary>
    private void FindVerticalWater(BattleGrid grid)
    {
        _verticalWater.Clear();
        var seen = new HashSet<GridPos>();
        foreach (var start in grid.AllPositions())
        {
            if (grid[start].Terrain.Id != "water" || !seen.Add(start))
                continue;
            var body = new List<GridPos> { start };
            for (int i = 0; i < body.Count; i++)
            {
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    var n = new GridPos(body[i].X + dx, body[i].Y + dy);
                    if (grid.InBounds(n) && grid[n].Terrain.Id == "water" && seen.Add(n))
                        body.Add(n);
                }
            }
            int width = body.Max(c => c.X) - body.Min(c => c.X) + 1, height = body.Max(c => c.Y) - body.Min(c => c.Y) + 1;
            if (height > width)
                _verticalWater.UnionWith(body);
        }
    }

    /// <summary>The bridge tile is drawn for a north–south walkway; true when this bridge runs east–west instead.</summary>
    private static bool SpansEastWest(BattleGrid grid, GridPos p)
    {
        bool Is(int dx, int dy, string a, string b)
        {
            var n = new GridPos(p.X + dx, p.Y + dy);
            return grid.InBounds(n) && grid[n].Terrain.Id is var id && (id == a || id == b);
        }
        // A bridge crosses the water: it runs east–west when water (or shallows) lies north or south of it and not
        // east or west. Otherwise (both or neither) it follows the bridge cells beside it, so a long bridge whose
        // ends touch reeds or banks still turns as one piece (session 10: Ch 6's bridges were half turned).
        bool wetNorthSouth = Is(0, -1, "water", "shallows") || Is(0, 1, "water", "shallows");
        bool wetEastWest = Is(-1, 0, "water", "shallows") || Is(1, 0, "water", "shallows");
        if (wetNorthSouth != wetEastWest)
            return wetNorthSouth;
        return Is(-1, 0, "bridge", "bridge") || Is(1, 0, "bridge", "bridge");
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
                    // A themed map with its own pines but no oaks (snow) plants pines only.
                    bool pinesOnly = Themed && LoadProps($"{_artSet}_pine").Count > 0 && LoadProps($"{_artSet}_oak").Count == 0;
                    var kind = Props(!pinesOnly && Hash(p, 10 + i) % 3 == 0 ? "oak" : "pine");
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
            case "barricade" when SiegeProps().Count > 0:
                // Wagons, stalls, crates, barrels and carts, picked per cell.
                AddBillboard(SiegeProps()[(int)(Hash(p, 90) % (uint)SiegeProps().Count)], top);
                return;
            case "rubble" when Props("rubble").Count > 0:
                AddBillboard(Props("rubble")[(int)(Hash(p, 95) % (uint)Props("rubble").Count)],
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
            case "gate" or "door" when ResourceLoader.Exists($"{GateRoot}/leaf.png"):
                AddGate(grid, p, top);
                return;
            // A chest prop (crates stand in until chest art exists, docs/ART_NEEDED.md section 0).
            case "chest" when Props("chest").Count + Props("crates").Count > 0:
                var chest = Props("chest").Count > 0 ? Props("chest") : Props("crates");
                // Smaller than the crates (session 17 playtest: "the chest looks too big").
                AddBillboard(chest[(int)(Hash(p, 97) % (uint)chest.Count)], top, scale: ChestScale);
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
            case "barricade":
                AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh { Size = new Vector3(0.7f, 0.45f, 0.5f) },
                    MaterialOverride = Material("#7a5634", 0f),
                    Position = top + new Vector3(0f, 0.22f, 0f),
                });
                break;
            case "throne":
                // A tall back at the north edge of the cell, so whoever sits on it stays in view.
                AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh { Size = new Vector3(0.6f, 0.9f, 0.12f) },
                    MaterialOverride = Material("#b8902e", 0f),
                    Position = top + new Vector3(0f, 0.45f, -0.4f),
                });
                AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh { Size = new Vector3(0.44f, 0.6f, 0.04f) },
                    MaterialOverride = Material("#2f5a34", 0f),
                    Position = top + new Vector3(0f, 0.42f, -0.33f),
                });
                break;
        }
    }

    /// <summary>
    /// A castle gate (Ch 5): 3D oak doors that fill each straight run of gate cells along the wall's line, like the
    /// hedges, with a stone post at each end of the run. Gate cells sit at ground height in the wall, so when
    /// a trigger breaks the gate the gap is level with the yard.
    /// </summary>
    private void AddGate(BattleGrid grid, GridPos p, Vector3 top)
    {
        bool IsGate(int x, int y) => grid.InBounds(new GridPos(x, y)) && grid[new GridPos(x, y)].Terrain.Id is "gate" or "door";
        bool northSouth = IsGate(p.X, p.Y - 1) || IsGate(p.X, p.Y + 1);
        if (!northSouth && !(IsGate(p.X - 1, p.Y) || IsGate(p.X + 1, p.Y)))
        {
            // A lone gate cell runs along whichever axis its wall does.
            bool Wall(int x, int y) => grid.InBounds(new GridPos(x, y)) && grid[new GridPos(x, y)].Terrain.Id == "wall";
            northSouth = Wall(p.X, p.Y - 1) || Wall(p.X, p.Y + 1);
        }
        // Drawn once, from the run's first (north or west) cell.
        if (northSouth ? IsGate(p.X, p.Y - 1) : IsGate(p.X - 1, p.Y))
            return;
        int length = 1;
        while (northSouth ? IsGate(p.X, p.Y + length) : IsGate(p.X + length, p.Y))
            length++;
        var along = northSouth ? Vector3.Back : Vector3.Right; // +Z (south) or +X (east)
        var across = northSouth ? Vector3.Right : Vector3.Back;
        // As tall as the wall it closes (the cell before the run), else one step.
        var before = northSouth ? new GridPos(p.X, p.Y - 1) : new GridPos(p.X - 1, p.Y);
        float height = grid.InBounds(before) ? GridMetrics.SurfaceY(grid, before) - top.Y : 0f;
        if (height < 0.2f)
            height = GridMetrics.StepHeight;

        Vector3 Size(float a, float c, float h) => (along * a + across * c).Abs() + new Vector3(0f, h, 0f);
        float yaw = northSouth ? Mathf.Pi / 2f : 0f;
        for (int i = 0; i < length; i++)
        {
            // A leaf per cell: a thick oak slab, its faces painted with the leaf (hinge outward, meeting edge inward).
            var centre = top + along * i + new Vector3(0f, height / 2f, 0f);
            bool mirrored = length > 1 && i == length - 1;
            AddGateBox(centre, Size(1f, GateThickness, height), northSouth ? GateTop : GateTopEastWest);
            foreach (int side in new[] { 1, -1 })
                AddGateQuad(centre + across * ((GateThickness / 2f + 0.002f) * side), yaw + (side < 0 ? Mathf.Pi : 0f),
                    height, mirrored ^ (side < 0) ? GateLeafMirrored : GateLeaf);
        }
        // A stone post with a cap at both ends of the run, a little above the wall.
        var stone = StoneSideMaterial(0) ?? Material("#8a8a80", 0f);
        float postHeight = height + 0.18f;
        foreach (float end in new[] { -0.5f, length - 0.5f })
        {
            var foot = top + along * end;
            AddGateBox(foot + new Vector3(0f, postHeight / 2f, 0f), new Vector3(0.36f, postHeight, 0.36f), stone);
            AddGateBox(foot + new Vector3(0f, postHeight + 0.03f, 0f), new Vector3(0.44f, 0.06f, 0.44f), stone);
        }
    }

    private void AddGateBox(Vector3 centre, Vector3 size, Material material)
    {
        AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = material, Position = centre });
    }

    private void AddGateQuad(Vector3 centre, float yaw, float height, Material material)
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = new Vector2(GridMetrics.CellSize, height) },
            MaterialOverride = material,
            Position = centre,
            Rotation = new Vector3(0f, yaw, 0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
    }

    private const string GateRoot = "res://assets/textures/gate";
    private StandardMaterial3D? _gateLeaf, _gateLeafMirrored, _gateTop;
    private StandardMaterial3D GateLeaf => _gateLeaf ??= GateLeafMaterial(flip: false);
    private StandardMaterial3D GateLeafMirrored => _gateLeafMirrored ??= GateLeafMaterial(flip: true);
    private const float GateThickness = 0.55f;
    private StandardMaterial3D? _gateTopEastWest;
    private StandardMaterial3D GateTop => _gateTop ??= GateTopMaterial(rotate: false);
    private StandardMaterial3D GateTopEastWest => _gateTopEastWest ??= GateTopMaterial(rotate: true);

    /// <summary>The doors' top (planks along the run): the art runs north–south, so east–west runs turn it.</summary>
    private static StandardMaterial3D GateTopMaterial(bool rotate)
    {
        var image = ResourceLoader.Load<Texture2D>($"{GateRoot}/top.png").GetImage();
        if (rotate)
            image.Rotate90(ClockDirection.Clockwise);
        return new StandardMaterial3D
        {
            AlbedoTexture = ImageTexture.CreateFromImage(image),
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            Uv1Triplanar = true,
            Uv1Offset = new Vector3(0.5f, 0f, 0.5f),
            Roughness = 1f,
        };
    }

    /// <summary>One door leaf on a face (tools/art/gate_textures.py); flipped for the other leaf of a pair.</summary>
    private static StandardMaterial3D GateLeafMaterial(bool flip) => new()
    {
        AlbedoTexture = ResourceLoader.Load<Texture2D>($"{GateRoot}/leaf.png"),
        TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
        Uv1Scale = new Vector3(flip ? -1f : 1f, 1f, 1f),
        Roughness = 1f,
    };

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
        var mesh = new BoxMesh { Size = new Vector3(x1 - x0, HedgeHeight, z1 - z0) };
        var position = top + new Vector3((x0 + x1) / 2f, HedgeHeight / 2f, (z0 + z1) / 2f);
        AddChild(new MeshInstance3D
        {
            Mesh = mesh,
            MaterialOverride = HedgeMaterial,
            Position = position,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
        // The visible hedge is pulled toward the camera in its shader; an unpulled copy casts its shadow.
        AddChild(new MeshInstance3D { Mesh = mesh, Position = position, CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly });
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

    /// <summary>
    /// An upright pixel-art prop standing on <paramref name="ground"/>, facing the camera like the units. The visible
    /// sprite is pulled toward the camera (see _Process), which would put its shadow away from its base, so with
    /// <paramref name="shadow"/> an invisible copy stands on the true ground point and casts the shadow instead.
    /// </summary>
    private void AddBillboard(Texture2D texture, Vector3 ground, bool shadow = true, float scale = 1f)
    {
        Sprite3D Make(GeometryInstance3D.ShadowCastingSetting cast) => new()
        {
            Texture = texture,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, // like the units: FixedY squashed props under the high camera
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            Shaded = false,
            PixelSize = ArtPixelSize * scale,
            Offset = new Vector2(0f, texture.GetHeight() / 2f), // the art is cropped, so this stands it on the ground
            Position = ground,
            CastShadow = cast,
        };
        var sprite = Make(GeometryInstance3D.ShadowCastingSetting.Off);
        AddChild(sprite);
        _billboards.Add((sprite, ground));
        if (shadow)
            AddChild(Make(GeometryInstance3D.ShadowCastingSetting.ShadowsOnly));
    }

    public override void _Process(double delta)
    {
        // Pull every prop toward the camera (BillboardPull) so raised terrain behind it can't cut through it.
        // Only when the camera has moved: there can be a few hundred props.
        var camera = GetViewport()?.GetCamera3D();
        if (camera is null)
            return;
        var eye = ToLocal(camera.GlobalPosition);
        bool moved = !camera.GlobalTransform.IsEqualApprox(_lastCamera);
        if (moved)
        {
            _lastCamera = camera.GlobalTransform;
            foreach (var (sprite, ground) in _billboards)
                sprite.Position = ground + (eye - ground).Normalized() * BillboardPull;
        }
        GridPos? focus = Cursor is { Visible: true } && Grid is not null ? Cursor.Pos : null;
        if (moved || focus != _fadedFor)
            FadeAround(focus, eye);
    }

    /// <summary>Makes the props near <paramref name="focus"/> and in front of it (toward the camera) see-through.</summary>
    private void FadeAround(GridPos? focus, Vector3 eye)
    {
        _fadedFor = focus;
        foreach (var sprite in _faded)
        {
            sprite.AlphaCut = SpriteBase3D.AlphaCutMode.Discard;
            sprite.Modulate = Colors.White;
        }
        _faded.Clear();
        if (focus is not { } cell || Grid is null)
            return;
        var center = GridMetrics.CellCenter(Grid, cell);
        var toCamera = new Vector2(eye.X - center.X, eye.Z - center.Z).Normalized();
        foreach (var (sprite, ground) in _billboards)
        {
            var d = new Vector2(ground.X - center.X, ground.Z - center.Z) / GridMetrics.CellSize;
            float along = d.Dot(toCamera);
            float across = (d - toCamera * along).Length();
            if (d.Length() > FadeRadius && (along <= 0f || along > FadeTowardCamera || across > FadeHalfWidth))
                continue;
            // Alpha blending only while faded: the props are alpha-cut otherwise (sharper, no sorting issues).
            sprite.AlphaCut = SpriteBase3D.AlphaCutMode.Disabled;
            sprite.Modulate = new Color(1f, 1f, 1f, FadeAlpha);
            _faded.Add(sprite);
        }
    }

    /// <summary>One variant of a decoration prop (picked by the cell hash), if that prop has art.</summary>
    private void AddDecor(string id, GridPos p, int salt, Vector3 ground)
    {
        // Themed maps (Ch 14's snow) only scatter decorations drawn for them: green tufts and bushes looked wrong on snow.
        if (Themed && LoadProps($"{_artSet}_{id}").Count == 0)
            return;
        var art = Props(id);
        // Only the taller decorations cast shadows; grass tufts and flowers are too small to show one.
        if (art.Count > 0)
            AddBillboard(art[(int)(Hash(p, salt) % (uint)art.Count)], ground, shadow: id is "wheat" or "bush" or "reed");
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
    private List<Texture2D>? _siegeProps;

    /// <summary>Every siege-clutter prop together, for barricade cells.</summary>
    private List<Texture2D> SiegeProps() =>
        _siegeProps ??= new[] { "wagon", "stall", "crates", "barrels", "cart" }.SelectMany(Props).ToList();

    /// <summary>A prop's art: the map's art set version ("snow_pine") when there is one, else the usual.</summary>
    private List<Texture2D> Props(string id) =>
        _artSet is { } set && LoadProps($"{set}_{id}") is { Count: > 0 } themed ? themed : LoadProps(id);

    private List<Texture2D> LoadProps(string id)
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
    /// <summary>Terrains whose raised blocks are masonry, not earth (castle walls and floors), outside farm maps.</summary>
    private static readonly HashSet<string> StoneSides = new() { "wall", "square", "throne", "fort" };

    private StandardMaterial3D SideMaterial(string terrainId, string colorHex, GridPos p)
    {
        // Raised bridge blocks are Ch 12's ship decks: their sides are the hulls (tools/art/hull_texture.py).
        if (terrainId == "bridge" && SideTextureMaterial("hull", HullTexture) is { } hull)
            return hull;
        if (_artSet != "farm" && StoneSides.Contains(terrainId) && StoneSideMaterial(terrainId == "wall" && Hash(p, 120) % 4 == 0 ? 1 : 0) is { } stone)
            return stone;
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

    /// <summary>
    /// Masonry for the sides of stone blocks (assets/textures/wall_side[_N].png, tools/art/wall_art.py): one tile per
    /// cell face, lined up with the cell edges so the courses meet at the corners. Null when the art is missing.
    /// </summary>
    private StandardMaterial3D? StoneSideMaterial(int variant)
    {
        string path = variant == 0 ? WallSideRoot + ".png" : $"{WallSideRoot}_{variant}.png";
        if (!ResourceLoader.Exists(path))
            return variant == 0 ? null : StoneSideMaterial(0);
        string key = $"stone_side_{variant}";
        if (!_materials.TryGetValue(key, out var material))
        {
            material = new StandardMaterial3D
            {
                AlbedoTexture = ResourceLoader.Load<Texture2D>(path),
                AlbedoColor = new Color(0.82f, 0.82f, 0.82f),
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                Uv1Triplanar = true,
                Uv1Scale = Vector3.One,
                // Cell centres sit on whole numbers: shift half a cell so each face shows one whole tile.
                Uv1Offset = new Vector3(0.5f, 0f, 0.5f),
                Roughness = 0.95f,
            };
            _materials[key] = material;
        }
        return material;
    }

    private const float ChestScale = 0.7f;

    /// <summary>An art set with its own tiles and props (not "farm", which only swaps tiles round).</summary>
    private bool Themed => _artSet is { } set && set != "farm";
    private const string HullTexture = "res://assets/textures/hull_side.png";
    private const float BulwarkHeight = 0.2f, BulwarkThickness = 0.07f;

    /// <summary>
    /// A ship deck (a raised bridge cell) gets a low hull rail along each edge over open water, so the ships read as
    /// ships, not raised docks (session 17). Edges onto gangplanks, piers or land stay open. Looks only: no effect on
    /// movement.
    /// </summary>
    private void AddBulwarks(BattleGrid grid, GridPos p, Vector3 top)
    {
        if (SideTextureMaterial("hull", HullTexture) is not { } hull)
            return;
        foreach (var (dx, dy) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
        {
            var n = new GridPos(p.X + dx, p.Y + dy);
            if (grid.InBounds(n) && grid[n].Terrain.Id != "water")
                continue;
            float half = GridMetrics.CellSize / 2f - BulwarkThickness / 2f;
            var size = dx == 0 ? new Vector3(GridMetrics.CellSize, BulwarkHeight, BulwarkThickness)
                               : new Vector3(BulwarkThickness, BulwarkHeight, GridMetrics.CellSize);
            AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = size },
                MaterialOverride = hull,
                Position = top + new Vector3(dx * half, BulwarkHeight / 2f, dy * half),
            });
        }
    }

    /// <summary>A side texture laid one tile per cell face, like the masonry. Null when the art is missing.</summary>
    private StandardMaterial3D? SideTextureMaterial(string key, string path)
    {
        if (!ResourceLoader.Exists(path))
            return null;
        if (!_materials.TryGetValue(key, out var material))
        {
            material = new StandardMaterial3D
            {
                AlbedoTexture = ResourceLoader.Load<Texture2D>(path),
                AlbedoColor = new Color(0.82f, 0.82f, 0.82f),
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                Uv1Triplanar = true,
                Uv1Scale = Vector3.One,
                Uv1Offset = new Vector3(0.5f, 0f, 0.5f),
                Roughness = 0.95f,
            };
            _materials[key] = material;
        }
        return material;
    }

    private const string WallSideRoot = "res://assets/textures/wall_side";
    private const string WallDecorRoot = "res://assets/textures/wall_decor";
    private static readonly string[] WallDecor = { "slit", "slit", "window", "banner_green", "banner_red" };

    /// <summary>
    /// Arrow slits, barred windows and banners on some of a wall cell's exposed faces (the sides that stand at least
    /// a step above their neighbour, or face off the map), in the top half-cell of the face. About one face in four.
    /// </summary>
    private void AddWallDecor(BattleGrid grid, GridPos p, Vector3 top)
    {
        var faces = new (int Dx, int Dy, float Yaw)[] { (0, 1, 0f), (1, 0, Mathf.Pi / 2f), (0, -1, Mathf.Pi), (-1, 0, -Mathf.Pi / 2f) };
        for (int i = 0; i < faces.Length; i++)
        {
            var (dx, dy, yaw) = faces[i];
            var n = new GridPos(p.X + dx, p.Y + dy);
            if (grid.InBounds(n) && top.Y - GridMetrics.SurfaceY(grid, n) < GridMetrics.StepHeight - 0.01f)
                continue;
            uint roll = Hash(p, 130 + i);
            if (roll % 4 != 0)
                continue;
            string id = WallDecor[(int)(roll / 4 % (uint)WallDecor.Length)];
            if (WallDecorMaterial(id) is not { } material)
                continue;
            var normal = new Vector3(dx, 0f, dy);
            AddChild(new MeshInstance3D
            {
                Mesh = new QuadMesh { Size = new Vector2(GridMetrics.CellSize, GridMetrics.StepHeight) },
                MaterialOverride = material,
                Position = top + normal * (GridMetrics.CellSize / 2f + 0.004f) + new Vector3(0f, -GridMetrics.StepHeight / 2f, 0f),
                Rotation = new Vector3(0f, yaw, 0f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
        }
    }

    private StandardMaterial3D? WallDecorMaterial(string id)
    {
        string path = $"{WallDecorRoot}/{id}.png";
        if (!ResourceLoader.Exists(path))
            return null;
        string key = $"wall_decor_{id}";
        if (!_materials.TryGetValue(key, out var material))
        {
            material = new StandardMaterial3D
            {
                AlbedoTexture = ResourceLoader.Load<Texture2D>(path),
                AlbedoColor = new Color(0.82f, 0.82f, 0.82f),
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor,
                Roughness = 0.95f,
            };
            _materials[key] = material;
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
