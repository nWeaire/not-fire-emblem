"""Requests the game's art from PixelLab in the approved GBA style (docs/ART_DIRECTION.md) and downloads it.

Usage (from the repo root):
    python tools/art/generate.py balance                       # token check and generations left
    python tools/art/generate.py plan                          # what would be requested and roughly what it costs
    python tools/art/generate.py chars [--only ID ...] [--set map|battle] [--no-anims]
    python tools/art/generate.py portraits [--only ID ...]     # 4 takes each into tools/art/takes/portraits/
    python tools/art/generate.py backgrounds [--only ID ...]   # assets/backgrounds/<id>.png
    python tools/art/generate.py tiles [--only SET ...]        # 16 tiles each into tools/art/takes/tiles/<set>/
    python tools/art/generate.py download [--only ID ...]      # re-fetch finished characters; never spends

Every request is recorded in tools/art/state.json under "gba" as soon as it is submitted, so an interrupted run
resumes without paying again. Create tools/art/STOP to stop new submissions. --reserve N (default 50) stops
submitting when fewer generations remain.
"""
from __future__ import annotations

import argparse
import base64
import io
import json
import os
import shutil
import sys
import time
from pathlib import Path

import manifest as M
from pixellab_client import REPO_ROOT, PixelLab, PixelLabError, RateLimited, save_base64_image

STATE_PATH = Path(__file__).with_name("state.json")
STOP = Path(__file__).with_name("STOP")
TAKES = Path(__file__).with_name("takes")
SETS = {"map": M.MAP, "battle": M.BATTLE}
MAX_JOBS = 8  # PixelLab Tier 1 concurrency


# ------------------------------------------------------------------ state

def load_state() -> dict:
    state = json.loads(STATE_PATH.read_text(encoding="utf-8")) if STATE_PATH.exists() else {}
    state.setdefault("gba", {})
    for key in ("characters", "portraits", "backgrounds", "tiles", "props"):
        state["gba"].setdefault(key, {})
    return state


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


def b64(path: Path) -> str:
    return base64.b64encode(path.read_bytes()).decode()


def image_size(path: Path) -> dict:
    from PIL import Image
    with Image.open(path) as im:
        return {"width": im.width, "height": im.height}


# ------------------------------------------------------------------ job runner

class Runner:
    """Submits jobs, keeping at most MAX_JOBS in flight, and waits for them. Each job has an on_done callback."""

    def __init__(self, client: PixelLab, state: dict, args):
        self.client, self.state, self.args = client, state, args
        self.running: list[tuple[str, callable]] = []  # (job id, on_done(job))

    def check_budget(self, what: str) -> None:
        if STOP.exists():
            raise SystemExit(f"{STOP.name} present; stopping before {what}")
        bal = self.client.balance()
        left = (bal.get("subscription") or {}).get("generations") or 0
        if (bal.get("credits") or {}).get("usd", 0) <= 0 and left < self.args.reserve:
            raise SystemExit(f"stopping before {what}: {left:.0f} generations left (reserve {self.args.reserve})")

    def submit(self, what: str, endpoint: str, body: dict) -> dict:
        self.check_budget(what)
        while True:
            if len(self.running) >= MAX_JOBS:
                self.wait_oldest()
            try:
                response = self.client.post(endpoint, body)
                print(f"submitted {what}")
                return response
            except RateLimited:
                if not self.running:
                    time.sleep(20)
                else:
                    self.wait_oldest()

    def track(self, job_id: str, on_done) -> None:
        self.running.append((job_id, on_done))

    def wait_oldest(self) -> None:
        job_id, on_done = self.running.pop(0)
        try:
            job = self.client.wait_for_job(job_id, poll_seconds=6)
        except PixelLabError as e:
            if "-> 404" not in str(e):
                print(f"  ! job {job_id}: {e}")
                on_done(None)
                save_state(self.state)
                return
            job = {"status": "expired"}
        on_done(job)
        save_state(self.state)

    def drain(self) -> None:
        while self.running:
            self.wait_oldest()


