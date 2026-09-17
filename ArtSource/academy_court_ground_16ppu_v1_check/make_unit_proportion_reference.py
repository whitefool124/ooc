# -*- coding: utf-8 -*-
"""人物比例基准卡：以火法师为准（主体 51px = 1.59 格），6 倍显示。"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = r"E:\数据库\OCC_Codex"
ART = os.path.join(ROOT, "UnityProject", "Assets", "Game", "Resources", "Art")
OUT = os.path.join(ROOT, "ArtSource", "sephiria_style_study_2026-09-16", "project_assets")
TILE = os.path.join(ART, "FormalAcademyIndependentFloors32", "academy_block_court_a.png")
UNIT = os.path.join(ART, "FormalUnits64", "pyromancer.png")
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf"]
Z, CELL, FACE = 6, 32, 8
CELL6, FACE6 = CELL * Z, FACE * Z
GOLD, TEXT, DIM, BAND = (255, 214, 110), (240, 240, 244), (178, 178, 188), (60, 70, 92)


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def main():
    tile = Image.open(TILE).convert("RGBA").resize((CELL6, CELL6), Image.NEAREST)
    face = Image.open(TILE).convert("RGBA").crop((0, CELL - FACE, CELL, CELL)).resize((CELL6, FACE6), Image.NEAREST)
    unit = Image.open(UNIT).convert("RGBA")
    big = unit.resize((unit.width * Z, unit.height * Z), Image.NEAREST)
    bbox = unit.getbbox()
    body_h = bbox[3] - bbox[1]

    W, H = 1100, 900
    page = Image.new("RGB", (W, H), (18, 18, 22))
    d = ImageDraw.Draw(page)
    d.text((36, 22), "人物比例基准：火法师（主体 51px = 1.59 格）", font=font(27), fill=TEXT)
    d.text((36, 58), "全部 6 倍显示（1 原生像素 = 6 屏幕像素）；金线是格顶，人物从这里往上冒头",
           font=font(18), fill=DIM)

    base = 720                                  # 脚底/格底
    cx = 300
    page.paste(tile, (cx, base - CELL6))
    page.paste(face, (cx, base))
    page.paste(big, (cx + (CELL6 - big.width) // 2, base - big.height), big)

    top = base - CELL6
    d.line([(cx - 40, top), (cx + CELL6 + 260, top)], fill=GOLD, width=2)
    d.line([(cx - 40, base), (cx + CELL6 + 260, base)], fill=GOLD, width=2)
    d.text((cx + CELL6 + 20, top - 26), "格顶", font=font(17), fill=GOLD)
    d.text((cx + CELL6 + 20, base + 6), "格底（脚底锚点）", font=font(17), fill=GOLD)

    body_top = base - body_h * Z
    d.line([(cx - 150, body_top), (cx - 70, body_top)], fill=(120, 200, 255), width=2)
    d.line([(cx - 110, body_top), (cx - 110, base)], fill=(120, 200, 255), width=1)
    d.text((36, 300), f"主体 {body_h}px", font=font(20), fill=(120, 200, 255))
    d.text((36, 328), f"= {body_h / CELL:.2f} 格", font=font(20), fill=(120, 200, 255))
    d.text((36, 356), f"= {body_h * Z} 屏幕像素", font=font(16), fill=(120, 200, 255))

    d.text((36, 800), f"画布 32×64（{CELL6}×{big.height} 屏幕像素）　主体 {bbox[2]-bbox[0]}×{body_h}"
                      f"　主体/格 = {body_h / CELL:.2f}　冒头 {(body_h - CELL) / CELL:.2f} 格"
                      f"（{(body_h - CELL) * Z} 屏幕像素）", font=font(17), fill=DIM)
    d.text((36, 830), "这是基准：新人形单位的主体高度向 51px 靠（建议 46–58px = 1.4–1.8 格），"
                      "四足与小型单位另行判断。", font=font(17), fill=DIM)

    page.save(os.path.join(OUT, "unit_proportion_reference_pyromancer_6x.png"))
    print("saved unit_proportion_reference_pyromancer_6x.png", page.size, "subject", body_h, "px")


if __name__ == "__main__":
    main()
