"""Requests the game's art from PixelLab and downloads it into assets/.

Usage (from the repo root):
    python tools/art/generate.py balance              # check the token and remaining credits
    python tools/art/generate.py plan                 # list what would be requested (no spending)
    python tools/art/generate.py download             # fetch finished art only; never submits (or spends) anything
    python tools/art/generate.py run --pilot          # one character with all its animations, to check the style
    python tools/art/generate.py run                  # everything not yet generated
    python tools/art/generate.py run --only rowan kai # just these sprite ids (characters/portraits)
    python tools/art/generate.py run --skip-animations / --skip-portraits / --skip-tiles
    python tools/art/generate.py run --reserve 100    # stop submitting when fewer generations remain (default 50)
    python tools/art/generate.py run --set battle     # side-view battle-screen sprites instead of map sprites

Create an empty tools/art/STOP file to stop new runs from starting (e.g. to pause a queued batch).

Progress is recorded in tools/art/state.json after every step, so an interrupted run resumes
without re-submitting (and re-paying for) anything that was already requested.
"""
from __future__ import annotations

import argparse
import json
import os
import sys
import time
from pathlib import Path

import manifest as M
from pixellab_client import REPO_ROOT, PixelLab, PixelLabError, RateLimited, save_base64_image

STATE_PATH = Path(__file__).with_name("state.json")
# The active art set (manifest.SETS[...]); chosen by --set in main().
S: dict = M.SETS["map"]
PORTRAITS = REPO_ROOT / "assets" / "portraits"
TILES = REPO_ROOT / "assets" / "textures" / "terrain"
PILOT = "rowan"


# ------------------------------------------------------------------ state

def load_state() -> dict:
    if STATE_PATH.exists():
        return json.loads(STATE_PATH.read_text(encoding="utf-8"))
    return {"characters": {}, "portraits": {}, "tiles": {}}


def save_state(state: dict) -> None:
    """Atomic write with retries: OneDrive briefly locks synced files, which surfaces as OSError 22/13."""
    data = json.dumps(state, indent=2, sort_keys=True) + "\n"
    tmp = STATE_PATH.with_suffix(".json.tmp")
    for attempt in range(10):
        try:
            tmp.write_text(data, encoding="utf-8")
            os.replace(tmp, STATE_PATH)
            return
        except OSError as e:
            if attempt == 9:
                raise
            print(f"    state.json busy ({e.strerror}); retrying")
            time.sleep(1 + attempt)


# ------------------------------------------------------------------ request bodies

def character_name(sprite_id: str) -> str:
    return sprite_id if S is M.SETS["map"] else f"{sprite_id}_battle"


def find_existing_character(client: PixelLab, name: str) -> str | None:
    """A character created on PixelLab but never recorded (e.g. a crash before the state was saved)."""
    listing = client.get("/characters?limit=100")
    items = listing.get("characters", []) if isinstance(listing, dict) else listing
    for item in items:
        if item.get("name") == name and item.get("status") != "failed":
            return item.get("id")
    return None


def character_body(sprite_id: str, spec: dict) -> dict:
    return {
        "description": f"{spec['description']}, {S['style']}",
        "name": character_name(sprite_id),
        "image_size": {"width": S["size"], "height": S["size"]},
        "view": S["view"],
        "template_id": spec["body"]["template_id"],
        "no_background": True,
        "outline": "single color black outline",
        "detail": "medium detail",
    }


def animation_body(character_id: str, name: str, anim: dict, directions: list[str], group_id: str | None) -> dict:
    body = {"character_id": character_id, "animation_name": name, "directions": directions}
    if group_id:
        body["animation_group_id"] = group_id
    if anim["kind"] == "template":
        body.update(mode="template", template_animation_id=anim["template"])
    else:
        body.update(mode="v3", action_description=anim["action"], frame_count=anim["frames"])
    return body


# ------------------------------------------------------------------ plan

