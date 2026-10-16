"""Still takes for battle sprites in the "E" look (docs/ART_DIRECTION.md §2): 9 takes per character at 84 px.

Usage (from the repo root):
    python tools/art/battle_stills.py <sprite_id> [...]      # ids from manifest.CHARACTERS (uses their desc)

Takes go to tools/art/takes/stills/<sprite_id>/take_N.png. Pick one (a ready stance, facing right), copy it to
tools/art/approved/refs/<sprite_id>_battle.png and run animate_still.py on it. Each request is recorded in state.json
under "gba" / "still_takes" and is not paid for twice. About 15 generations per character.
"""
from __future__ import annotations

import sys
import time

import manifest as M
from generate import REPO_ROOT, TAKES, load_state, save_state
from pixellab_client import PixelLab, PixelLabError, RateLimited, save_base64_image

E_STYLE = ("Game Boy Advance Fire Emblem battle animation sprite, like Fire Emblem: The Blazing Blade, bright "
           "saturated colours, anime proportions, 3-tone cel shading, crisp dark outline, clean readable pixels")


def main() -> int:
    ids = sys.argv[1:]
    unknown = [i for i in ids if i not in M.CHARACTERS]
    if not ids or unknown:
        print(__doc__ if not ids else f"Unknown ids: {unknown}")
        return 1
    client = PixelLab()
    state = load_state()
    takes = state["gba"].setdefault("still_takes", {})

    for sid in ids:
        if takes.get(sid, {}).get("status") in ("processing", "completed"):
            continue
        body = {"description": f"{M.CHARACTERS[sid]['desc']}, full body, side view facing right. {E_STYLE}",
                "image_size": {"width": 84, "height": 84}, "no_background": True,
                "target_palette": {"max_colors": 16}}
        while True:
            try:
                response = client.post("/generate-image-v2", body)
                break
            except RateLimited:
                time.sleep(20)
        takes[sid] = {"status": "processing", "job": response["background_job_id"]}
        save_state(state)
        print(f"  submitted {sid}")

    for sid in ids:
        entry = takes[sid]
        if entry["status"] != "processing":
            continue
        try:
            job = client.wait_for_job(entry["job"], poll_seconds=6)
            time.sleep(3)  # the job can report completed a moment before its images are attached
            job = client.get(f"/background-jobs/{entry['job']}")
        except PixelLabError as e:
            print(f"  ! {sid}: {e}")
            entry["status"] = "failed"
            save_state(state)
            continue
        images = (job.get("last_response") or {}).get("images") or []
        for i, image in enumerate(images):
            save_base64_image(image if isinstance(image, dict) else {"base64": image}, TAKES / "stills" / sid / f"take_{i}.png")
        entry.update(status="completed" if images else "failed", usage=job.get("usage"), takes=len(images))
        save_state(state)
        print(f"  {sid}: {len(images)} takes {job.get('usage')}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