# ------------------------------------------------------------------ characters

def char_key(set_name: str, sid: str) -> str:
    return f"{set_name}:{sid}"


def run_characters(r: Runner, ids: list[str], sets: list[str], anims: bool) -> None:
    chars = r.state["gba"]["characters"]
    ref = {"type": "base64", "base64": b64(M.STYLE_REF), "format": "png"}

    # 1. Characters (adopted or created).
    for sid in ids:
        spec = M.CHARACTERS[sid]
        for set_name in sets:
            if set_name not in spec["sets"]:
                continue
            key = char_key(set_name, sid)
            entry = chars.setdefault(key, {})
            if "character_id" in entry:
                continue
            adopt = spec.get("adopt", {}).get(set_name)
            if adopt:
                entry.update(character_id=adopt, status="completed", adopted=True)
                for name, group in spec.get("done_anims", {}).get(set_name, {}).items():
                    entry.setdefault("animations", {})[name] = {"group_id": group, "status": "completed",
                                                                "adopted": True, "directions": ["east"]}
                save_state(r.state)
                continue
            s = SETS[set_name]
            response = r.submit(f"{key} character", "/create-character-pro", {
                "description": f"{spec['desc']}. {M.CHAR_STYLE}",
                "image_size": {"width": s["size"], "height": s["size"]}, "method": "create_with_style",
                "view": s["view"], "template_id": spec.get("template", "mannequin"), "reference_image": ref,
                "style_description": M.CHAR_STYLE, "no_background": True})
            entry.update(character_id=response["character_id"], status="processing", usage=response.get("usage"))
            save_state(r.state)

            def done(job, entry=entry, key=key):
                entry["status"] = "completed" if job else "failed"
                if job:
                    entry["usage"] = job.get("usage") or entry.get("usage")
                print(f"  {key}: {entry['status']} {entry.get('usage')}")
            r.track(response["background_job_id"], done)
    r.drain()

    # 2. Animations, east-facing first across everyone so each unit can fight before extras are bought.
    if anims:
        for sid in ids:
            spec = M.CHARACTERS[sid]
            for set_name in sets:
                if set_name not in spec["sets"]:
                    continue
                entry = chars.get(char_key(set_name, sid), {})
                if entry.get("status") != "completed":
                    print(f"  {sid} {set_name}: character not ready; skipping animations")
                    continue
                wanted = M.map_anims(spec["weapon"]) if set_name == "map" else M.battle_anims(spec["weapon"])
                if set_name == "map" and spec.get("template") == "horse" and "walk" in wanted:
                    wanted["walk"] = M.HORSE_WALK  # the horse body names its walk differently
                for name, anim in wanted.items():
                    request_animation(r, entry, f"{char_key(set_name, sid)}/{name}", name, anim,
                                      SETS[set_name]["directions"])
        r.drain()

    # 3. Download.
    for sid in ids:
        for set_name in sets:
            entry = chars.get(char_key(set_name, sid))
            if entry and entry.get("status") == "completed":
                download_character(r.client, set_name, sid, entry)


def request_animation(r: Runner, entry: dict, what: str, name: str, anim: dict, directions: list[str]) -> None:
    a = entry.setdefault("animations", {}).setdefault(name, {"directions": [], "jobs": {}})
    missing = [d for d in directions if d not in a.get("directions", [])]
    for direction in missing:
        body = {"character_id": entry["character_id"], "animation_name": name, "directions": [direction]}
        if a.get("group_id"):
            body["animation_group_id"] = a["group_id"]
        if anim["kind"] == "template":
            body.update(mode="template", template_animation_id=anim["template"])
        else:
            body.update(mode="v3", action_description=anim["action"], frame_count=anim["frames"])
        try:
            response = r.submit(f"{what} {direction}", "/characters/animations", body)
        except PixelLabError as e:
            print(f"  ! {what} {direction}: {e}")
            continue
        a["group_id"] = a.get("group_id") or response.get("animation_group_id")
        a.setdefault("directions", []).append(direction)
        a["status"] = "processing"
        save_state(r.state)

        def done(job, a=a, direction=direction, what=what):
            if job is None:
                a["directions"].remove(direction)  # failed: ask again next run
                a["status"] = "partial"
                return
            a.setdefault("usage", []).append(job.get("usage"))
            a["status"] = "completed"
            print(f"  {what} {direction}: done {job.get('usage')}")
        r.track(response["background_job_ids"][0], done)


