using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace NotFireEmblem.Grid;

/// <summary>
/// Paints a flat map's ground into one image, SNES style (GridMetrics.Flat), in four layers:
///   1. Grass, with forest blended in by Wang corner tiles on a dual grid: one 32 px tile centred on every cell
///      corner, picked by which of the four cells around it are forest.
///   2. Paths, painted in code: one continuous dirt track through the road cells' centres, with a wavering edge,
///      a darker rim and a soft shadow on the grass. Tiled path art repeats its edges every cell; this doesn't.
///   3. Ground objects inside their cells (walls, fields, water, bridges...), frayed where they meet other ground.
///   4. Standalone objects drawn whole on top (houses, peaks, hedges), from transparent sprites.
///
/// Art lives in assets/textures/flat/: wang/&lt;terrain&gt;/ (tile_N.png + corners.txt), objects/&lt;terrain&gt;_N.png,
/// sprites/&lt;name&gt;_N.png. A map's art set (e.g. "farm") checks &lt;set&gt;/ subfolders first.
/// </summary>
public static class FlatTerrain
{
    /// <summary>SNES-scale cells: 16 art pixels each, drawn large (TacticsCamera keeps every pixel whole).</summary>
    public const int TilePixels = 16;
    public const string Root = "res://assets/textures/flat";

    /// <summary>Path colours (earthy dirt, light to dark) and the rim.</summary>
    private static readonly Color[] Dirt =
    {
        Color.Color8(162, 127, 81), Color.Color8(154, 120, 77), Color.Color8(146, 114, 73), Color.Color8(134, 103, 65),
    };
    private static readonly Color DirtRim = Color.Color8(117, 86, 54);

    /// <summary>Terrains drawn as whole sprites instead of tiles: terrain id → sprite name (by art set).</summary>
    private static readonly Dictionary<string, string> SpriteFor = new()
    {
        ["fort"] = "house", ["farm:fort"] = "farmhouse", ["mountain"] = "peak", ["farm:wall"] = "hedge",
    };

    private sealed record WangSet(Dictionary<(bool, bool, bool, bool), List<Image>> Tiles);

    private static readonly Dictionary<string, WangSet?> WangCache = new();
    private static readonly Dictionary<string, List<Image>> ImageCache = new();

    /// <summary>Dark gaps between treetops: the forest floor in shadow.</summary>
    private static readonly Color CanopyShadow = Color.Color8(26, 54, 34);

    /// <summary>The ground image for a grid (TilePixels per cell), or null if no flat art is installed.</summary>
    public static Image? Paint(BattleGrid grid, string? artSet = null)
    {
        var grass = Images("ground/plain", artSet);
        if (grass.Count == 0)
            return null;

        int w = grid.Width, h = grid.Height, T = TilePixels;
        var image = Image.CreateEmpty(w * T, h * T, false, Image.Format.Rgba8);
        string At(int x, int y) => grid[new GridPos(Math.Clamp(x, 0, w - 1), Math.Clamp(y, 0, h - 1))].Terrain.Id;
        bool Is(int x, int y, string id) => x >= 0 && y >= 0 && x < w && y < h && At(x, y) == id;

        // 1. Textured grass under everything, painted per pixel so it never repeats.
        PaintGrass(image);

        // 1b. Forests: a smooth "forestness" field (cell values blended between cell centres) outlines each wood
        //     as one organic shape. Its floor is dark shadow; round treetops on a staggered half-cell lattice
        //     overlap on top, so edges are scalloped by the trees themselves.
        float Forestness(float fx, float fy)
        {
            float gx = fx - 0.5f, gy = fy - 0.5f;
            int ix = (int)MathF.Floor(gx), iy = (int)MathF.Floor(gy);
            float tx = gx - ix, ty = gy - iy;
            float V(int a, int b) => Is(Math.Clamp(a, 0, w - 1), Math.Clamp(b, 0, h - 1), "forest") ? 1f : 0f;
            float top = V(ix, iy) + (V(ix + 1, iy) - V(ix, iy)) * tx;
            float bottom = V(ix, iy + 1) + (V(ix + 1, iy + 1) - V(ix, iy + 1)) * tx;
            return top + (bottom - top) * ty;
        }
        {
            for (int py = 0; py < h * T; py++)
                for (int px = 0; px < w * T; px++)
                {
                    float f = Forestness((px + 0.5f) / T, (py + 0.5f) / T) + (Noise(px, py, 4f) - 0.5f) * 0.25f;
                    if (f > 0.62f)
                        image.SetPixel(px, py, CanopyShadow);
                }
            // Tree clumps on a staggered lattice, a third of a cell apart, drawn top to bottom.
            int step = Math.Max(4, T / 3);
            for (int ty = -step; ty < h * T + step; ty += step)
                for (int tx = -step; tx < w * T + step; tx += step)
                {
                    int cx = tx + ((ty / step) % 2 == 0 ? 0 : step / 2) + (int)(Hash(tx, ty, 13) % 3) - 1;
                    int cy = ty + (int)(Hash(tx, ty, 14) % 3) - 1;
                    if (Forestness((cx + 0.5f) / T, (cy + 0.5f) / T) < 0.6f)
                        continue;
                    DrawTree(image, cx, cy, 4 + (int)(Hash(tx, ty, 15) % 2));
                }
        }

        // 2. Paths.
        PaintPaths(image, grid, Is);

        // 3. Ground objects, frayed where they meet a different terrain.
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                string id = At(x, y);
                if (SpriteName(id, artSet) is not null || Images($"objects/{id}", artSet) is not { Count: > 0 } tiles)
                    continue;
                var tile = tiles[(int)(Hash(x, y, 3) % (uint)tiles.Count)];
                Blit(image, Fray(tile, y > 0 && At(x, y - 1) != id, y < h - 1 && At(x, y + 1) != id,
                    x > 0 && At(x - 1, y) != id, x < w - 1 && At(x + 1, y) != id, x, y), x * T, y * T);
            }

