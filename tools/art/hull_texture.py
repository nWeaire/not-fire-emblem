"""Builds the ship hull art for the sides of raised bridge blocks (Ch 12's ship decks, MapView.SideMaterial):

- assets/textures/hull_side.png: seamless 32 px hull planking. Four 8 px strakes of tarred oak with staggered butt
  joints and treenails, a painted red wale (the band along a ship's side) on the second strake, and a darker,
  weed-stained lowest strake at the waterline.

Drawn in code, like wall_art.py (GBA browns, a dark outline between planks), so the planks line up with the cells.
Deterministic, so reruns give the same file.
    python tools/art/hull_texture.py
"""
import random
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SIZE = 32
SEAM = (38, 26, 18, 255)
OAK = [(112, 78, 48, 255), (104, 72, 44, 255), (120, 84, 52, 255)]
OAK_LIGHT = (140, 102, 64, 255)
OAK_DARK = (86, 58, 36, 255)
WALE = (150, 46, 38, 255)
WALE_LIGHT = (184, 70, 54, 255)
WALE_DARK = (112, 32, 28, 255)
TAR = (64, 48, 34, 255)
WEED = (70, 86, 52, 255)
NAIL = (52, 40, 30, 255)


def main() -> None:
    rng = random.Random(1712)
    im = Image.new("RGBA", (SIZE, SIZE), SEAM)
    px = im.load()
    for strake in range(4):
        y0 = strake * 8
        painted = strake == 1
        waterline = strake == 3
        # Butt joints, staggered per strake, wrapping round the tile so it tiles sideways.
        joint = (strake * 11 + 5) % SIZE
        for x in range(SIZE):
            base = rng.choice(OAK)
            for y in range(y0, y0 + 8):
                if y == y0 + 7:
                    continue  # the seam below the strake
                if painted:
                    c = WALE_LIGHT if y == y0 else WALE_DARK if y == y0 + 6 else WALE
                elif waterline:
                    c = TAR if y > y0 + 3 or rng.random() < 0.25 else OAK_DARK
                    if y > y0 + 4 and rng.random() < 0.35:
                        c = WEED
                else:
                    c = OAK_LIGHT if y == y0 else OAK_DARK if y == y0 + 6 else base
                    if rng.random() < 0.06:
                        c = OAK_DARK  # grain
                px[x, y] = c
            if x == joint:
                for y in range(y0, y0 + 7):
                    px[x, y] = SEAM
        # Treenails either side of the joint.
        for dx in (-2, 2):
            px[(joint + dx) % SIZE, y0 + 3] = NAIL
    out = ROOT / "assets" / "textures" / "hull_side.png"
    im.save(out)
    print(f"wrote {out.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