def download_character(client: PixelLab, set_name: str, sid: str, entry: dict) -> None:
    s = SETS[set_name]
    out = REPO_ROOT / s["out"] / sid
    detail = client.get(f"/characters/{entry['character_id']}")
    for direction, url in (detail.get("rotation_urls") or {}).items():
        if url and direction in s["directions"]:
            client.download(url, out / "rotations" / f"{direction}.png")
    names = {a.get("group_id"): n for n, a in entry.get("animations", {}).items()}
    for group in detail.get("animations", []):
        name = names.get(group.get("animation_group_id"))
        if not name:
            continue
        for d in group.get("directions", []):
            if d["direction"] not in s["directions"]:
                continue
            for i, url in enumerate(d.get("frames", [])):
                for attempt in range(4):
                    try:
                        client.download(url, out / "animations" / name / d["direction"] / f"frame_{i:03d}.png")
                        break
                    except Exception as e:  # storage sometimes answers 525; the asset is fine
                        if attempt == 3:
                            raise
                        print(f"    retry download ({e})")
                        time.sleep(3)
    print(f"  downloaded {set_name}:{sid} -> {out.relative_to(REPO_ROOT)}")


# ------------------------------------------------------------------ images (portraits, backgrounds)

def save_images(job: dict, dests: list[Path]) -> int:
    lr = job.get("last_response") or {}
    images = lr.get("images") or ([lr["image"]] if lr.get("image") else [])
    for img, dest in zip(images, dests):
        save_base64_image(img if isinstance(img, dict) else {"base64": img}, dest)
    return len(images)


def run_portraits(r: Runner, ids: list[str]) -> None:
    done_map = r.state["gba"]["portraits"]
    style = M.APPROVED / "portraits" / "rowan.png"
    for pid in ids:
        if done_map.get(pid, {}).get("status") == "completed":
            continue
        response = r.submit(f"portrait {pid}", "/generate-image-v2", {
            "description": f"{M.PORTRAITS[pid]}. {M.PORTRAIT_STYLE}", "image_size": {"width": 96, "height": 96},
            "no_background": True, "target_palette": {"max_colors": 20},
            "style_image": {"image": {"type": "base64", "base64": b64(style)}, "size": image_size(style)},
            "style_options": {"color_palette": False, "outline": True, "detail": True, "shading": True}})
        done_map[pid] = {"status": "processing"}
        save_state(r.state)

        def done(job, pid=pid):
            if not job or job.get("status") != "completed":
                done_map[pid] = {"status": "failed"}
                return
            n = save_images(job, [TAKES / "portraits" / f"{pid}_{i}.png" for i in range(9)])
            done_map[pid] = {"status": "completed", "usage": job.get("usage"), "takes": n}
            print(f"  portrait {pid}: {n} takes {job.get('usage')}")
        r.track(response["background_job_id"], done)
    r.drain()


