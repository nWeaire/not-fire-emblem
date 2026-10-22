"""Textures for the 3D castle gate (MapView.AddGate), drawn in code at the map's 32 px per cell. Session 11 replaced
the camera-facing gate_0.png prop (and gate_art.py) with these:

- assets/textures/gate/leaf.png: one oak door leaf, 32x16 (a cell wide, a wall step high), hinge on the left and
  the meeting edge on the right, iron bands, studs and a ring pull. Mirrored for the other leaf.
- assets/textures/gate/top.png: the closed doors seen from above, 32x32, planks along a north-south run.
    python tools/art/gate_textures.py
"""
import random
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "assets/textures/gate"

OUTLINE = (30, 22, 18, 255)
WOOD_DARK = (64, 40, 24, 255)
WOOD = (128, 84, 48, 255)
LIGHT = (158, 108, 62, 255)
GRAIN = (108, 68, 38, 255)
IRON = (58, 58, 66, 255)
IRON_LIGHT = (130, 130, 142, 255)
IRON_DARK = (30, 30, 36, 255)


def leaf() -> Image.Image:
    rnd = random.Random(5)
    im = Image.new("RGBA", (32, 16), WOOD)
    px = im.load()
    for x in range(32):
        plank = x // 4
        base = LIGHT if plank % 2 else WOOD
        for y in range(16):
            px[x, y] = base
            if x % 4 == 0:
                px[x, y] = WOOD_DARK
            elif rnd.random() < 0.12:
                px[x, y] = GRAIN
    for band in (3, 11):
        for x in range(1, 31):
            px[x, band] = IRON
            px[x, band + 1] = IRON_DARK
            if x % 6 == 3:
                px[x, band] = IRON_LIGHT
    # ring pull near the meeting edge
    for (x, y) in ((27, 7), (28, 6), (29, 7), (28, 8), (27, 8), (29, 8), (28, 9)):
        px[x, y] = IRON_DARK if y == 9 else IRON
    px[28, 7] = IRON_LIGHT
    for y in range(16):
        px[0, y] = OUTLINE
        px[31, y] = OUTLINE
    for x in range(32):
        px[x, 0] = OUTLINE
    return im


def top() -> Image.Image:
    """The doors seen from above (a north-south run): planks along the run, crossed by iron straps."""
    rnd = random.Random(9)
    im = Image.new("RGBA", (32, 32), WOOD)
    px = im.load()
    for x in range(32):
        for y in range(32):
            base = LIGHT if (x // 4) % 2 else WOOD
            px[x, y] = GRAIN if rnd.random() < 0.12 else base
            if x % 4 == 0:
                px[x, y] = WOOD_DARK
    for strap in (5, 25):
        for x in range(32):
            px[x, strap] = IRON
            px[x, strap + 1] = IRON_DARK
            if x % 5 == 2:
                px[x, strap] = IRON_LIGHT
    return im


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    leaf().save(OUT / "leaf.png")
    top().save(OUT / "top.png")
    print("wrote", OUT)
