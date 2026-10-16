"""Turns one chosen still into a battle sprite by animating the still itself (POST /animate-with-text-v3).

Building a PixelLab character from a still (create_from_concept) redraws it and loses the look, so battle sprites
picked from still takes are animated directly instead. Output is the layout SpriteLibrary expects:
    assets/sprites/battle/<sprite_id>/rotations/east.png
    assets/sprites/battle/<sprite_id>/animations/<name>/east/frame_NNN.png

Usage (from the repo root):
    python tools/art/animate_still.py <still.png> <sprite_id> --weapon sword [--only attack critical]

The still must face right on a transparent background. It is placed on a wider canvas (--width, default 128) so
lunges have room; the height is kept so the battle screen draws it 1:1. Each request is recorded in state.json
under "gba" / "stills" and is not paid for twice; delete the entry to redo one. About 1 generation per animation.
"""
from __future__ import annotations

import argparse
import base64
import io
import sys
import time

from PIL import Image

import manifest as M
from generate import REPO_ROOT, load_state, save_state
from pixellab_client import PixelLab, PixelLabError, RateLimited


# The still faces right, so "backward" is ambiguous to the model: spell out left and right. Animations that end back
# in stance also get the still as their last frame.
ACTIONS = {
    "idle": ("stands in a battle-ready stance, weapon at the ready, subtle breathing, barely moves", True),
    "hit": ("is struck hard from the right: flinches and is knocked back a step to the LEFT, away from the enemy, "
            "then steadies and returns to the same stance", True),
    "dodge": ("quickly hops back to the LEFT, away from the enemy, leaning away from an attack, then steps back to "
              "the same stance", True),
}
ATTACKS = {  # weapon: attack ending back in stance (the critical keeps the manifest's text and ends mid-strike)
    "sword": "steps forward to the RIGHT and swings the sword in a wide fast slash from high behind to low in front, "
             "then returns to the same stance",
}
RIGHT = "faces right the whole time, side view, never turns toward the camera"
CLEAN = "no effects, no particles, no motion lines, no dust"  # the battle screen draws hit flashes itself


def without_effects(frames: list[Image.Image]) -> list[Image.Image]:
    """Drops frames where the model painted an effect (a splash, slash or sparkle) over the sprite: the sprite's area
    grows by more than 12%, or more than 30 near-white pixels appear that the first frame doesn't have. The battle
    screen draws hit flashes itself, so idle, hit and dodge should be the body only."""
    def opaque(im):
        return [p for p in im.get_flattened_data() if p[3] > 128]
    base = opaque(frames[0])
    colours = {p[:3] for p in base}
    keep = []
    for im in frames:
        px = opaque(im)
        white = sum(1 for p in px if min(p[:3]) > 215 and p[:3] not in colours)
        if len(px) <= 1.12 * len(base) and white <= 30:
            keep.append(im)
    return keep


def padded(still: Image.Image, width: int, left: int) -> Image.Image:
    canvas = Image.new("RGBA", (width, still.height), (0, 0, 0, 0))
    canvas.paste(still, (left, 0), still)
    return canvas


def b64png(image: Image.Image) -> dict:
    buffer = io.BytesIO()
    image.save(buffer, "PNG")
    return {"type": "base64", "base64": base64.b64encode(buffer.getvalue()).decode(), "format": "png"}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("still")
    parser.add_argument("sprite_id")
    parser.add_argument("--weapon", required=True, choices=sorted(M.BATTLE_ATTACKS))
    parser.add_argument("--width", type=int, default=128)
    parser.add_argument("--left", type=int, default=16, help="x offset of the still on the wider canvas")
    parser.add_argument("--only", nargs="*")
    parser.add_argument("--redo", nargs="*", default=[], help="animations to request again (pays again)")
    parser.add_argument("--clean-only", action="store_true", help="re-run the effect filter on saved frames; never spends")
    args = parser.parse_args()

    still = Image.open(args.still).convert("RGBA")
    first = padded(still, args.width, args.left)
    out = REPO_ROOT / "assets" / "sprites" / "battle" / args.sprite_id
    (out / "rotations").mkdir(parents=True, exist_ok=True)
    first.save(out / "rotations" / "east.png")

    if args.clean_only:
        for name in ("idle", "hit", "dodge"):
            frames = out / "animations" / name / "east"
            paths = sorted(frames.glob("frame_*.png"))
            kept = without_effects([Image.open(p).convert("RGBA") for p in paths])
            for p in paths:
                p.unlink()
                p.with_name(p.name + ".import").unlink(missing_ok=True)
            for i, im in enumerate(kept):
                im.save(frames / f"frame_{i:03d}.png")
            print(f"  {args.sprite_id}/{name}: kept {len(kept)} of {len(paths)} frames")
        return 0

    client = PixelLab()
    state = load_state()
    entries = state["gba"].setdefault("stills", {}).setdefault(args.sprite_id, {})
    wanted = {n: a for n, a in M.battle_anims(args.weapon).items() if not args.only or n in args.only}

    for name, anim in wanted.items():
        entry = entries.get(name, {})
        if entry.get("status") in ("processing", "completed") and name not in args.redo:
            continue
        action, to_stance = ACTIONS.get(name, (None, False))
        if name == "attack":
            action, to_stance = ATTACKS.get(args.weapon, M.BATTLE_ATTACKS[args.weapon][0]), True
        if action and name in ("hit", "dodge", "idle"):
            action = f"{action}. Keeps holding the {args.weapon} the whole time. {CLEAN}"
        body = {"first_frame": b64png(first), "action": f"{action}. {RIGHT}" if action else anim["action"],
                "frame_count": anim["frames"], "no_background": True}
        if to_stance:
            body["last_frame"] = b64png(first)
        while True:
            try:
                response = client.post("/animate-with-text-v3", body)
                break
            except RateLimited:
                time.sleep(20)
        entries[name] = {"status": "processing", "job": response["background_job_id"], "still": args.still}
        save_state(state)
        print(f"  submitted {args.sprite_id}/{name}")

    for name in wanted:
        entry = entries[name]
        if entry["status"] != "processing":
            continue
        try:
            job = client.wait_for_job(entry["job"], poll_seconds=5)
        except PixelLabError as e:
            print(f"  ! {name}: {e}")
            entry["status"] = "failed"
            save_state(state)
            continue
        images = (job.get("last_response") or {}).get("images") or []
        frames = out / "animations" / name / "east"
        for old in frames.glob("frame_*.png"):
            old.unlink()
            old.with_name(old.name + ".import").unlink(missing_ok=True)
        decoded = [Image.open(io.BytesIO(base64.b64decode((image if isinstance(image, dict) else {"base64": image})
                                                          ["base64"].split(",")[-1]))).convert("RGBA")
                   for image in images]
        if name in ("idle", "hit", "dodge"):
            decoded = without_effects(decoded)
        frames.mkdir(parents=True, exist_ok=True)
        for i, im in enumerate(decoded):
            im.save(frames / f"frame_{i:03d}.png")
        entry.update(status="completed", usage=job.get("usage"), frames=len(decoded))
        save_state(state)
        print(f"  {name}: {len(decoded)} of {len(images)} frames kept {job.get('usage')}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
