"""UI style mockups: map screen, battle screen and dialogue, per style, from generated 9-slice panels.
Usage: ui_mock.py <style> <panel.png> <plate.png>   (writes ui_<style>_{map,battle,dialogue}.png)"""
import colorsys
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps, ImageFilter

NW, NH, K = 427, 240, 3  # native UI canvas, upscaled x3 to 1280x720
P, P2 = Path("pilot"), Path("pilot2")
style, panel_path, plate_path = sys.argv[1:4]
PALETTES = {  # text, shadow, accent, hp-ally, hp-enemy, menu highlight
    "u1_classic": ((255, 255, 255), (24, 32, 80), (255, 232, 120), (96, 200, 255), (255, 96, 96), (255, 255, 255, 70)),
    "u2_parchment": ((62, 38, 20), None, (150, 40, 24), (60, 130, 70), (170, 50, 40), (150, 90, 30, 60)),
    "u3_crest": ((240, 226, 190), (10, 12, 30), (232, 190, 90), (110, 190, 240), (230, 80, 80), (232, 190, 90, 60)),
    "u4_clean": ((238, 232, 220), None, (240, 200, 120), (120, 200, 230), (235, 110, 100), (238, 232, 220, 40)),
}
TXT, SHD, ACC, HPA, HPE, HIL = PALETTES[style]
F = ImageFont.truetype("fonts/PixelifySans.ttf", 10)
FB = ImageFont.truetype("fonts/PixelifySans.ttf", 13)
FS = ImageFont.truetype("fonts/Silkscreen-Regular.ttf", 8)


def trim(im):
    return im.crop(im.getbbox())


PANEL = trim(Image.open(panel_path).convert("RGBA"))
PLATE = trim(Image.open(plate_path).convert("RGBA"))


