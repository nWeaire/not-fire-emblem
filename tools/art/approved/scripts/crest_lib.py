"""Crest library in the style of H1/G5/G6, plus Rowan's own crest grown tier by tier from H1.
Usage: crest_lib.py library            -> crests/lib_t<tier>_<set>/NN.png
       crest_lib.py rowan <tier> <prev> -> crests/rowan_t<tier>/NN.png (prev = path of the previous tier's crest)"""
import base64, sys
from pathlib import Path

sys.path.insert(0, r"C:\Users\nweai\OneDrive\Documents\not-fire-emblem\tools\art")
from pixellab_client import PixelLab, RateLimited  # noqa: E402

c = PixelLab()
OUT = Path("crests")
GUIDE = [OUT / "h_wings2" / "00.png", OUT / "g_blades2" / "04.png", OUT / "g_blades2" / "05.png"]  # H1, G5, G6
STYLE = ("floating magical crest emblem of thin bright white luminous line art with a pale blue glow, sharp tapering "
         "calligraphic strokes, hooked blade-like curls, heraldic, perfectly symmetrical, centered, built around a V "
         "shape, no hand, no frame, no circle border, no gold, transparent background, pixel art")
TIERS = {
    1: "a simple small crest with few strokes: one central spike and two curved arms",
    2: "a moderately detailed crest: doubled parallel lines along the arms, longer hooks and a few extra barbs",
    3: "an ornate crest: layered arms with branching filigree and a small gem or star at the core",
    4: "a magnificent, highly elaborate crest: a crown of rays above, wide sweeping layered wings and intricate "
       "symmetrical filigree filling the whole space",
}
MOTIFS = {
    "a": "each design built around a different motif: horns, thorns, feathers, flames, a crescent moon, a serpent, "
         "a star, a sword blade, a branching tree",
    "b": "each design built around a different motif: beast claws, antlers, a tower spire, a rose, lightning, a "
         "rising sun, fins, a bird's skull, a single eye",
}


def b64(path):
    return {"type": "base64", "base64": base64.b64encode(Path(path).read_bytes()).decode(), "format": "png"}


def save_images(job, folder):
    lr = job.get("last_response") or {}
    imgs = lr.get("images") or ([lr["image"]] if lr.get("image") else [])
    folder.mkdir(parents=True, exist_ok=True)
    for i, img in enumerate(imgs):
        data = img["base64"] if isinstance(img, dict) else img
        if data.startswith("data:"):
            data = data.split(",", 1)[1]
        (folder / f"{i:02d}.png").write_bytes(base64.b64decode(data))
    print(folder.name, job.get("usage"), len(imgs))


def post(path, body):
    while True:
        try:
            return c.post(path, body)
        except RateLimited:
            pending and finish(pending.pop(0))


pending = []


def finish(item):
    job_id, folder = item
    save_images(c.wait_for_job(job_id, poll_seconds=4), folder)


if sys.argv[1] == "library":
    styles = [{"image": b64(p), "width": 84, "height": 84} for p in GUIDE]
    for tier, tdesc in TIERS.items():
        for key, mdesc in MOTIFS.items():
            folder = OUT / f"lib_t{tier}_{key}"
            if folder.exists():
                continue
            r = post("/generate-with-style-v2", {"style_images": styles, "description": f"{tdesc}; {mdesc}",
                                                 "style_description": STYLE, "no_background": True})
            pending.append((r["background_job_id"], folder))
elif sys.argv[1] == "rowan":
    tier, prev = int(sys.argv[2]), Path(sys.argv[3])
    r = post("/generate-image-v2", {
        "description": f"a more elaborate version of the reference crest, keeping its central spike and crescent "
                       f"arms recognisable: {TIERS[tier]}. {STYLE}",
        "image_size": {"width": 84, "height": 84}, "no_background": True, "target_palette": {"max_colors": 16},
        "reference_images": [{"image": b64(prev), "size": {"width": 84, "height": 84},
                              "usage_description": "the same crest at its previous, simpler stage; keep its core shape"}],
        "style_image": {"image": b64(GUIDE[0]), "size": {"width": 84, "height": 84}}})
    pending.append((r["background_job_id"], OUT / f"rowan_t{tier}"))
while pending:
    finish(pending.pop(0))
print("balance", c.balance()["subscription"]["generations"])
