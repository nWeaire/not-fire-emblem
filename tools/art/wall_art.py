"""Builds the stone-wall art for the sides of raised wall blocks (MapView.AddWallFaces), in the wall tile's palette:

- assets/textures/wall_side.png, wall_side_1.png: seamless 32 px masonry for the block sides. The first is even
  courses of dressed stone; the second is rougher, with odd-sized blocks, moss and a crack.
- assets/textures/wall_decor/<id>.png: 32x16 see-through overlays for the top half of a wall face: an arrow slit, a
  barred window and two hanging banners.

Drawn in code (GBA browns-and-greys, a dark outline) so the masonry lines up with the cells. Deterministic, so reruns
give the same files.
    python tools/art/wall_art.py
"""
import random
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SIZE = 32

MORTAR = (85, 80, 68, 255)
STONES = [(144, 144, 132, 255), (162, 161, 150, 255), (125, 126, 110, 255), (120, 118, 104, 255)]
SHADOW = (106, 108, 96, 255)
LIGHT = (178, 177, 166, 255)
MOSS = (92, 104, 70, 255)
MOSS_LIGHT = (112, 126, 82, 255)
DARK = (34, 32, 30, 255)
VOID = (22, 20, 24, 255)


def courses(rng: random.Random, lengths: list[int]) -> list[list[tuple[int, int]]]:
    """For each 8 px course, the (start, length) of its stones, wrapping round the 32 px tile."""
    rows = []
    for i in range(SIZE // 8):
        start = rng.randrange(SIZE) if len(lengths) > 1 else (0 if i % 2 == 0 else 8)
        stones, x = [], 0
        while x < SIZE:
            n = min(rng.choice(lengths), SIZE - x)
            if 0 < SIZE - x - n < 6:  # no slivers at the end of the course
                n = SIZE - x
            stones.append(((start + x) % SIZE, n))
            x += n
        rows.append(stones)
    return rows


def masonry(seed: int, lengths: list[int], moss: bool) -> Image.Image:
    rng = random.Random(seed)
    img = Image.new("RGBA", (SIZE, SIZE), MORTAR)
    px = img.load()
    for row, stones in enumerate(courses(rng, lengths)):
        y0 = row * 8
        for start, length in stones:
            colour = rng.choice(STONES)
            for dx in range(1, length):
                x = (start + dx) % SIZE
                for dy in range(1, 8):
                    c = colour
                    if dy == 1:
                        c = LIGHT
                    elif dy == 7:
                        c = SHADOW
                    elif rng.random() < 0.06:
                        c = SHADOW
                    px[x, y0 + dy] = c
    if moss:
        # Moss creeping along a few mortar lines, and one crack.
        for _ in range(26):
            x, y = rng.randrange(SIZE), rng.choice((0, 8, 16, 24)) + rng.choice((0, 0, 1))
            px[x, y % SIZE] = rng.choice((MOSS, MOSS_LIGHT))
        x, y = rng.randrange(SIZE), 10
        for _ in range(9):
            px[x % SIZE, y % SIZE] = DARK
            x += rng.choice((-1, 0, 1))
            y += 1
    return img


def overlay() -> Image.Image:
    return Image.new("RGBA", (SIZE, 16), (0, 0, 0, 0))


def arrow_slit() -> Image.Image:
    img = overlay()
    px = img.load()
    # A cross-shaped arrow loop in a lighter dressed-stone surround.
    for y in range(2, 15):
        for x in range(13, 19):
            px[x, y] = LIGHT if x in (13, 18) or y in (2, 14) else STONES[1]
    for y in range(3, 14):
        px[15, y] = VOID
        px[16, y] = VOID
    for x in range(14, 18):
        px[x, 7] = VOID
    for y in range(3, 14):
        px[17, y] = SHADOW if px[17, y] != VOID else VOID
    return img


def window() -> Image.Image:
    img = overlay()
    px = img.load()
    # A small square window with a stone lintel and two iron bars.
    for x in range(10, 22):
        px[x, 2] = LIGHT
        px[x, 3] = STONES[1]
        px[x, 4] = SHADOW
    for y in range(5, 14):
        for x in range(11, 21):
            px[x, y] = VOID if 12 <= x <= 19 and y <= 12 else STONES[1]
    for y in range(5, 13):
        px[14, y] = (70, 70, 78, 255)
        px[17, y] = (70, 70, 78, 255)
    for x in range(11, 21):
        px[x, 13] = LIGHT  # the sill
    return img


def banner(cloth: tuple, light: tuple, emblem: tuple) -> Image.Image:
    img = overlay()
    px = img.load()
    # A rod across the top, the cloth hanging from it, a swallowtail hem and an emblem.
    for x in range(10, 22):
        px[x, 0] = DARK
        px[x, 1] = (120, 92, 52, 255)
    for y in range(2, 16):
        for x in range(11, 21):
            tail = y >= 13 and abs(x - 15.5) < (y - 12)  # the notch of the swallowtail
            if tail:
                continue
            edge = x in (11, 20) or y == 15 or (y >= 13 and abs(x - 15.5) < (y - 11))
            px[x, y] = DARK if edge else light if x == 12 else cloth
    for x, y in ((15, 5), (16, 5), (14, 6), (15, 6), (16, 6), (17, 6), (15, 7), (16, 7), (15, 8), (16, 8), (15, 9), (16, 9)):
        px[x, y] = emblem
    return img


def main() -> None:
    out = ROOT / "assets/textures"
    masonry(3, [16], moss=False).save(out / "wall_side.png")
    masonry(5, [8, 11, 14, 20], moss=True).save(out / "wall_side_1.png")
    decor = out / "wall_decor"
    decor.mkdir(exist_ok=True)
    arrow_slit().save(decor / "slit.png")
    window().save(decor / "window.png")
    banner((52, 104, 64, 255), (76, 132, 86, 255), (214, 176, 72, 255)).save(decor / "banner_green.png")
    banner((140, 40, 40, 255), (172, 64, 58, 255), (232, 226, 210, 255)).save(decor / "banner_red.png")
    print("wall art written to", out)


if __name__ == "__main__":
    main()