def run_backgrounds(r: Runner, ids: list[str]) -> None:
    done_map = r.state["gba"]["backgrounds"]
    style = M.APPROVED / "battle_bg" / "forest_gba_clean.png"
    w, h = M.BG_SIZE
    for bid in ids:
        if done_map.get(bid, {}).get("status") == "completed":
            continue
        response = r.submit(f"background {bid}", "/generate-image-v2", {
            "description": f"{M.BACKGROUNDS[bid]}. {M.BG_STYLE}", "image_size": {"width": w, "height": h},
            "no_background": False, "target_palette": {"max_colors": 32},
            "style_image": {"image": {"type": "base64", "base64": b64(style)}, "size": image_size(style)},
            "style_options": {"color_palette": False, "outline": True, "detail": True, "shading": True}})
        done_map[bid] = {"status": "processing"}
        save_state(r.state)

        def done(job, bid=bid):
            if not job or job.get("status") != "completed":
                done_map[bid] = {"status": "failed"}
                return
            # Every take goes to takes/backgrounds/<id>/ for picking; the first is installed.
            n = save_images(job, [TAKES / "backgrounds" / bid / f"take_{i}.png" for i in range(16)])
            shutil.copyfile(TAKES / "backgrounds" / bid / "take_0.png", REPO_ROOT / "assets" / "backgrounds" / f"{bid}.png")
            done_map[bid] = {"status": "completed", "usage": job.get("usage"), "takes": n}
            print(f"  background {bid}: {job.get('usage')}")
        r.track(response["background_job_id"], done)
    r.drain()


def run_props(r: Runner, ids: list[str]) -> None:
    done_map = r.state["gba"]["props"]
    style = REPO_ROOT / "assets" / "sprites" / "units" / "rowan_youth" / "rotations" / "south.png"
    for pid in ids:
        if done_map.get(pid, {}).get("status") == "completed":
            continue
        response = r.submit(f"prop {pid}", "/generate-image-v2", {
            "description": f"{M.PROPS[pid]}. {M.PROP_STYLES.get(pid, M.PROP_STYLE)}",
            "image_size": {"width": M.PROP_SIZE.get(pid, 64), "height": M.PROP_SIZE.get(pid, 64)},
            "no_background": True, "target_palette": {"max_colors": 16},
            "style_image": {"image": {"type": "base64", "base64": b64(style)}, "size": image_size(style)},
            "style_options": {"color_palette": False, "outline": True, "detail": True, "shading": True}})
        done_map[pid] = {"status": "processing"}
        save_state(r.state)

        def done(job, pid=pid):
            if not job or job.get("status") != "completed":
                done_map[pid] = {"status": "failed"}
                return
            n = save_images(job, [TAKES / "props" / pid / f"take_{i}.png" for i in range(16)])
            done_map[pid] = {"status": "completed", "usage": job.get("usage"), "takes": n}
            print(f"  prop {pid}: {n} takes {job.get('usage')}")
        r.track(response["background_job_id"], done)
    r.drain()


STYLE_TILES32 = [M.APPROVED / "terrain_gba32" / "T_terrain32" / f"tile_{i}.png" for i in (0, 3, 7, 11)]


def run_tiles(r: Runner, ids: list[str]) -> None:
    done_map = r.state["gba"]["tiles"]
    for tid in ids:
        if done_map.get(tid, {}).get("status") == "completed":
            continue
        numbered = " ".join(f"{i + 1}). {p} tile" for i, (_, p) in enumerate(M.TILESETS[tid]))
        response = r.submit(f"tiles {tid}", "/create-tiles-pro", {
            "description": f"{numbered}. {M.TILE_STYLE}", "tile_type": "square_topdown", "tile_size": 32,
            "tile_view": "top-down", "outline_mode": "segmentation",
            # Style images set the output size, so only the 32 px set may guide it (16 px ones forced 16 px tiles).
            "style_images": [{"base64": b64(p), **image_size(p)} for p in STYLE_TILES32 if p.exists()]})
        done_map[tid] = {"status": "processing", "tile_id": response["tile_id"]}
        save_state(r.state)

        def done(job, tid=tid):
            entry = done_map[tid]
            if not job:
                entry["status"] = "failed"
                return
            result = r.client.get(f"/tiles-pro/{entry['tile_id']}")
            for k, url in (result.get("storage_urls") or {}).items():
                r.client.download(url, TAKES / "tiles" / tid / f"{k}.png")
            entry.update(status="completed", usage=(job or {}).get("usage"))
            print(f"  tiles {tid}: saved to {TAKES / 'tiles' / tid}")
        r.track(response["background_job_id"], done)
    r.drain()


