"""Battle mockup v3: GBA forest (fighters inpainted out), round-3 sprites, Clean UI. Rowan crits, the brute counters."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageOps, ImageFont

P, P1 = Path("pilot3"), Path("pilot")
W, H = 240, 135            # scene pixels (x4 -> 960x540)
UW, UH = 320, 180          # UI pixels (x3 -> 960x540)
OUTW, OUTH = 960, 540
FEET = 101
FONT = ImageFont.truetype("fonts/PixelifySans.ttf", 10)
FB = ImageFont.truetype("fonts/PixelifySans.ttf", 13)
FS = ImageFont.truetype("fonts/Silkscreen-Regular.ttf", 8)
BIG = ImageFont.truetype("fonts/PixelifySans.ttf", 14)
TXT, ACC, HPA, HPE = (238, 232, 220), (240, 200, 120), (120, 200, 230), (235, 110, 100)

BG = Image.open("forest_clean_0.png").convert("RGBA").resize((W, H))
PANEL = Image.open("pilot2/u4_clean_panel/01.png").convert("RGBA")
PANEL = PANEL.crop(PANEL.getbbox())
PLATE = Image.open("pilot2/u4_clean_plate/02.png").convert("RGBA")
PLATE = PLATE.crop(PLATE.getbbox())


def frames_of(name, mirror=False):
    fr = [Image.open(f).convert("RGBA") for f in sorted((P / name).glob("*.png"))]
    return [ImageOps.mirror(f) for f in fr] if mirror else fr


def anchor(im):
    l, t, r, b = im.getbbox()
    return (l + r) // 2, b


crit = frames_of("rowan_crit")
brute_atk = frames_of("brute_attack", mirror=True)
RA, BA = anchor(crit[0]), anchor(brute_atk[0])
impact = Image.open(P1 / "vfx_impact" / "01.png").convert("RGBA")
streak = Image.open(P1 / "vfx_slash" / "15.png").convert("RGBA")
claw = ImageOps.mirror(Image.open(P1 / "vfx_slash" / "13.png").convert("RGBA"))

ROWAN_X, BRUTE_X = 62, 182
# Rowan's blade tip on the strike frames, and the brute's claw reach, measured from the sprites themselves.
TIP = max(f.getbbox()[2] for f in crit[10:13]) - RA[0]
BRUTE_FRONT = BRUTE_X - (BA[0] - brute_atk[0].getbbox()[0]) + 10   # front of the brute's body, a little inside
REACH = BA[0] - min(f.getbbox()[0] for f in brute_atk[4:7])
STRIKE_X = BRUTE_FRONT - TIP + 4
ROWAN_FRONT = ROWAN_X + 8


def nine(src, w, h, m=None):
    m = m or (8 if src.height < 48 else 12)
    out = Image.new("RGBA", (w, h))
    xs = [(0, m, 0, m), (m, src.width - m, m, w - m), (src.width - m, src.width, w - m, w)]
    ys = [(0, m, 0, m), (m, src.height - m, m, h - m), (src.height - m, src.height, h - m, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            out.alpha_composite(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST), (dx0, dy0))
    return out


def ui_layer(hp_r, hp_b, label):
    ui = Image.new("RGBA", (UW, UH))
    d = ImageDraw.Draw(ui)
    ui.alpha_composite(nine(PLATE, 84, 22), (6, 6))
    d.text((18, 9), "Rowan", font=FB, fill=TXT)
    ui.alpha_composite(nine(PLATE, 96, 22), (UW - 102, 6))
    d.text((UW - 90, 9), "Gloam Brute", font=FB, fill=TXT)
    for x, weapon, stats, hp, mx, col in ((0, "Iron Sword", (88, 9, 12), hp_r, 20, HPA),
                                          (UW // 2, "Claws", (71, 6, 0), hp_b, 24, HPE)):
        ui.alpha_composite(nine(PANEL, UW // 2, 40), (x, UH - 40))
        d.text((x + 12, UH - 34), weapon, font=FONT, fill=TXT)
        for i, (lab, val) in enumerate(zip(("HIT", "DMG", "CRT"), stats)):
            d.text((x + 82 + i * 24, UH - 34), lab, font=FS, fill=ACC)
            d.text((x + 84 + i * 24, UH - 25), f"{val:2d}", font=FONT, fill=TXT)
        d.text((x + 12, UH - 18), f"{hp:2d}", font=FONT, fill=TXT)
        d.rectangle([x + 30, UH - 14, x + UW // 2 - 12, UH - 11], fill=(30, 30, 30))
        d.rectangle([x + 31, UH - 13, x + 31 + int((UW // 2 - 44) * hp / mx), UH - 12], fill=col)
    if label:
        d.text((UW // 2 - 34, 30), label, font=BIG, fill=(255, 236, 110), stroke_width=2, stroke_fill=(120, 24, 0))
    return ui.resize((OUTW, OUTH), Image.NEAREST)


def tint(im, color, amount):
    m = Image.blend(im, Image.new("RGBA", im.size, color + (255,)), amount)
    m.putalpha(im.split()[3])
    return m


frames, durations = [], []
state = {"hp_r": 20, "hp_b": 24}


def frame(r_im, rx, b_im, bx, *, dim=0.0, flash=0.0, shake=(0, 0), rt=None, bt=None, vfx=(), dmg=None, label=None,
          ms=80):
    c = BG.copy()
    if dim:
        c = Image.blend(c, Image.new("RGBA", c.size, (12, 8, 28, 255)), dim)
    sd = ImageDraw.Draw(c, "RGBA")
    for x in (rx, bx):
        sd.ellipse([x - 14, FEET - 2, x + 14, FEET + 2], fill=(0, 0, 0, 100))
    c.alpha_composite(tint(b_im, *bt) if bt else b_im, (bx - BA[0], FEET - BA[1]))
    c.alpha_composite(tint(r_im, *rt) if rt else r_im, (rx - RA[0], FEET - RA[1]))
    for im, x, y in vfx:
        c.alpha_composite(im, (int(x - im.width / 2), int(y - im.height / 2)))
    if dmg:
        ImageDraw.Draw(c).text(dmg[:2], dmg[2], font=FB, fill="white", stroke_width=1, stroke_fill=(200, 0, 0))
    if flash:
        c = Image.blend(c, Image.new("RGBA", c.size, (255, 255, 255, 255)), flash)
    big = c.resize((OUTW, OUTH), Image.NEAREST)
    big.alpha_composite(ui_layer(state["hp_r"], state["hp_b"], label))
    if shake != (0, 0):
        s = Image.new("RGBA", big.size, (0, 0, 0, 255))
        sx, sy = shake[0] * 4, shake[1] * 4
        s.alpha_composite(big, (max(0, sx), max(0, sy)), (max(0, -sx), max(0, -sy)))
        big = s
    frames.append(big.convert("RGB"))
    durations.append(ms)


R0, B0 = crit[0], brute_atk[0]
for _ in range(8):
    frame(R0, ROWAN_X, B0, BRUTE_X, ms=90)
# --- Rowan's critical
frame(R0, ROWAN_X, B0, BRUTE_X, flash=0.9, ms=50)
for i in range(1, 8):
    frame(crit[i], ROWAN_X, B0, BRUTE_X, dim=min(0.5, 0.1 * i), label="CRITICAL!", ms=85)
for n, i in enumerate((8, 9, 10)):
    frame(crit[i], ROWAN_X + (STRIKE_X - ROWAN_X) * (n + 1) // 3, B0, BRUTE_X, dim=0.5, label="CRITICAL!", ms=55)
chest = FEET - 26
cut = streak.resize((44, 44), Image.NEAREST)
frame(crit[11], STRIKE_X, B0, BRUTE_X, dim=0.5, vfx=[(cut, BRUTE_FRONT + 4, chest)], ms=55)
frame(crit[11], STRIKE_X, B0, BRUTE_X, flash=1.0, ms=55)
state["hp_b"] = 24
for n, sh in enumerate([(3, -2), (-3, 2), (2, 1), (-1, -1), (1, 0), (0, 0)]):
    state["hp_b"] = max(0, 24 - int(18 * min(1, (n + 1) / 4)))
    vfx = [(impact.resize((int(30 + 8 * n),) * 2, Image.NEAREST), BRUTE_FRONT, chest)] if n < 3 else []
    frame(crit[12], STRIKE_X, B0, BRUTE_X + min(6, 2 * n), dim=max(0, 0.4 - 0.08 * n), shake=sh,
          bt=((255, 255, 255) if n % 2 == 0 else (255, 40, 40), 0.85 if n < 3 else 0.3), vfx=vfx,
          dmg=(BRUTE_X - 10, FEET - 62 - 2 * n, "18"), ms=70)
for n in range(4):
    frame(crit[12] if n < 2 else R0, STRIKE_X - (STRIKE_X - ROWAN_X) * (n + 1) // 4, B0, BRUTE_X + 6 - 2 * n, ms=70)
for _ in range(4):
    frame(R0, ROWAN_X, B0, BRUTE_X, ms=90)
# --- the brute's counter: lunges in and rakes; claws stop at Rowan's chest
ATK_X = ROWAN_FRONT + REACH - 4
for n, i in enumerate(range(1, 4)):
    frame(R0, ROWAN_X, brute_atk[i], BRUTE_X - (BRUTE_X - ATK_X) * (n + 1) // 3, ms=75)
rake = claw.resize((36, 36), Image.NEAREST)
frame(R0, ROWAN_X, brute_atk[4], ATK_X, vfx=[(rake, ROWAN_FRONT, FEET - 24)], ms=70)
for n, sh in enumerate([(2, -1), (-2, 1), (1, 0), (0, 0)]):
    state["hp_r"] = 20 - int(6 * min(1, (n + 1) / 3))
    frame(R0, ROWAN_X - min(4, 2 * n), brute_atk[min(8, 5 + n)], ATK_X, shake=sh,
          rt=((255, 255, 255) if n % 2 == 0 else (255, 60, 60), 0.75 if n < 2 else 0.2),
          vfx=[(impact.resize((22, 22), Image.NEAREST), ROWAN_FRONT, FEET - 24)] if n < 2 else (),
          dmg=(ROWAN_X - 6, FEET - 56 - 2 * n, "6"), ms=70)
for n in range(3):
    frame(R0, ROWAN_X - 4 + 2 * n, B0, ATK_X + (BRUTE_X - ATK_X) * (n + 1) // 3, ms=70)
for _ in range(10):
    frame(R0, ROWAN_X, B0, BRUTE_X, ms=100)

frames[0].save("page/battle_final_style.gif", save_all=True, append_images=frames[1:], duration=durations, loop=0)
for i in (0, 21, 36):
    frames[i].save(f"m3_key{i}.png")
print(len(frames), "frames", TIP, REACH, STRIKE_X, ATK_X)
