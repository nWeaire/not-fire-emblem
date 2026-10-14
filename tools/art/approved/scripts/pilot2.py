"""Round 2: terrain at several detail levels, battle backgrounds, UI panel styles. Resumable via pilot2_state.json."""
import base64, json, sys, time
from pathlib import Path

REPO = Path(r"C:\Users\nweai\OneDrive\Documents\not-fire-emblem")
sys.path.insert(0, str(REPO / "tools" / "art"))
from pixellab_client import PixelLab, PixelLabError, RateLimited  # noqa: E402

HERE = Path(__file__).parent
OUT = HERE / "pilot2"
STATE = HERE / "pilot2_state.json"
state = json.loads(STATE.read_text()) if STATE.exists() else {}
c = PixelLab()


def save():
    STATE.write_text(json.dumps(state, indent=2))


TERRAIN = [("plain", "lush green grass field"),
           ("forest", "dense forest floor seen from above, tree canopy and undergrowth"),
           ("mountain", "rocky brown mountain stone with cracks and ridges"),
           ("wall", "grey castle stone brick wall top"),
           ("fort", "packed earth courtyard floor with a stone border"),
           ("water", "blue river water with ripples"),
           ("bridge", "wooden plank bridge, planks running left to right")]

TERRAIN_LEVELS = {
    "t1_gba": (16, "Game Boy Advance Fire Emblem map tile, flat bright colours, very limited palette, simple shapes"),
    "t2_mid": (32, "16-bit tactics RPG pixel art tile, moderate detail, clean readable shading, limited palette"),
    "t3_rich": (32, "richly detailed pixel art tile, many small details, textured shading with highlights and "
                    "shadows, like modern HD-2D tactics games"),
    "t4_high": (64, "highly detailed painterly pixel art texture, fine detail, soft lighting, high colour count"),
}

BG_SCENES = {"plains": "open grassy plains battlefield with rolling green hills, a few trees and distant mountains, "
                       "bright sky with clouds, wide ground area in the lower half for two fighters",
             "forest": "deep forest clearing battlefield, tall tree trunks, dappled light, ferns, mossy ground in "
                       "the lower half for two fighters"}
BG_LEVELS = {
    "b1_gba": ((240, 135), {"max_colors": 32},
               "Game Boy Advance Fire Emblem battle background, 16-bit GBA pixel art, flat simple shading"),
    "b2_mid": ((320, 180), {"max_colors": 64},
               "16-bit SNES era pixel art battle background, moderate detail, layered parallax look"),
    "b3_high": ((480, 270), None,
                "highly detailed modern pixel art battle background, atmospheric lighting, depth, rich colour, "
                "like Octopath Traveler or Triangle Strategy"),
}

UI_STYLES = {
    "u1_classic": ("classic Game Boy Advance Fire Emblem menu window, deep royal blue gradient fill, light blue and "
                   "white bevelled rounded border", "royal blue, light blue, white"),
    "u2_parchment": ("storybook RPG window, aged cream parchment fill, dark brown carved wood frame with brass "
                     "corner rivets", "cream, brown, brass"),
    "u3_crest": ("ornate dark navy slate window with gold filigree border and small red crest gems in the corners, "
                 "regal fantasy RPG", "dark navy, gold, crimson"),
    "u4_clean": ("minimal modern tactics game window, flat dark charcoal fill, thin crisp warm white 1px border with "
                 "small corner ticks, no decoration", "charcoal, warm white"),
}

JOBS = []  # (key, endpoint, body)
for lvl, (size, style) in TERRAIN_LEVELS.items():
    numbered = " ".join(f"{i + 1}). {p} tile" for i, (_, p) in enumerate(TERRAIN))
    JOBS.append((lvl, "/create-tiles-pro", {
        "description": f"{numbered}. {style}, seamless top-down", "tile_type": "square_topdown",
        "tile_size": size, "tile_view": "top-down", "outline_mode": "segmentation"}))
for lvl, ((w, h), pal, style) in BG_LEVELS.items():
    for scene, desc in BG_SCENES.items():
        body = {"description": f"{desc}. {style}", "image_size": {"width": w, "height": h}, "no_background": False}
        if pal:
            body["target_palette"] = pal
        JOBS.append((f"{lvl}_{scene}", "/generate-image-v2", body))
for key, (desc, pal) in UI_STYLES.items():
    JOBS.append((f"{key}_panel", "/generate-ui-v2", {
        "description": f"empty rectangular dialog window panel, {desc}, pixel art, symmetrical, plain empty centre",
        "image_size": {"width": 128, "height": 96}, "color_palette": pal}))
    JOBS.append((f"{key}_plate", "/generate-ui-v2", {
        "description": f"wide empty name plate banner, {desc}, pixel art, symmetrical",
        "image_size": {"width": 128, "height": 32}, "color_palette": pal}))


def write_b64(img, dest):
    dest.parent.mkdir(parents=True, exist_ok=True)
    data = img["base64"] if isinstance(img, dict) else img
    if data.startswith("data:"):
        data = data.split(",", 1)[1]
    dest.write_bytes(base64.b64decode(data))


def finish(key):
    e = state[key]
    job = c.wait_for_job(e["job"], poll_seconds=5)
    e["usage"] = job.get("usage")
    if e.get("tile_id"):
        res = c.get(f"/tiles-pro/{e['tile_id']}")
        for k, url in (res.get("storage_urls") or {}).items():
            c.download(url, OUT / key / f"{k}.png")
    else:
        lr = job.get("last_response") or {}
        imgs = lr.get("images") or ([lr["image"]] if lr.get("image") else [])
        for i, img in enumerate(imgs):
            write_b64(img, OUT / key / f"{i:02d}.png")
    e["done"] = True
    save()
    print("done", key, e["usage"])


def main():
    pending = [k for k, v in state.items() if not v.get("done")]
    for key, endpoint, body in JOBS:
        if key in state:
            continue
        while True:
            try:
                r = c.post(endpoint, body)
                break
            except RateLimited:
                finish(pending.pop(0))
        state[key] = {"job": r.get("background_job_id"), "tile_id": r.get("tile_id")}
        save()
        pending.append(key)
        print("submitted", key)
    for key in pending:
        try:
            finish(key)
        except PixelLabError as ex:
            print("!", key, ex)
    print("balance", c.balance().get("subscription", {}).get("generations"))


if __name__ == "__main__":
    main()