        // 4. Standalone sprites, drawn whole (top to bottom so lower ones overlap the ones behind).
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                string id = At(x, y);
                if (SpriteName(id, artSet) is not { } name)
                    continue;
                var sprites = Images($"sprites/{name}", artSet);
                if (name != "hedge" && sprites.Count == 0)
                    continue;
                if (name == "hedge")
                {
                    // A hedge row: small tree clumps along the cell, so neighbouring cells join up.
                    for (int i = 0; i < 3; i++)
                        DrawTree(image, x * T + T / 6 + i * T / 3, y * T + T / 2 + (int)(Hash(x, y, 40 + i) % 3) - 1, 3);
                    continue;
                }
                var sprite = sprites[(int)(Hash(x, y, 5) % (uint)sprites.Count)];
                Blit(image, sprite, x * T + (T - sprite.GetWidth()) / 2, y * T + T - sprite.GetHeight());
            }
        return image;
    }

    private static string? SpriteName(string terrainId, string? artSet) =>
        artSet is not null && SpriteFor.TryGetValue($"{artSet}:{terrainId}", out var named) ? named
        : SpriteFor.GetValueOrDefault(terrainId);

    private static readonly Color TreeOutline = Color.Color8(22, 44, 30);
    private static readonly Color TreeDark = Color.Color8(40, 70, 40);
    private static readonly Color TreeMid = Color.Color8(58, 96, 48);
    private static readonly Color TreeLight = Color.Color8(86, 124, 60);

    /// <summary>
    /// One SNES-style tree clump seen from above: a round canopy with a dark outline, lit from the top left in two
    /// greens with a dithered boundary, and a darker underside.
    /// </summary>
    private static void DrawTree(Image image, int cx, int cy, int radius)
    {
        int width = image.GetWidth(), height = image.GetHeight();
        for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                int px = cx + dx, py = cy + dy;
                if (px < 0 || py < 0 || px >= width || py >= height)
                    continue;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d > radius + 0.3f)
                    continue;
                Color c;
                if (d > radius - 0.7f)
                    c = TreeOutline;
                else
                {
                    float light = -(dx + dy) / (radius * 1.6f) + (Bayer[((py & 3) * 4) + (px & 3)] / 16f - 0.5f) * 0.5f;
                    c = light > 0.35f ? TreeLight : light > -0.3f ? TreeMid : TreeDark;
                }
                image.SetPixel(px, py, c);
            }
    }

    /// <summary>Grass shades from the Thracia sketch, dark to light, plus tufts, flecks and flowers.</summary>
    private static readonly Color[] GrassShades =
    {
        Color.Color8(40, 63, 38), Color.Color8(55, 91, 45), Color.Color8(66, 107, 51), Color.Color8(90, 124, 64),
    };
    private static readonly Color GrassTuft = Color.Color8(26, 54, 34);
    private static readonly Color GrassFleck = Color.Color8(112, 140, 74);
    private static readonly Color[] Flowers = { Color.Color8(222, 222, 204), Color.Color8(222, 198, 92) };

    /// <summary>4×4 Bayer matrix (0..1) for SNES-style ordered dithering between shades.</summary>
    private static readonly float[] Bayer =
        { 0f, 8f, 2f, 10f, 12f, 4f, 14f, 6f, 3f, 11f, 1f, 9f, 15f, 7f, 13f, 5f };

    /// <summary>
    /// Grass by noise: big soft patches and smaller clumps choose one of four shades, dithered at the boundaries;
    /// then dark two-pixel tufts, light flecks and the odd flower. Seamless, and never the same twice.
    /// </summary>
    private static void PaintGrass(Image image)
    {
        int width = image.GetWidth(), height = image.GetHeight();
        for (int py = 0; py < height; py++)
            for (int px = 0; px < width; px++)
            {
                float v = Noise(px, py, 13f) * 0.55f + Noise(px, py, 4f) * 0.45f;
                v += (Bayer[(py & 3) * 4 + (px & 3)] / 16f - 0.5f) * 0.16f;
                int shade = v < 0.34f ? 0 : v < 0.5f ? 1 : v < 0.7f ? 2 : 3;
                var color = GrassShades[shade];
                uint r = Hash(px, py, 21) % 1000;
                if (r < 22 && shade < 3)
                    color = GrassTuft;
                else if (r > 992)
                    color = GrassFleck;
                else if (r == 500 && Hash(px, py, 23) % 2 == 0)
                    color = Flowers[Hash(px, py, 22) % 2];
                image.SetPixel(px, py, color);
                // Tufts are two pixels tall: darken the pixel above too.
                if (r < 6 && py > 0)
                    image.SetPixel(px, py - 1, GrassTuft);
            }
    }

    /// <summary>
    /// One continuous path: every road cell's centre joins its road neighbours (and the map edge, if it's on one)
    /// with a line; pixels within a wavering half-width of those lines are dirt.
    /// </summary>
    private static void PaintPaths(Image image, BattleGrid grid, Func<int, int, string, bool> isAt)
    {
        int w = grid.Width, h = grid.Height, T = TilePixels;
        var segments = new List<(Vector2 A, Vector2 B)>();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!isAt(x, y, "road"))
                    continue;
                var c = new Vector2(x + 0.5f, y + 0.5f);
                segments.Add((c, c));
                if (isAt(x + 1, y, "road")) segments.Add((c, c + Vector2.Right));
                if (isAt(x, y + 1, "road")) segments.Add((c, c + Vector2.Down));
                // Roads that meet something solid (a house, a bridge) run into it; roads at the edge leave the map.
                if (x == 0) segments.Add((c, c + Vector2.Left));
                if (y == 0) segments.Add((c, c + Vector2.Up));
                if (x == w - 1) segments.Add((c, c + Vector2.Right));
                if (y == h - 1) segments.Add((c, c + Vector2.Down));
                foreach (var d in new[] { Vector2.Right, Vector2.Left, Vector2.Up, Vector2.Down })
                {
                    int nx = x + (int)d.X, ny = y + (int)d.Y;
                    if (nx >= 0 && ny >= 0 && nx < w && ny < h && grid[new GridPos(nx, ny)].Terrain.Id is "bridge" or "fort")
                        segments.Add((c, c + d * 0.5f));
                }
            }
        if (segments.Count == 0)
            return;

        for (int py = 0; py < h * T; py++)
            for (int px = 0; px < w * T; px++)
            {
                var p = new Vector2((px + 0.5f) / T, (py + 0.5f) / T);
                float dist = float.MaxValue;
                foreach (var (a, b) in segments)
                {
                    if (MathF.Abs(p.X - a.X) > 2f || MathF.Abs(p.Y - a.Y) > 2f)
                        continue;
                    dist = MathF.Min(dist, SegmentDistance(p, a, b));
                }
                float half = 0.34f + 0.07f * (Noise(px, py, 6f) - 0.5f) * 2f;
                if (dist < half)
                {
                    float n = Noise(px, py, 2.5f) * 0.7f + Noise(px, py, 1.2f) * 0.3f;
                    var color = dist > half - 0.07f ? DirtRim : Dirt[Math.Clamp((int)(n * Dirt.Length), 0, Dirt.Length - 1)];
                    if (Hash(px, py, 7) % 61 == 0)
                        color = color.Lightened(0.18f); // the odd pebble
                    image.SetPixel(px, py, color);
                }
                else if (dist < half + 0.07f)
                {
                    image.SetPixel(px, py, image.GetPixel(px, py).Darkened(0.18f)); // soft edge on the grass
                }
            }
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float len = ab.LengthSquared();
        float t = len == 0f ? 0f : Math.Clamp((p - a).Dot(ab) / len, 0f, 1f);
        return (p - (a + ab * t)).Length();
    }

    /// <summary>Alpha-blends an image in, clipping at the edges (dual-grid tiles hang half off the map).</summary>
    private static void Blit(Image dst, Image src, int x, int y)
    {
        int sx = Math.Max(0, -x), sy = Math.Max(0, -y);
        int ex = Math.Min(src.GetWidth(), dst.GetWidth() - x), ey = Math.Min(src.GetHeight(), dst.GetHeight() - y);
        if (ex <= sx || ey <= sy)
            return;
        dst.BlendRect(src, new Rect2I(sx, sy, ex - sx, ey - sy), new Vector2I(x + sx, y + sy));
    }

    /// <summary>A copy of the tile whose edges facing other terrain fade out raggedly, so it sits softly on the grass.</summary>
    private static Image Fray(Image tile, bool north, bool south, bool west, bool east, int cx, int cy)
    {
        if (!north && !south && !west && !east)
            return tile;
        var copy = (Image)tile.Duplicate();
        int size = copy.GetWidth();
        for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                float depth = 0.8f + 2.4f * Noise(cx * size + px, cy * size + py, 3f);
                if ((north && py < depth) || (south && size - 1 - py < depth) || (west && px < depth) || (east && size - 1 - px < depth))
                    copy.SetPixel(px, py, new Color(0, 0, 0, 0));
            }
        return copy;
    }

    /// <summary>Value noise in 0..1 on a lattice of the given spacing (pixels).</summary>
    private static float Noise(int x, int y, float scale)
    {
        float fx = x / scale, fy = y / scale;
        int ix = (int)MathF.Floor(fx), iy = (int)MathF.Floor(fy);
        float tx = fx - ix, ty = fy - iy;
        tx = tx * tx * (3 - 2 * tx);
        ty = ty * ty * (3 - 2 * ty);
        int salt = (int)(scale * 10);
        float V(int a, int b) => (Hash(a, b, salt) & 0xffff) / 65535f;
        float top = V(ix, iy) + (V(ix + 1, iy) - V(ix, iy)) * tx;
        float bottom = V(ix, iy + 1) + (V(ix + 1, iy + 1) - V(ix, iy + 1)) * tx;
        return top + (bottom - top) * ty;
    }

    private static WangSet? Wang(string terrainId)
    {
        if (WangCache.TryGetValue(terrainId, out var cached))
            return cached;
        string dir = $"{Root}/wang/{terrainId}";
        WangSet? set = null;
        if (FileAccess.FileExists($"{dir}/corners.txt"))
        {
            var tiles = new Dictionary<(bool, bool, bool, bool), List<Image>>();
            foreach (var line in FileAccess.GetFileAsString($"{dir}/corners.txt").Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var p = line.Trim().Split(' ');
                if (p.Length != 5 || LoadImage($"{dir}/tile_{p[0]}.png") is not { } img)
                    continue;
                var key = (p[1] == "u", p[2] == "u", p[3] == "u", p[4] == "u");
                if (!tiles.TryGetValue(key, out var list))
                    tiles[key] = list = new List<Image>();
                list.Add(img);
            }
            set = tiles.Count > 0 ? new WangSet(tiles) : null;
        }
        WangCache[terrainId] = set;
        return set;
    }

    /// <summary>Numbered images "&lt;stem&gt;_N.png", from the art set's folder first.</summary>
    private static List<Image> Images(string stem, string? artSet)
    {
        foreach (var candidate in artSet is null ? new[] { stem } : new[] { $"{artSet}/{stem}", stem })
        {
            if (!ImageCache.TryGetValue(candidate, out var list))
            {
                list = new List<Image>();
                for (int i = 0; LoadImage($"{Root}/{candidate}_{i}.png") is { } img; i++)
                    list.Add(img);
                ImageCache[candidate] = list;
            }
            if (list.Count > 0)
                return list;
        }
        return new List<Image>();
    }

    private static Image? LoadImage(string path)
    {
        if (!ResourceLoader.Exists(path))
            return null;
        var image = ResourceLoader.Load<Texture2D>(path).GetImage();
        if (image.IsCompressed())
            image.Decompress();
        image.Convert(Image.Format.Rgba8);
        return image;
    }

    private static uint Hash(int x, int y, int salt) =>
        (uint)((x * 73856093) ^ (y * 19349663) ^ (salt * 83492791)) * 2654435761u >> 8;
}
