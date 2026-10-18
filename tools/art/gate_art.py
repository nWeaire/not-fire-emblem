"""Builds the castle-gate art for MapView.AddGate (Ch 5's east gate):

- assets/sprites/props/gate_0.png: a camera-facing prop of two oak doors in a stone arch, with iron bands, studs
  and ring pulls, in GBA browns and greys with a dark outline like the other props.

Drawn in code rather than generated, so the gate is exactly the shape the map needs. Deterministic, so reruns give
the same file.
    python tools/art/gate_art.py
"""
import random
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
W, H = 46, 48

OUTLINE = (30, 22, 18, 255)
STONE = (128, 126, 116, 255)
STONE_LIGHT = (160, 158, 146, 255)
STONE_DARK = (88, 86, 80, 255)
WOOD_DARK = (64, 40, 24, 255)
SHADOW = (92, 58, 34, 255)
WOOD = (128, 84, 48, 255)
LIGHT = (158, 108, 62, 255)
GRAIN = (108, 68, 38, 255)
IRON = (58, 58, 66, 255)
IRON_LIGHT = (130, 130, 142, 255)
IRON_DARK = (30, 30, 36, 255)

CX = (W - 1) / 2  # centre line, where the leaves meet
ARCH_Y = 23       # centre of the arch's curve
OUTER_R = 22.5    # stone arch
INNER_R = 16.5    # door opening


def inside(x: int, y: int, r: float, top: int) -> bool:
    """In a round-topped shape of half-width r whose straight sides start at ARCH_Y."""
    dx = x - CX
    if abs(dx) > r or y < top:
        return False
    return y >= ARCH_Y or dx * dx + (y - ARCH_Y) ** 2 <= r * r


def main() -> None:
    rng = random.Random(11)
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()
    for y in range(H):
        for x in range(W):
            if not inside(x, y, OUTER_R, 0):
                continue
            if inside(x, y, INNER_R, 0):
                # The doors: vertical planks, 4 px each, mirrored about the centre seam.
                col = int(abs(x - CX))
                plank = col % 4
                c = WOOD_DARK if col == 0 else SHADOW if plank == 0 else LIGHT if plank == 1 else WOOD
                if c is WOOD and rng.random() < 0.08:
                    c = GRAIN
                px[x, y] = c
            else:
                # The arch: stone blocks in staggered courses.
                course = y // 5
                joint = (x + (3 if course % 2 else 0)) % 7 == 0 or y % 5 == 0
                px[x, y] = STONE_DARK if joint else STONE_LIGHT if (x + y) % 11 == 0 else STONE
    # Iron bands across both leaves, with studs on every plank.
    for band in (24, 38):
        for x in range(W):
            if inside(x, band, INNER_R, 0):
                px[x, band - 1] = IRON_DARK
                px[x, band] = IRON_LIGHT
                px[x, band + 1] = IRON
                px[x, band + 2] = IRON_DARK
        for x in range(W):
            if inside(x, band, INNER_R - 1, 0) and int(abs(x - CX)) % 4 == 2:
                px[x, band] = (210, 210, 220, 255)
    # A ring pull on each leaf, beside the seam.
    for side in (-1, 1):
        rx = int(CX + side * 4.5)
        for dx, dy in ((0, -1), (-1, 0), (1, 0), (-1, 1), (1, 1), (0, 2)):
            px[rx + dx, 30 + dy] = IRON_DARK
        px[rx, 30] = IRON_LIGHT
    # The dark seam between the leaves and a shadowed doorstep.
    for y in range(H):
        for x in (int(CX), int(CX) + 1):
            if inside(x, y, INNER_R, 0):
                px[x, y] = OUTLINE
    for x in range(W):
        if inside(x, H - 1, INNER_R, 0):
            px[x, H - 1] = WOOD_DARK
    # Outline everything, like the generated props.
    alpha = [[px[x, y][3] > 0 for x in range(W)] for y in range(H)]
    for y in range(H):
        for x in range(W):
            if not alpha[y][x]:
                continue
            edge = any(not (0 <= x + dx < W and 0 <= y + dy < H and alpha[y + dy][x + dx])
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            inner_edge = inside(x, y, INNER_R + 1, 0) and not inside(x, y, INNER_R, 0)
            if edge or inner_edge:
                px[x, y] = OUTLINE
    out = ROOT / "assets/sprites/props/gate_0.png"
    img.save(out)
    print(out)


if __name__ == "__main__":
    main()
