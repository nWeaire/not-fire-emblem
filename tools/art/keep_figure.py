"""Erases painted-in effects from map sprite frames: keeps the figure (the largest 8-connected shape) and drops every
other shape, such as slash arcs, sparkles, fireballs or a stray second head (session 15: PixelLab paints effects into
attack frames; the game draws hit effects itself). A shape that touches the figure's box and is at least --keep pixels
(a weapon tip drawn apart from the hand) is kept. Originals are copied to tools/art/takes/map_raw/ first.

Usage (from the repo root):
    python tools/art/keep_figure.py <sprite_id> [...] [--anims attack] [--rotations] [--pale] [--battle] [--dry]
--pale also erases bright pixels whose colour is far from every colour of the still rotations (slash arcs, sparks
and spell light joined to a weapon).
"""
from __future__ import annotations

import argparse
import shutil
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
UNITS = ROOT / "assets" / "sprites" / "units"   # --battle switches to assets/sprites/battle
RAW = Path(__file__).with_name("takes") / "map_raw"


def shapes(im: Image.Image) -> list[set[tuple[int, int]]]:
    px = im.load()
    w, h = im.size
    seen, found = set(), []
    for y in range(h):
        for x in range(w):
            if px[x, y][3] < 32 or (x, y) in seen:
                continue
            stack, shape = [(x, y)], set()
            seen.add((x, y))
            while stack:
                cx, cy = stack.pop()
                shape.add((cx, cy))
                for dx in (-1, 0, 1):
                    for dy in (-1, 0, 1):
                        nx, ny = cx + dx, cy + dy
                        if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in seen and px[nx, ny][3] >= 32:
                            seen.add((nx, ny))
                            stack.append((nx, ny))
            found.append(shape)
    return sorted(found, key=len, reverse=True)


def palette(sid: str) -> list[tuple[int, int, int]]:
    """The colours of the character's still rotations: anything bright and far from these is a painted effect."""
    cols = set()
    for f in (UNITS / sid / "rotations").glob("*.png"):
        im = Image.open(f).convert("RGBA")
        cols |= {c[:3] for _, c in im.getcolors(1 << 16) if c[3] >= 32}
    return sorted(cols)


def erase_pale(im: Image.Image, pal: list[tuple[int, int, int]], light: int, far: int) -> int:
    """Erases bright pixels (max channel >= light) whose colour is further than `far` from every palette colour."""
    px = im.load()
    removed = 0
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a < 32 or max(r, g, b) < light:
                continue
            if min((r - pr) ** 2 + (g - pg) ** 2 + (b - pb) ** 2 for pr, pg, pb in pal) > far * far:
                px[x, y] = (0, 0, 0, 0)
                removed += 1
    return removed


def erase_arcs(im: Image.Image, edge_share: float = 0.6) -> int:
    """Erases floating near-white patches (swing trails, rings): 4-connected groups of near-white pixels whose outside
    neighbours are mostly transparent. White fur or steel is edged by the figure's dark outline instead."""
    px = im.load()
    w, h = im.size
    white = lambda x, y: px[x, y][3] >= 32 and min(px[x, y][:3]) >= 200
    seen, removed = set(), 0
    for y in range(h):
        for x in range(w):
            if (x, y) in seen or not white(x, y):
                continue
            stack, group = [(x, y)], []
            seen.add((x, y))
            empty = edge = 0
            while stack:
                cx, cy = stack.pop()
                group.append((cx, cy))
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if not (0 <= nx < w and 0 <= ny < h) or px[nx, ny][3] < 32:
                        empty += 1
                        edge += 1
                    elif white(nx, ny):
                        if (nx, ny) not in seen:
                            seen.add((nx, ny))
                            stack.append((nx, ny))
                    else:
                        edge += 1
            if edge and empty / edge >= edge_share:
                for gx, gy in group:
                    px[gx, gy] = (0, 0, 0, 0)
                removed += len(group)
    return removed


