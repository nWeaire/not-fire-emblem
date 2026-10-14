"""Recolours generated crest line art into the two crest colours.

A crest keeps its shape and takes its colour from the heart that holds it (docs/story/STORY.md §4), so every crest
is generated once and recoloured: Radiant (shining white) and Shrouded (deep purple). Brightness is mapped onto
each ramp, so the outline, mid-tones and core keep their relationship.

Usage (from the repo root):
    python tools/art/crest_recolor.py <source.png> <out_dir> <name>   # writes <name>_radiant.png and <name>_shrouded.png
    python tools/art/crest_recolor.py --build                         # rebuilds assets/crests/ from tools/art/approved/crests/
"""
from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[2]
SOURCES = REPO / "tools" / "art" / "approved" / "crests"
OUT = REPO / "assets" / "crests"

RADIANT = [(30, 48, 96), (120, 160, 220), (205, 225, 250), (255, 255, 255)]
SHROUDED = [(8, 2, 14), (34, 6, 58), (76, 18, 122), (128, 48, 190), (176, 104, 236)]


def _ramp(stops, t):
    t = max(0.0, min(1.0, t)) * (len(stops) - 1)
    i = min(int(t), len(stops) - 2)
    f = t - i
    return tuple(int(stops[i][k] + (stops[i + 1][k] - stops[i][k]) * f) for k in range(3))


def recolor(image: Image.Image, stops) -> Image.Image:
    out = image.convert("RGBA")
    px = out.load()
    lums = [0.299 * r + 0.587 * g + 0.114 * b for r, g, b, a in out.get_flattened_data() if a > 0]
    lo, hi = min(lums), max(lums)
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a:
                px[x, y] = _ramp(stops, ((0.299 * r + 0.587 * g + 0.114 * b) - lo) / max(1.0, hi - lo)) + (a,)
    return out


def write_pair(source: Path, out_dir: Path, name: str) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    image = Image.open(source)
    recolor(image, RADIANT).save(out_dir / f"{name}_radiant.png")
    recolor(image, SHROUDED).save(out_dir / f"{name}_shrouded.png")


def build() -> None:
    """Every approved/crests/<line>/t<N>.png becomes assets/crests/<line>/t<N>_{radiant,shrouded}.png."""
    for source in sorted(SOURCES.glob("*/t*.png")):
        write_pair(source, OUT / source.parent.name, source.stem)
    print(f"crests written to {OUT.relative_to(REPO)}")


if __name__ == "__main__":
    if sys.argv[1:] == ["--build"]:
        build()
    elif len(sys.argv) == 4:
        write_pair(Path(sys.argv[1]), Path(sys.argv[2]), sys.argv[3])
    else:
        sys.exit(__doc__)
