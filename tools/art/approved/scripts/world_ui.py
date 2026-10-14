"""Clean-UI map screen over the in-game world screenshot: new windows sit exactly over the old HUD panels."""
from PIL import Image, ImageDraw, ImageFont

NW, NH, K = 427, 240, 3
TXT, ACC, HPA, HIL = (238, 232, 220), (240, 200, 120), (120, 200, 230), (238, 232, 220, 40)
F = ImageFont.truetype("fonts/PixelifySans.ttf", 10)
FB = ImageFont.truetype("fonts/PixelifySans.ttf", 13)
FS = ImageFont.truetype("fonts/Silkscreen-Regular.ttf", 8)
PANEL = Image.open("pilot2/u4_clean_panel/01.png").convert("RGBA")
PANEL = PANEL.crop(PANEL.getbbox())


def nine(src, w, h, m=12):
    out = Image.new("RGBA", (w, h))
    xs = [(0, m, 0, m), (m, src.width - m, m, w - m), (src.width - m, src.width, w - m, w)]
    ys = [(0, m, 0, m), (m, src.height - m, m, h - m), (src.height - m, src.height, h - m, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            out.alpha_composite(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST), (dx0, dy0))
    return out


ui = Image.new("RGBA", (NW, NH))
d = ImageDraw.Draw(ui)
t = lambda xy, s, f=F, c=TXT: d.text(xy, s, font=f, fill=c)

# objective window over the old one (top-left)
ui.alpha_composite(nine(PANEL, 150, 50), (4, 4))
t((14, 9), "Prologue", FB)
t((14, 24), "Goal: Defeat The Binder")
t((14, 35), "Aldric must survive", F, ACC)

# unit window over the old one (bottom-left)
ui.alpha_composite(nine(PANEL, 136, 92), (4, 140))
face = Image.open("pilot/portrait_rowan/00.png").convert("RGBA")
ui.alpha_composite(face.crop((14, 4, 82, 72)).resize((30, 30), Image.NEAREST), (12, 148))
t((46, 147), "Rowan", FB)
t((46, 162), "Youth  Lv 1")
t((12, 182), "HP", FS, ACC)
d.rectangle([26, 184, 82, 187], fill=(30, 30, 30))
d.rectangle([27, 185, 81, 186], fill=HPA)
t((88, 180), "20/20")
for i, (lab, val) in enumerate((("Str", 7), ("Mag", 1), ("Dex", 8), ("Spd", 9), ("Lck", 7), ("Def", 6), ("Res", 2),
                                 ("Mov", 5))):
    x, y = 12 + (i % 4) * 31, 195 + (i // 4) * 12
    t((x, y), lab, FS, ACC)
    t((x + 19, y - 1), str(val))

# action menu beside Rowan
ui.alpha_composite(nine(PANEL, 80, 72), (238, 72))
for i, item in enumerate(["Attack", "Items", "Trade", "Wait"]):
    y = 80 + i * 14
    if i == 0:
        d.rectangle([243, y - 1, 312, y + 11], fill=HIL)
        d.polygon([(246, y + 2), (246, y + 9), (251, y + 5)], fill=ACC)
    t((256, y - 1), item)

# terrain window over the old one (bottom-right)
ui.alpha_composite(nine(PANEL, 90, 40), (NW - 92, NH - 44))
t((NW - 82, NH - 40), "Plain", FB)
t((NW - 82, NH - 25), "DEF 0  AVO 0")

base = Image.open("world_final.png").convert("RGBA")
ImageDraw.Draw(base).rectangle((0, 690, 1280, 720), fill=base.getpixel((6, 700)))   # key-hint line
base.alpha_composite(ui.resize((NW * K, NH * K), Image.NEAREST).crop((0, 0, 1280, 720)))
base.convert("RGB").save("page/world_final_ui.png")
