"""Builds assets/textures/hedge_leaves.png: a seamless 32 px leaf texture for the 3D hedges (MapView.AddHedge).

Colours come from the generated hedge prop (assets/sprites/props/hedge_0.png); each pixel is picked at random from
its greens, weighted by how often they appear, with a few darker leaf clusters, the GBA grass rule
(docs/NEXT_STEPS.md, session 6). Deterministic, so reruns give the same file.
    python tools/art/hedge_texture.py
"""
import random
from collections import Counter
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SIZE = 32


def main() -> None:
    src = Image.open(ROOT / "assets/sprites/props/hedge_0.png").convert("RGBA")
    pixels = src.get_flattened_data() if hasattr(src, "get_flattened_data") else src.getdata()
    counts = Counter(p[:3] for p in pixels if p[3] > 200 and p[1] > p[0] and p[1] > 40)
    greens = [c for c, _ in counts.most_common(8)]
    weights = [n for _, n in counts.most_common(8)]
    dark = min(greens, key=sum)
    rng = random.Random(7)
    img = Image.new("RGB", (SIZE, SIZE))
    px = img.load()
    for y in range(SIZE):
        for x in range(SIZE):
            px[x, y] = rng.choices(greens, weights)[0]
    # Darker clusters (gaps between leaves), wrapped at the edges so the tile stays seamless.
    for _ in range(14):
        cx, cy = rng.randrange(SIZE), rng.randrange(SIZE)
        for dx, dy in ((0, 0), (1, 0), (0, 1), (-1, 0), (0, -1)):
            if rng.random() < 0.8:
                px[(cx + dx) % SIZE, (cy + dy) % SIZE] = dark
    out = ROOT / "assets/textures/hedge_leaves.png"
    img.save(out)
    print(out, len(greens), "greens")


if __name__ == "__main__":
    main()