def plan(selected: list[str], args) -> None:
    gens_per_char = 1 + -(-S['size'] * S['size'] * 8 // 65536)
    total_template = total_v3 = 0
    print(f"Characters ({len(selected)}), {S['size']}px, view '{S['view']}', directions {S['directions']}:")
    for sid in selected:
        spec = S['specs'][sid]
        anims = [] if args.skip_animations else list(spec["animations"].items())
        kinds = ", ".join(f"{n}({a['kind']})" for n, a in anims)
        print(f"  {sid:22} ~{gens_per_char} gens + animations: {kinds}")
        for _, a in anims:
            if a["kind"] == "template":
                total_template += len(S['directions'])
            else:
                total_v3 += len(S['directions'])
    if S is not M.SETS["map"]:
        args.skip_portraits = args.skip_tiles = True
    portraits = [] if args.skip_portraits else [p for p in M.PORTRAITS if p in selected or not args.only]
    print(f"Portraits ({len(portraits)}), {M.PORTRAIT_SIZE}px: {', '.join(portraits)}")
    print(f"Terrain tiles: {'skipped' if args.skip_tiles or args.only else ', '.join(t for t, _ in M.TERRAIN_TILES)}")
    print()
    print("Estimated cost (PixelLab generations):")
    print(f"  characters      ~{gens_per_char * len(selected)}")
    print(f"  template anims  ~{total_template} (1 per direction)")
    print(f"  v3 anims        {total_v3} direction jobs (cost per job is reported after the pilot run)")
    print(f"  portraits       ~{len(portraits)}")
    print("Run `generate.py run --pilot` first to check the style and the real per-job cost.")


# ------------------------------------------------------------------ budget

class BudgetExhausted(Exception):
    pass


def remaining_generations(client: PixelLab) -> float:
    balance = client.balance()
    gens = (balance.get("subscription") or {}).get("generations") or 0
    usd = (balance.get("credits") or {}).get("usd") or 0
    return gens + (1e9 if usd > 0 else 0)  # paid credits mean no hard generation cap


def check_budget(client: PixelLab, args, what: str) -> None:
    left = remaining_generations(client)
    if left < args.reserve:
        raise BudgetExhausted(f"stopping before {what}: {left:.0f} generations left (reserve {args.reserve})")


# ------------------------------------------------------------------ run

def run(client: PixelLab, state: dict, selected: list[str], args) -> None:
    chars = state.setdefault(S["state_key"], {})
    if S is not M.SETS["map"]:
        args.skip_portraits = args.skip_tiles = True

    # 1. Submit missing characters (waiting for running ones when throttled), then wait for all of them.
    def wait_characters() -> None:
        for sid in selected:
            entry = chars.get(sid, {})
            if "character_id" in entry and entry.get("status") != "completed":
                print(f"[{sid}] waiting for character {entry['character_id']}")
                job = None
                if entry.get("job"):
                    try:
                        job = client.wait_for_job(entry["job"])
                        entry["usage"] = job.get("usage") or entry.get("usage")
                    except PixelLabError as e:
                        if "-> 404" not in str(e):
                            raise
                        # Old job records expire server-side; the character itself still says whether it's done.
                        print(f"[{sid}] job {entry['job']} expired; polling the character instead")
                if job is None:
                    while client.get(f"/characters/{entry['character_id']}").get("status") == "pending":
                        time.sleep(8)
                entry["status"] = "completed"
                save_state(state)

    for sid in selected:
        entry = chars.setdefault(sid, {})
        for attempt in range(5):
            if "character_id" in entry:
                break
            existing = find_existing_character(client, character_name(sid))
            if existing:
                print(f"[{sid}] adopting unrecorded character {existing}")
                entry.update(character_id=existing, status="adopted")
                save_state(state)
                break
            check_budget(client, args, f"character {sid}")
            print(f"[{sid}] creating character")
            try:
                response = client.post("/create-character-v3", character_body(sid, S["specs"][sid]))
            except RateLimited:
                print("    throttled; waiting for running characters before retrying")
                wait_characters()
                continue
            entry.update(character_id=response["character_id"], job=response["background_job_id"], status="processing",
                         usage=response.get("usage"))
            save_state(state)
    wait_characters()

    # 2. Animations, in priority order. PixelLab caps concurrent jobs, so when a request is throttled or
    #    only some directions are accepted, wait for the running jobs and then ask for the missing ones.
    if not args.skip_animations:
        queue = [(name, sid) for name in S["priority"] for sid in selected
                 if name in S["specs"][sid]["animations"]]
        for name, sid in queue:
            for attempt in range(8):
                anim = chars[sid].setdefault("animations", {}).setdefault(name, {"jobs": [], "directions": []})
                anim.setdefault("directions", [])
                missing = [d for d in S["directions"] if d not in anim["directions"]]
                if not missing:
                    break
                check_budget(client, args, f"{sid} '{name}' animation")
                print(f"[{sid}] requesting '{name}' {missing}")
                try:
                    response = client.post("/characters/animations", animation_body(
                        chars[sid]["character_id"], name, S["specs"][sid]["animations"][name], missing, anim.get("group_id")))
                except RateLimited:
                    response = None
                if response:
                    anim["group_id"] = anim.get("group_id") or response.get("animation_group_id")
                    anim["jobs"] += response["background_job_ids"]
                    anim["directions"] += response.get("directions") or missing[:len(response["background_job_ids"])]
                    anim["status"] = "processing"
                    save_state(state)
                if [d for d in S["directions"] if d not in anim["directions"]]:
                    print("    throttled; waiting for running jobs before retrying")
                    wait_for_animations(client, state, chars)
        wait_for_animations(client, state, chars)

    # 3. Download rotations and animation frames.
    for sid in selected:
        download_character(client, sid, chars[sid])

    # 4. Portraits (synchronous endpoint).
    if not args.skip_portraits:
        for sid, prompt in M.PORTRAITS.items():
            if args.only and sid not in selected:
                continue
            if args.pilot and sid != PILOT:
                continue
            if state["portraits"].get(sid, {}).get("status") == "completed":
                continue
            check_budget(client, args, f"{sid} portrait")
            print(f"[{sid}] portrait")
            response = client.post("/create-image-pixen", {
                "description": f"{prompt}, {M.PORTRAIT_STYLE}",
                "image_size": {"width": M.PORTRAIT_SIZE, "height": M.PORTRAIT_SIZE},
                "no_background": True,
                "outline": "single color black outline",
                "detail": "highly detailed",
            })
            save_base64_image(response["image"], PORTRAITS / f"{sid}.png")
            state["portraits"][sid] = {"status": "completed", "usage": response.get("usage")}
            save_state(state)

    # 5. Terrain tiles (one request for all terrain types).
    if not args.skip_tiles and not args.only and not args.pilot:
        tiles = state.setdefault("tiles", {})
        if "tile_id" not in tiles:
            check_budget(client, args, "terrain tiles")
            print("[terrain] requesting tiles")
            numbered = " ".join(f"{i + 1}). {prompt} tile" for i, (_, prompt) in enumerate(M.TERRAIN_TILES))
            response = client.post("/create-tiles-pro", {
                "description": f"{numbered}. {M.TERRAIN_STYLE}",
                "tile_type": "square_topdown",
                "tile_size": M.TERRAIN_TILE_SIZE,
                "tile_view": "top-down",
                "outline_mode": "segmentation",
            })
            tiles.update(tile_id=response["tile_id"], job=response["background_job_id"], status="processing")
            save_state(state)
        if tiles.get("status") != "completed":
            print("[terrain] waiting for tiles")
            client.wait_for_job(tiles["job"])
            result = client.get(f"/tiles-pro/{tiles['tile_id']}")
            urls = result.get("storage_urls", {})
            for i, (terrain_id, _) in enumerate(M.TERRAIN_TILES):
                url = urls.get(f"tile_{i}")
                if url:
                    client.download(url, TILES / f"{terrain_id}.png")
                else:
                    print(f"    ! no tile_{i} for {terrain_id} (got {len(urls)} tiles)")
            for key, url in urls.items():  # keep any extra variations too
                client.download(url, TILES / "variations" / f"{key}.png")
            tiles.update(status="completed", usage=result.get("usage"))
            save_state(state)

    print("Done. Spent so far (per asset) is recorded in tools/art/state.json.")
    print("Balance:", json.dumps(client.balance()))


def wait_for_animations(client: PixelLab, state: dict, chars: dict) -> None:
    for sid, entry in chars.items():
        for name, anim in entry.get("animations", {}).items():
            done = anim.setdefault("done_jobs", [])
            for job_id in anim.get("jobs", []):
                if job_id in done:
                    continue
                try:
                    job = client.wait_for_job(job_id)
                    anim.setdefault("usage", []).append(job.get("usage"))
                except PixelLabError as e:
                    print(f"    ! [{sid}] '{name}': {e}")
                    # Forget the failed direction so the next run requests it again.
                    i = anim["jobs"].index(job_id)
                    if i < len(anim.get("directions", [])):
                        anim["directions"].pop(i)
                    anim["jobs"].remove(job_id)
                    save_state(state)
                    continue
                done.append(job_id)
                save_state(state)
            complete = all(d in anim.get("directions", []) for d in S["directions"]) and len(done) == len(anim.get("jobs", []))
            anim["status"] = "completed" if complete else "partial"
    save_state(state)


def download_character(client: PixelLab, sid: str, entry: dict) -> None:
    detail = client.get(f"/characters/{entry['character_id']}")
    out = REPO_ROOT / S["out"] / sid
    for direction, url in (detail.get("rotation_urls") or {}).items():
        if url and direction in S["directions"]:
            client.download(url, out / "rotations" / f"{direction}.png")
    names_by_group = {a.get("group_id"): n for n, a in entry.get("animations", {}).items()}
    for group in detail.get("animations", []):
        name = names_by_group.get(group.get("animation_group_id")) or group.get("display_name") or group.get("animation_type")
        for d in group.get("directions", []):
            for i, url in enumerate(d.get("frames", [])):
                client.download(url, out / "animations" / name / d["direction"] / f"frame_{i:03d}.png")
    print(f"[{sid}] downloaded to {out.relative_to(REPO_ROOT)}")


# ------------------------------------------------------------------ main

def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("command", choices=["balance", "plan", "run", "download"])
    parser.add_argument("--set", choices=list(M.SETS), default="map", help="which art set to generate")
    parser.add_argument("--only", nargs="*", default=[], help="sprite ids to process")
    parser.add_argument("--pilot", action="store_true", help=f"only '{PILOT}', with animations and portrait")
    parser.add_argument("--skip-animations", action="store_true")
    parser.add_argument("--skip-portraits", action="store_true")
    parser.add_argument("--skip-tiles", action="store_true")
    parser.add_argument("--reserve", type=float, default=50, help="generations to keep in hand")
    args = parser.parse_args()

    global S
    S = M.SETS[args.set]
    selected = [PILOT] if args.pilot else (args.only or list(S["specs"]))
    unknown = [x for x in selected if x not in S["specs"]]
    if unknown:
        parser.error(f"unknown sprite ids: {unknown}. Known: {list(S['specs'])}")

    if args.command == "plan":
        plan(selected, args)
        return 0
    try:
        client = PixelLab()
        if args.command == "balance":
            print(json.dumps(client.balance(), indent=2))
            return 0
        if args.command == "download":
            state = load_state()
            for sid in selected:
                entry = state.get(S["state_key"], {}).get(sid, {})
                if entry.get("status") == "completed":
                    download_character(client, sid, entry)
                elif entry:
                    print(f"[{sid}] not finished on PixelLab yet ({entry.get('status')}); skipped")
            return 0
        stop_file = Path(__file__).with_name("STOP")
        if stop_file.exists():
            print(f"{stop_file.name} file present; not starting. Delete it to resume generation.")
            return 0
        state = load_state()
        try:
            run(client, state, selected, args)
        except BudgetExhausted as e:
            print(f"budget: {e}")
            for sid in selected:  # still fetch whatever finished
                if state.get(S["state_key"], {}).get(sid, {}).get("status") == "completed":
                    download_character(client, sid, state[S["state_key"]][sid])
        return 0
    except PixelLabError as e:
        print(f"error: {e}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