def clean(path: Path, keep: int, dry: bool, pal=None, light=170, far=48, arcs=False) -> int:
    im = Image.open(path).convert("RGBA")
    pale = erase_pale(im, pal, light, far) if pal else 0
    pale += erase_arcs(im) if arcs else 0
    found = shapes(im)
    if len(found) < 2 and not pale:
        return 0
    main = found[0]
    xs, ys = [p[0] for p in main], [p[1] for p in main]
    box = (min(xs) - 1, min(ys) - 1, max(xs) + 1, max(ys) + 1)
    near = lambda s: any(box[0] <= x <= box[2] and box[1] <= y <= box[3] for x, y in s)
    drop = [s for s in found[1:] if not (len(s) >= keep and near(s))]
    removed = sum(len(s) for s in drop) + pale
    if removed and not dry:
        raw = RAW / path.relative_to(UNITS)
        if not raw.exists():
            raw.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, raw)
        px = im.load()
        for s in drop:
            for x, y in s:
                px[x, y] = (0, 0, 0, 0)
        im.save(path)
    return removed


def bright(path: Path) -> int:
    im = Image.open(path).convert("RGBA")
    return sum(1 for r, g, b, a in im.getdata() if a >= 32 and max(r, g, b) >= 220)


def drop_bright(folder: Path, extra: int, dry: bool) -> list[str]:
    """Deletes frames whose bright-pixel count is `extra` above the first frame's, then renumbers the rest."""
    frames = sorted(folder.glob("frame_*.png"))
    if len(frames) < 3:
        return []
    base = bright(frames[0])
    bad = [f for f in frames[1:] if bright(f) - base >= extra]
    if len(frames) - len(bad) < 3:
        bad = sorted(bad, key=bright, reverse=True)[:len(frames) - 3]
    if bad and not dry:
        for f in bad:
            raw = RAW / f.relative_to(UNITS)
            if not raw.exists():
                raw.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(f, raw)
            f.unlink()
        for i, f in enumerate(sorted(folder.glob("frame_*.png"))):
            f.rename(folder / f"tmp_{i:03d}.png")
        for f in sorted(folder.glob("tmp_*.png")):
            f.rename(folder / f.name.replace("tmp_", "frame_"))
    return [f.name for f in bad]


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("ids", nargs="+")
    ap.add_argument("--anims", nargs="*", default=["attack"])
    ap.add_argument("--rotations", action="store_true")
    ap.add_argument("--keep", type=int, default=12, help="smallest separate shape kept if it touches the figure")
    ap.add_argument("--dry", action="store_true")
    ap.add_argument("--arcs", action="store_true", help="also erase floating near-white patches (swing trails)")
    ap.add_argument("--battle", action="store_true", help="battle-screen sprites (assets/sprites/battle) instead of map")
    ap.add_argument("--pale", action="store_true", help="also erase bright colours missing from the still rotations")
    ap.add_argument("--drop-bright", type=int, metavar="N",
                    help="drop frames with N+ more bright pixels than the first (an effect in the character's own colours)")
    args = ap.parse_args()
    if args.battle:
        global UNITS, RAW
        UNITS = ROOT / "assets" / "sprites" / "battle"
        RAW = Path(__file__).with_name("takes") / "battle_raw"
    for sid in args.ids:
        files = [f for a in args.anims for f in sorted((UNITS / sid / "animations" / a).rglob("*.png"))]
        if args.rotations:
            files += sorted((UNITS / sid / "rotations").glob("*.png"))
        total = 0
        pal = palette(sid) if args.pale else None
        for f in files:
            n = clean(f, args.keep, args.dry, pal, arcs=args.arcs)
            if n:
                total += n
                print(f"  {f.relative_to(UNITS)}: {n} px")
        print(f"{sid}: {total} px removed{' (dry run)' if args.dry else ''}")
        if args.drop_bright:
            for a in args.anims:
                for folder in sorted((UNITS / sid / "animations" / a).glob("*")):
                    if dropped := drop_bright(folder, args.drop_bright, args.dry):
                        print(f"  {a}/{folder.name}: dropped {', '.join(dropped)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
