# -*- coding: utf-8 -*-
"""同比例对照 v2：32×32 地面方格 vs 人物，全部 6 倍显示。"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = r"E:\数据库\OCC_Codex"
ART = os.path.join(ROOT, "UnityProject", "Assets", "Game", "Resources", "Art")
OUT = os.path.join(ROOT, "ArtSource", "sephiria_style_study_2026-09-16", "project_assets")
TILE = os.path.join(ART, "FormalAcademyIndependentFloors32", "academy_block_court_a.png")
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf"]

UNITS = ["pyromancer.png", "raider_idle_32x64.png", "maintenance_guard_native.png"]
Z, CELL, FACE = 6, 32, 8
CELL6, FACE6 = CELL * Z, FACE * Z
GOLD, TEXT, DIM = (255, 214, 110), (238, 238, 242), (176, 176, 186)


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def find(name):
    for dp, dn, fn in os.walk(ROOT):
        if any(s in dp for s in ("\\Library\\", "\\obj\\", "\\Temp\\", "\\.git\\", "\\Logs\\")):
            continue
        if name in fn:
            return os.path.join(dp, name)
    return None


def body(im):
    b = im.getbbox()
    return (b[2] - b[0], b[3] - b[1]) if b else (0, 0)


def main():
    tile = Image.open(TILE).convert("RGBA")
    tile6 = tile.resize((CELL6, CELL6), Image.NEAREST)
    face6 = tile.crop((0, CELL - FACE, CELL, CELL)).resize((CELL6, FACE6), Image.NEAREST)

    units = []
    for n in UNITS:
        p = find(n)
        if p:
            im = Image.open(p).convert("RGBA")
            units.append((n, im, body(im)))

    # 一排 6 格，人物站在第 1、3、4 格
    cols, margin, run_top = 6, 40, 300
    strip_w = cols * CELL6
    W, H = max(margin * 2 + strip_w, 1180), 1180
    page = Image.new("RGB", (W, H), (18, 18, 22))
    d = ImageDraw.Draw(page)

    d.text((margin, 20), "同比例对照：32×32 地面方格 + 人物", font=font(28), fill=TEXT)
    d.text((margin, 58), "全部按定案 6 倍显示（1 原生像素 = 6 屏幕像素）——下方格子 192×192，人物画布 192×384",
           font=font(19), fill=DIM)

    for i in range(cols):
        x = margin + i * CELL6
        page.paste(tile6, (x, run_top))
        page.paste(face6, (x, run_top + CELL6))

    d.line([(margin, run_top), (margin + strip_w, run_top)], fill=GOLD, width=2)
    d.line([(margin, run_top + CELL6), (margin + strip_w, run_top + CELL6)], fill=GOLD, width=2)
    d.text((margin + 6, run_top - 26), "格顶：人物从这里往上冒头", font=font(17), fill=GOLD)
    d.text((margin + 6, run_top + CELL6 + FACE6 + 6), "格底：脚底锚在这里", font=font(17), fill=GOLD)

    for cell_x, (name, im, (bw, bh)) in zip([1, 3, 4], units):
        big = im.resize((im.width * Z, im.height * Z), Image.NEAREST)
        page.paste(big, (margin + cell_x * CELL6 + (CELL6 - big.width) // 2, run_top + CELL6 - big.height), big)

    y = run_top + CELL6 + FACE6 + 44
    d.text((margin, y), "人物主体相对一格的倍率：", font=font(19), fill=TEXT)
    y += 30
    for name, im, (bw, bh) in units:
        d.text((margin, y), f"· {name:<34} 画布 {im.width}×{im.height}　主体 {bw}×{bh}"
                            f"　= {bh / CELL:.2f} 格（冒头 {(bh - CELL) / CELL:.2f} 格）",
               font=font(17), fill=DIM)
        y += 26

    # 尺寸并排（单独一行，占满宽度）
    y += 26
    d.text((margin, y), "尺寸并排（同一 6 倍尺度）", font=font(20), fill=TEXT)
    y += 32
    base = y + CELL6                       # 所有底部对齐线
    page.paste(tile6, (margin, base - CELL6))
    d.rectangle([margin, base - CELL6, margin + CELL6, base], outline=GOLD, width=2)
    d.text((margin, base + 8), f"地面方格 32×32 = {CELL6}×{CELL6} 屏幕像素", font=font(16), fill=GOLD)

    x3 = margin + CELL6 + 40
    page.paste(face6, (x3, base - FACE6))
    d.text((x3, base + 8), f"前立面 32×{FACE}", font=font(16), fill=GOLD)
    x3 += CELL6 + 40

    for name, im, (bw, bh) in units:
        big = im.resize((im.width * Z, im.height * Z), Image.NEAREST)
        page.paste(big, (x3, base - big.height), big)
        d.line([(x3 - 12, base - CELL6), (x3 + big.width + 12, base - CELL6)], fill=GOLD, width=1)
        d.text((x3, base + 8), f"{name.split('.')[0][:12]}  主体 {bh}px = {bh / CELL:.2f} 格",
               font=font(15), fill=DIM)
        x3 += big.width + 30

    page.save(os.path.join(OUT, "compare_tile_vs_unit_6x.png"))
    print("saved", page.size)


if __name__ == "__main__":
    main()
