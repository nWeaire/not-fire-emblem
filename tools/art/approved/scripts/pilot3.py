"""Round 3: battle + map characters in the style of the fighters in the GBA forest background. Resumable."""
import base64, json, sys
from pathlib import Path

REPO = Path(r"C:\Users\nweai\OneDrive\Documents\not-fire-emblem")
sys.path.insert(0, str(REPO / "tools" / "art"))
from pixellab_client import PixelLab, PixelLabError, RateLimited  # noqa: E402

HERE = Path(__file__).parent
OUT = HERE / "pilot3"
STATE = HERE / "pilot3_state.json"
state = json.loads(STATE.read_text()) if STATE.exists() else {}
c = PixelLab()
REF = {"type": "base64", "base64": base64.b64encode((HERE / "refs" / "gba_clean_fighters.png").read_bytes()).decode(),
       "format": "png"}


def save():
    STATE.write_text(json.dumps(state, indent=2))


STYLE = ("small chunky 16-bit pixel art sprite like the fighters in a Game Boy Advance battle scene, muted earthy "
         "palette, 2-3 tone flat shading per material, dark outline, low detail, bold readable silhouette")

CAST = {
    "rowan": "18 year old farm boy hero, messy dark blue hair, off-white linen shirt, brown leather vest, brown "
             "trousers, boots, plain iron sword",
    "kai": "18 year old ranch boy, shaggy sandy blond hair, padded tan cloth gambeson, dented iron cap, long wooden "
           "spear with an iron tip",
    "mira": "18 year old village healer girl, long blonde braid, grey-green wool dress with white apron, wooden staff",
    "gloam_brute": "hulking hunched humanoid beast from a dark fairy tale, ash-grey and violet hide, long arms with "
                   "big claws, tattered rags, small glowing pale eyes",
    "binder": "sinister cloaked dark mage, tattered deep purple hooded cloak, shadowed face, one hand glowing with a "
              "purple sigil, dark tome",
}

CHARS = {}
for k, d in CAST.items():
    CHARS[f"battle_{k}"] = (d, "side", 64)
    CHARS[f"map_{k}"] = (d, "low top-down", 48)

FACE = "keeps facing right the whole time, side view, never turns toward the camera"
ANIMS = {
    "rowan_crit": ("battle_rowan", f"over-the-top critical: crouches low, sword gleams, then dashes forward in a huge "
                                   f"lunge with a wide sweeping slash leaving a white arc, dramatic follow-through. {FACE}", 12),
    "rowan_attack": ("battle_rowan", f"quick attack: steps forward and slashes the sword in a fast arc, then returns "
                                     f"to stance. {FACE}", 8),
    "brute_attack": ("battle_gloam_brute", f"rears back and swipes both clawed arms forward in a savage rake, then "
                                           f"recovers. {FACE}", 8),
    "binder_cast": ("battle_binder", f"raises the glowing purple hand, dark energy swirls, then thrusts it forward "
                                     f"releasing a burst of purple magic. {FACE}", 10),
}

JOBS = []


def submit(key, endpoint, body):
    while True:
        try:
            return c.post(endpoint, body)
        except RateLimited:
            wait_one()


def wait_one():
    for k, e in state.items():
        if not e.get("done"):
            finish(k)
            return


def finish(k):
    e = state[k]
    try:
        job = c.wait_for_job(e["job"], poll_seconds=5)
        e["usage"] = job.get("usage")
    except PixelLabError as ex:
        print("!", k, ex)
    e["done"] = True
    save()
    print("done", k, e.get("usage"))


def run_chars():
    for k, (desc, view, size) in CHARS.items():
        if k in state:
            continue
        r = submit(k, "/create-character-pro", {
            "description": f"{desc}. {STYLE}", "image_size": {"width": size, "height": size},
            "method": "create_with_style", "view": view, "template_id": "mannequin", "reference_image": REF,
            "style_description": STYLE, "no_background": True})
        state[k] = {"character_id": r["character_id"], "job": r["background_job_id"]}
        save()
        print("submitted", k)
    for k in list(state):
        if not state[k].get("done"):
            finish(k)


def run_anims():
    for k, (ck, action, frames) in ANIMS.items():
        if k in state:
            continue
        r = submit(k, "/characters/animations", {
            "character_id": state[ck]["character_id"], "animation_name": k, "mode": "v3",
            "action_description": action, "frame_count": frames, "directions": ["east"]})
        state[k] = {"job": r["background_job_ids"][0], "group": r.get("animation_group_id"), "char": ck}
        save()
        print("submitted", k)
    for k in list(state):
        if not state[k].get("done"):
            finish(k)


def download():
    groups = {state[k].get("group"): k for k in ANIMS if k in state}
    for k in CHARS:
        d = c.get(f"/characters/{state[k]['character_id']}")
        for direction, url in (d.get("rotation_urls") or {}).items():
            if url:
                c.download(url, OUT / k / f"{direction}.png")
        for g in d.get("animations", []):
            name = groups.get(g.get("animation_group_id"))
            if name:
                for dd in g.get("directions", []):
                    for i, url in enumerate(dd.get("frames", [])):
                        c.download(url, OUT / name / f"frame_{i:03d}.png")


if __name__ == "__main__":
    for step in sys.argv[1:]:
        {"chars": run_chars, "anims": run_anims, "download": download}[step]()
    print("balance", c.balance().get("subscription", {}).get("generations"))
