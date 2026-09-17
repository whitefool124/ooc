# -*- coding: utf-8 -*-
"""临时柱体在场景中的样子（6 倍，真实素材合成）。"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = r"E:\数据库\OCC_Codex"
ART = os.path.join(ROOT, "UnityProject", "Assets", "Game", "Resources", "Art")
PH = os.path.join(ROOT, "ArtSource", "temp_placeholder_permanent_pillar")
OUT = os.path.join(PH, "temp_pillar_in_context_6x.png")
Z, CELL = 6, 32
C6 = CELL * Z
GOLD, TEXT, DIM = (255, 214, 110), (240, 240, 244), (176, 176, 186)
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf"]

def font(s):
    for p in FONTS:
        if os.path.exists(p):
            try: return ImageFont.truetype(p, s)
            except Exception: pass
    return ImageFont.load_default()

pillar = Image.open(os.path.join(PH, "temp_permanent_pillar_32.png")).convert("RGBA")
tile = Image.open(os.path.join(ART, "FormalAcademyIndependentFloors32", "academy_block_court_a.png")).convert("RGBA")
unit = Image.open(os.path.join(ART, "FormalUnits64", "pyromancer.png")).convert("RGBA")

def big(im): return im.resize((im.width * Z, im.height * Z), Image.NEAREST)

P, T, U = big(pillar), big(tile), big(unit)

W = 40 + 7 * C6 + 40
H = 60 + 400 + C6 + 150
page = Image.new("RGB", (W, H), (18, 18, 22))
d = ImageDraw.Draw(page)
d.text((40, 16), "临时占位柱体 · 场景中的样子（6 倍）", font=font(26), fill=TEXT)
d.text((40, 50), "左 3 格：不可破坏地形（柱体，铺满整格）　右 3 格：地面方格　火法师站在地面格上",
       font=font(17), fill=DIM)

base = 60 + 400                      # 格底
top = base - C6
for i in range(7):
    x = 40 + i * C6
    if i < 3:
        page.paste(P, (x, top))
    else:
        page.paste(T, (x, top))
d.line([(40, top), (40 + 7 * C6, top)], fill=GOLD, width=2)
d.line([(40, base), (40 + 7 * C6, base)], fill=GOLD, width=2)
d.text((44, top - 26), "格顶", font=font(16), fill=GOLD)
d.text((44, base + 8), "格底", font=font(16), fill=GOLD)

x_unit = 40 + 5 * C6 + (C6 - U.width) // 2
page.paste(U, (x_unit, base - U.height), U)

d.text((40, base + 40), "柱体：32×32 铺满整格、全部像素不透明、四边严丝合缝（连排成墙）",
       font=font(17), fill=DIM)
d.text((40, base + 68), "火法师：主体 51px = 1.59 格，明显高于一格柱体——柱体挡住的是格子，不挡人物上身",
       font=font(17), fill=DIM)
page.save(OUT)
print("saved", OUT, page.size)
