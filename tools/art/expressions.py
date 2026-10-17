"""Portrait expressions: edits each base portrait (assets/portraits/<who>.png) into <who>_<expression>.png.

One request per expression (POST /edit-images-v2, edit_with_text) carries every portrait that needs it, so the same
instruction is applied to several characters at once. The dialogue box picks <who>_<expression>.png, then the nearest
expression, then the base portrait (DialogueBox.Portrait). Chosen from how often each speaker uses each expression in
data/dialogue (the base portrait already covers "neutral").

Usage (from the repo root):
    python tools/art/expressions.py [--only worried smile ...]
Each request is recorded in state.json under "gba" / "expressions" and is not paid for twice. Takes are kept in
tools/art/takes/portraits/expressions/.
"""
from __future__ import annotations

import argparse
import base64
import io
import sys
import time

from PIL import Image

from generate import REPO_ROOT, TAKES, load_state, save_state
from pixellab_client import PixelLab, PixelLabError, RateLimited

PORTRAITS = REPO_ROOT / "assets" / "portraits"
KEEP = ("Change only the face to show this expression. Keep the same character, hair, clothes, pose, framing, colours, "
        "outline and pixel style exactly; same background")
EXPRESSIONS = {
    "determined": ("a determined, resolute expression: firm brows, steady eyes, mouth set in a line", ["rowan", "aldric", "kai"]),
    "worried": ("a worried, anxious expression: brows raised and drawn together, mouth slightly open", ["rowan", "mira", "elise"]),
    "surprised": ("a surprised expression: wide eyes, raised brows, mouth open", ["rowan", "kai", "brann", "elise"]),
    "smile": ("a warm smile: relaxed brows, eyes soft, a clear friendly smile", ["rowan", "kai", "mira", "brann"]),
    "cold": ("a cold, hard expression: narrowed eyes, flat mouth, unfriendly", ["aldric", "tamsin"]),
    "sad": ("a sad expression: brows tilted up in the middle, eyes lowered, mouth turned down", ["aldric"]),
    "laugh": ("laughing: eyes squeezed into happy curves, mouth open in a big grin", ["kai"]),
}


def b64png(image: Image.Image) -> dict:
    buffer = io.BytesIO()
    image.save(buffer, "PNG")
    return {"type": "base64", "base64": base64.b64encode(buffer.getvalue()).decode(), "format": "png"}


def base_mask(edited: Image.Image, base: Image.Image) -> Image.Image:
    """The pose is unchanged, so the base portrait's transparency is copied over (edits come back on white)."""
    edited.putalpha(base.getchannel("A"))
    return edited


def clear_bottom_bar(image: Image.Image) -> Image.Image:
    """Edited portraits can bring back the light bar along the bottom rows that the base portraits had removed."""
    px = image.load()
    w, h = image.size
    for y in range(h - 4, h):
        row = [px[x, y] for x in range(w)]
        if sum(1 for p in row if p[3] > 0 and min(p[:3]) > 170) > w * 0.6:
            for x in range(w):
                px[x, y] = (0, 0, 0, 0)
    return image


def remask_existing() -> None:
    """Re-applies the base transparency to every installed expression (free; for edits made before base_mask)."""
    for path in PORTRAITS.glob("*_*.png"):
        who = path.stem.split("_")[0]
        if (PORTRAITS / f"{who}.png").exists() and path.stem.split("_", 1)[1] in EXPRESSIONS:
            base = Image.open(PORTRAITS / f"{who}.png").convert("RGBA")
            base_mask(Image.open(path).convert("RGBA"), base).save(path)
            print("  remasked", path.name)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--only", nargs="*")
    parser.add_argument("--remask", action="store_true", help="re-apply base transparency to installed expressions; never spends")
    args = parser.parse_args()
    if args.remask:
        remask_existing()
        return 0
    client = PixelLab()
    state = load_state()
    done = state["gba"].setdefault("expressions", {})
    takes = TAKES / "portraits" / "expressions"
    takes.mkdir(parents=True, exist_ok=True)

    for expression, (look, who) in EXPRESSIONS.items():
        if args.only and expression not in args.only or done.get(expression, {}).get("status") == "completed":
            continue
        bases = [Image.open(PORTRAITS / f"{w}.png").convert("RGBA") for w in who]
        size = bases[0].size
        body = {"method": "edit_with_text", "description": f"{KEEP}. New expression: {look}.",
                "edit_images": [{"image": b64png(b), "width": size[0], "height": size[1]} for b in bases],
                "image_size": {"width": size[0], "height": size[1]}, "no_background": True}
        while True:
            try:
                response = client.post("/edit-images-v2", body)
                break
            except RateLimited:
                time.sleep(20)
        try:
            job = client.wait_for_job(response["background_job_id"], poll_seconds=5)
        except PixelLabError as e:
            print(f"  ! {expression}: {e}")
            continue
        images = (job.get("last_response") or {}).get("images") or []
        for name, base, image in zip(who, bases, images):
            data = (image if isinstance(image, dict) else {"base64": image})["base64"].split(",")[-1]
            edited = Image.open(io.BytesIO(base64.b64decode(data))).convert("RGBA").resize(size, Image.NEAREST)
            edited = clear_bottom_bar(base_mask(edited, base))
            edited.save(takes / f"{name}_{expression}.png")
            edited.save(PORTRAITS / f"{name}_{expression}.png")
        done[expression] = {"status": "completed", "usage": job.get("usage"), "who": who}
        save_state(state)
        print(f"  {expression}: {len(images)} portraits {job.get('usage')}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
