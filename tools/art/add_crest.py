"""Makes a "<sprite_id>_crest" copy of a unit's art with Rowan's crest glowing on his hand (POST /edit-animation-v2).

The game shows the "_crest" art once his crest awakens (BattleUnit.ShowsCrest). Edits every rotation and animation
frame in batches (16 frames per request up to 64 px, 4 above 80 px), so the look stays consistent.

Usage (from the repo root):
    python tools/art/add_crest.py <sprite_id> [--set map|battle] [--only ANIM ...]
"""
from __future__ import annotations

import argparse
import base64
import io
import shutil
import sys
import time
from pathlib import Path

from PIL import Image

from generate import REPO_ROOT, load_state, save_state
from pixellab_client import PixelLab, PixelLabError, RateLimited

EDIT = ("add a small glowing white crest sigil on the back of his right hand, a soft white light; keep everything "
        "else exactly the same: same pose, same colours, same outline, same size")
ROOTS = {"map": REPO_ROOT / "assets/sprites/units", "battle": REPO_ROOT / "assets/sprites/battle"}


def b64png(path: Path) -> dict:
    buffer = io.BytesIO()
    Image.open(path).convert("RGBA").save(buffer, "PNG")
    return {"type": "base64", "base64": base64.b64encode(buffer.getvalue()).decode(), "format": "png"}


def b64crop(path: Path, box) -> dict:
    buffer = io.BytesIO()
    Image.open(path).convert("RGBA").crop(box).save(buffer, "PNG")
    return {"type": "base64", "base64": base64.b64encode(buffer.getvalue()).decode(), "format": "png"}


def groups(folder: Path):
    """(name, frame paths) for the rotations and each animation direction."""
    rotations = sorted((folder / "rotations").glob("*.png"))
    if rotations:
        yield "rotations", rotations
    # All directions of an animation go together, so more frames fit each request (each costs 10 generations).
    for anim in sorted((folder / "animations").glob("*")):
        frames = sorted(anim.glob("*/frame_*.png"))
        if frames:
            yield anim.name, frames


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("sprite_id")
    parser.add_argument("--set", choices=list(ROOTS), action="append")
    parser.add_argument("--only", nargs="*")
    parser.add_argument("--redo", action="store_true",
                        help="edit the chosen animations again (pays again), clearing their old _crest frames first")
    args = parser.parse_args()
    client = PixelLab()
    state = load_state()
    done = state["gba"].setdefault("crest_edits", {})

    for set_name in args.set or list(ROOTS):
        src = ROOTS[set_name] / args.sprite_id
        dst = ROOTS[set_name] / f"{args.sprite_id}_crest"
        if not src.exists():
            continue
        if not dst.exists():
            shutil.copytree(src, dst, ignore=shutil.ignore_patterns("*.import"))
        for name, frames in groups(src):
            if args.only and name.split("/")[0] not in args.only:
                continue
            if args.redo:
                # The source may have a different frame count now: drop the old copies so none are left over.
                for folder in {f.parent for f in frames}:
                    for old in (dst / folder.relative_to(src)).glob("frame_*.png*"):
                        old.unlink()
            # Crop the group to the union of its content: smaller frames fit more per request (16 up to 64 px,
            # 9 up to 80, else 4). Results are pasted back at the same offset.
            full = Image.open(frames[0]).size
            box = None
            for f in frames:
                bb = Image.open(f).convert("RGBA").getbbox() or (0, 0, 1, 1)
                box = bb if box is None else (min(box[0], bb[0]), min(box[1], bb[1]), max(box[2], bb[2]), max(box[3], bb[3]))
            pad = 2
            box = (max(0, box[0] - pad), max(0, box[1] - pad), min(full[0], box[2] + pad), min(full[1], box[3] + pad))
            w, h = max(16, box[2] - box[0]), max(16, box[3] - box[1])
            w += -w % 4
            h += -h % 4
            box = (box[0], box[1], min(full[0], box[0] + w), min(full[1], box[1] + h))
            w, h = box[2] - box[0], box[3] - box[1]
            batch = 16 if max(w, h) <= 64 else 9 if max(w, h) <= 80 else 4
            # A lone frame (a battle rotation) is sent with a copy of itself: the endpoint needs two or more.
            work = frames if len(frames) > 1 else frames * 2
            for start in range(0, len(work), batch):
                chunk = work[start:start + batch]
                if len(chunk) == 1:
                    chunk = chunk * 2
                key = f"{set_name}:{args.sprite_id}:{name}:{start}"
                if done.get(key) == "completed" and not args.redo:
                    continue
                body = {"description": EDIT, "frames": [{"image": b64crop(f, box), "size": {"width": w, "height": h}} for f in chunk],
                        "image_size": {"width": w, "height": h}, "no_background": True}
                while True:
                    try:
                        response = client.post("/edit-animation-v2", body)
                        break
                    except RateLimited:
                        time.sleep(20)
                try:
                    job = client.wait_for_job(response["background_job_id"], poll_seconds=5)
                except PixelLabError as e:
                    print(f"  ! {key}: {e}")
                    continue
                images = (job.get("last_response") or {}).get("images") or []
                for frame, image in zip(chunk, images):
                    data = (image if isinstance(image, dict) else {"base64": image})["base64"].split(",")[-1]
                    out = dst / frame.relative_to(src)
                    out.parent.mkdir(parents=True, exist_ok=True)
                    edited = Image.open(io.BytesIO(base64.b64decode(data))).convert("RGBA").resize((w, h), Image.NEAREST)
                    canvas = Image.new("RGBA", full, (0, 0, 0, 0))
                    canvas.paste(edited, box[:2])
                    canvas.save(out)
                done[key] = "completed"
                save_state(state)
                print(f"  {key}: {len(images)} frames {job.get('usage')}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