# ------------------------------------------------------------------ plan

def plan(state: dict) -> None:
    g = state["gba"]
    total = 0
    print("Characters (10 per new character; template 1/dir; v3 ~1/dir at 48 px, 1-2 at 64 px):")
    for sid, spec in M.CHARACTERS.items():
        for set_name in spec["sets"]:
            entry = g["characters"].get(char_key(set_name, sid), {})
            cost = 0 if (entry.get("character_id") or spec.get("adopt", {}).get(set_name)) else 10
            wanted = M.map_anims(spec["weapon"]) if set_name == "map" else M.battle_anims(spec["weapon"])
            have = dict(entry.get("animations", {}))
            have.update({n: {"directions": ["east"]} for n in spec.get("done_anims", {}).get(set_name, {})})
            todo = [n for n in wanted if not have.get(n, {}).get("directions")]
            dirs = len(SETS[set_name]["directions"])
            cost += sum(dirs * (1 if wanted[n]["kind"] == "template" or set_name == "map" else 1.5) for n in todo)
            total += cost
            print(f"  {set_name:6} {sid:22} ~{cost:>4.0f}  anims to do: {', '.join(todo) or '-'}")
    for kind, items, each in (("portraits", M.PORTRAITS, 10), ("backgrounds", M.BACKGROUNDS, 10),
                              ("tiles", M.TILESETS, 10)):
        todo = [i for i in items if g[kind].get(i, {}).get("status") != "completed"]
        total += each * len(todo)
        print(f"{kind}: {len(todo)} x ~{each} = ~{each * len(todo)}  ({', '.join(todo) or '-'})")
    print(f"Estimated total: ~{total:.0f} generations")


# ------------------------------------------------------------------ main

def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("command", choices=["balance", "plan", "chars", "portraits", "backgrounds", "tiles", "props", "download"])
    parser.add_argument("--only", nargs="*", default=[])
    parser.add_argument("--set", choices=list(SETS), action="append")
    parser.add_argument("--no-anims", action="store_true")
    parser.add_argument("--reserve", type=float, default=50)
    args = parser.parse_args()
    state = load_state()

    if args.command == "plan":
        plan(state)
        return 0
    catalog = {"chars": M.CHARACTERS, "download": M.CHARACTERS, "portraits": M.PORTRAITS,
               "backgrounds": M.BACKGROUNDS, "tiles": M.TILESETS, "props": M.PROPS}.get(args.command, {})
    ids = args.only or list(catalog)
    unknown = [i for i in ids if i not in catalog]
    if unknown:
        parser.error(f"unknown ids {unknown}; known: {list(catalog)}")
    sets = args.set or list(SETS)
    try:
        client = PixelLab()
        if args.command == "balance":
            print(json.dumps(client.balance(), indent=2))
            return 0
        if args.command == "download":
            for sid in ids:
                for set_name in sets:
                    entry = state["gba"]["characters"].get(char_key(set_name, sid))
                    if entry and entry.get("status") == "completed":
                        download_character(client, set_name, sid, entry)
            return 0
        r = Runner(client, state, args)
        try:
            {"chars": lambda: run_characters(r, ids, sets, not args.no_anims),
             "portraits": lambda: run_portraits(r, ids),
             "backgrounds": lambda: run_backgrounds(r, ids),
             "tiles": lambda: run_tiles(r, ids),
             "props": lambda: run_props(r, ids)}[args.command]()
        finally:
            r.drain()
            save_state(state)
        print("Balance:", (client.balance().get("subscription") or {}).get("generations"))
        return 0
    except PixelLabError as e:
        print(f"error: {e}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