def nine(src, w, h, m=None):
    """9-slice: keeps corners, stretches edges and centre (nearest), so borders stay crisp."""
    m = m or (8 if src.height < 48 else 12)
    out = Image.new("RGBA", (w, h))
    xs = [(0, m, 0, m), (m, src.width - m, m, w - m), (src.width - m, src.width, w - m, w)]
    ys = [(0, m, 0, m), (m, src.height - m, m, h - m), (src.height - m, src.height, h - m, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 > dx0 and dy1 > dy0:
                out.alpha_composite(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST),
                                    (dx0, dy0))
    return out


def hue_shift(im, to_red=True):
    """Recolours a panel toward red for the enemy side (classic FE red vs blue)."""
    px = im.load()
    out = im.copy()
    po = out.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if s > 0.15:
                h = (h + 0.38) % 1.0 if to_red else h
            r, g, b = colorsys.hsv_to_rgb(h, s, v)
            po[x, y] = (int(r * 255), int(g * 255), int(b * 255), a)
    return out


def text(d, xy, s, font=F, fill=None):
    if SHD:
        d.text((xy[0] + 1, xy[1] + 1), s, font=font, fill=SHD)
    d.text(xy, s, font=font, fill=fill or TXT)


def bar(d, x, y, w, frac, col):
    d.rectangle([x, y, x + w, y + 3], fill=(30, 30, 30, 255))
    d.rectangle([x + 1, y + 1, x + 1 + int((w - 2) * frac), y + 2], fill=col)


def finish(ui, base, name):
    big = ui.resize((NW * K, NH * K), Image.NEAREST).crop((0, 0, 1280, 720))
    base = base.convert("RGBA").resize((1280, 720))
    base.alpha_composite(big)
    base.convert("RGB").save(f"ui_{style}_{name}.png")


face = Image.open(P / "portrait_rowan" / "00.png").convert("RGBA")

# ---------------------------------------------------------------- map screen
ui = Image.new("RGBA", (NW, NH))
d = ImageDraw.Draw(ui)
ui.alpha_composite(nine(PANEL, 118, 46), (6, 6))
ui.alpha_composite(face.crop((14, 4, 82, 72)).resize((34, 34), Image.NEAREST), (12, 12))
text(d, (50, 11), "Rowan", FB)
text(d, (50, 26), "Youth   Lv 1", F)
text(d, (50, 37), "HP", FS, ACC)
bar(d, 64, 38, 52, 1.0, HPA)
ui.alpha_composite(nine(PANEL, 64, 74), (238, 70))
for i, item in enumerate(["Attack", "Items", "Trade", "Wait"]):
    y = 78 + i * 15
    if i == 0:
        d.rectangle([243, y - 1, 296, y + 12], fill=HIL)
        text(d, (244, y), "▶", FS, ACC)
    text(d, (254, y), item)
ui.alpha_composite(nine(PANEL, 92, 44), (NW - 98, NH - 50))
text(d, (NW - 86, NH - 43), "Plain", FB)
text(d, (NW - 86, NH - 27), "DEF 0  AVO 0", F)
ui.alpha_composite(nine(PLATE, 180, 24), (NW - 186, 6))
text(d, (NW - 170, 11), "Goal: Defeat The Binder", F)
base = Image.open("game_map2.png").convert("RGB")
bd = ImageDraw.Draw(base)
fill = base.getpixel((6, 6))
for r in ((10, 10, 345, 155), (10, 418, 345, 690), (1095, 603, 1270, 690), (0, 688, 1280, 720)):
    bd.rectangle(r, fill=fill)
finish(ui, base, "map")

# ---------------------------------------------------------------- battle screen
bg = Image.open(sorted((P2 / "b2_mid_plains").glob("*.png"))[0]).convert("RGBA").resize((NW, NH), Image.NEAREST)
scene = bg.copy()
rowan = Image.open(P / "battle_rowan" / "east.png").convert("RGBA")
brig = ImageOps.mirror(Image.open(P / "battle_brigand" / "east.png").convert("RGBA"))
sd = ImageDraw.Draw(scene, "RGBA")
for x in (130, 300):
    sd.ellipse([x - 22, 182, x + 22, 188], fill=(0, 0, 0, 90))
scene.alpha_composite(rowan, (130 - 61 + 13, 185 - 92 + 4))
scene.alpha_composite(brig, (300 - 56, 185 - 90))
ui = Image.new("RGBA", (NW, NH))
d = ImageDraw.Draw(ui)
red_panel = hue_shift(PANEL) if style == "u1_classic" else PANEL
red_plate = hue_shift(PLATE) if style == "u1_classic" else PLATE
ui.alpha_composite(nine(PLATE, 96, 24), (6, 6))
text(d, (20, 10), "Rowan", FB)
ui.alpha_composite(nine(red_plate, 96, 24), (NW - 102, 6))
text(d, (NW - 88, 10), "Brigand", FB)
for side, x, pnl, name, weapon, stats, hp, mx, col in (
        (0, 0, PANEL, "Rowan", "Iron Sword", (88, 9, 12), 20, 20, HPA),
        (1, NW // 2, red_panel, "Brigand", "Iron Axe", (61, 7, 0), 26, 26, HPE)):
    ui.alpha_composite(nine(pnl, NW // 2, 50), (x, NH - 50))
    text(d, (x + 14, NH - 41), weapon, FB)
    for i, (lab, val) in enumerate(zip(("HIT", "DMG", "CRT"), stats)):
        text(d, (x + 118 + i * 28, NH - 41), lab, FS, ACC)
        text(d, (x + 120 + i * 28, NH - 32), f"{val:2d}", F)
    text(d, (x + 14, NH - 25), f"{hp:2d}", F)
    bar(d, x + 36, NH - 20, NW // 2 - 54, hp / mx, col)
finish(ui, scene.resize((1280, 720), Image.NEAREST), "battle")

# ---------------------------------------------------------------- dialogue
bg = Image.open(sorted((P2 / "b2_mid_forest").glob("*.png"))[0]).convert("RGBA").resize((1280, 720))
bg = bg.filter(ImageFilter.GaussianBlur(3))
ui = Image.new("RGBA", (NW, NH))
d = ImageDraw.Draw(ui)
ui.alpha_composite(face.resize((144, 144), Image.NEAREST), (24, NH - 62 - 144 + 8))
ui.alpha_composite(nine(PANEL, NW - 24, 62), (12, NH - 68))
ui.alpha_composite(nine(PLATE, 84, 24), (30, NH - 82))
text(d, (48, NH - 78), "Rowan", FB)
text(d, (28, NH - 52), "The one in the cloak. There's always one in a cloak.", FB)
text(d, (NW - 34, NH - 22), "▼", FS, ACC)
finish(ui, bg, "dialogue")
print("ok", style)
