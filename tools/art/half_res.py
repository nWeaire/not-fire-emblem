"""Halves pixel art for the 16 px SNES map: 2x2 blocks take their most common opaque colour (not an average, so the
palette stays exact), then a 1 px dark outline is restored round the shape. Usage:
    python tools/art/half_res.py SRC_DIR DST_DIR [--outline]
"""
import sys
from collections import Counter
from pathlib import Path
from PIL import Image


def halve(im: Image.Image, outline: bool) -> Image.Image:
    im = im.convert("RGBA")
    w, h = (im.width + 1) // 2, (im.height + 1) // 2
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    src = im.load()
    for y in range(h):
        for x in range(w):
            block = [src[min(im.width - 1, 2 * x + dx), min(im.height - 1, 2 * y + dy)] for dx in (0, 1) for dy in (0, 1)]
            opaque = [p for p in block if p[3] > 128]
            if len(opaque) >= 2:
                # Prefer the darkest of tied colours: it keeps outlines and features.
                counts = Counter(p[:3] for p in opaque).most_common()
                best = min((c for c, n in counts if n == counts[0][1]), key=lambda c: sum(c))
                out.putpixel((x, y), best + (255,))
    if outline:
        px = out.load()
        dark = (24, 22, 28, 255)
        edge = []
        for y in range(h):
            for x in range(w):
                if px[x, y][3] == 0:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if not (0 <= nx < w and 0 <= ny < h) or px[nx, ny][3] == 0:
                        edge.append((x, y))
                        break
        for x, y in edge:
            r, g, b, _ = px[x, y]
            px[x, y] = dark if r + g + b > 150 else (r, g, b, 255)
    return out


if __name__ == "__main__":
    src, dst = Path(sys.argv[1]), Path(sys.argv[2])
    outline = "--outline" in sys.argv
    dst.mkdir(parents=True, exist_ok=True)
    for f in sorted(src.glob("*.png")):
        halve(Image.open(f), outline).save(dst / f.name)
        print(f.name)
