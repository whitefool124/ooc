# -*- coding: utf-8 -*-
"""地块过渡打磨对照：横/竖两种交界，旧写法 vs 新写法，6 倍取景。"""
import importlib.util
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
SPEC = importlib.util.spec_from_file_location("b16", os.path.join(HERE, "draw_battle16.py"))
b16 = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(b16)
OUT = os.path.join(HERE, "b16")
CELL = b16.CELL
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def scene():
    """4×4：上两行石、下两行草（横交界）；左两列土、右两列草（竖交界）。"""
    g = []
    for y in range(4):
        row = []
        for x in range(4):
            row.append("s" if y < 2 else "g")
        g.append("".join(row))
    return b16.render_board(g, seed=9)


def crop_at(im, cx, cy, w, h):
    """以原生像素坐标为中心裁切。"""
    x0 = max(0, min(im.width - w, cx - w // 2))
    y0 = max(0, min(im.height - h, cy - h // 2))
    return im.crop((x0, y0, x0 + w, y0 + h))


def main():
    b16.OLD_STAMP = True
    old = scene()
    b16.OLD_STAMP = False
    new = scene()
    z = 6
    W, H = 40, 30
    pairs = [
        ("横向交界 · 旧", crop_at(old, 2 * CELL, 2 * CELL, W, H)),
        ("横向交界 · 新", crop_at(new, 2 * CELL, 2 * CELL, W, H)),
        ("竖向交界 · 旧", crop_at(old, 2 * CELL, 3 * CELL, W, H)),
        ("竖向交界 · 新", crop_at(new, 2 * CELL, 3 * CELL, W, H)),
    ]
    gap, top = 20, 34
    w = (W * z) * len(pairs) + gap * (len(pairs) - 1)
    h = top + H * z
    sheet = Image.new("RGB", (w, h), (18, 18, 22))
    x = 0
    for label, im in pairs:
        big = im.resize((im.width * z, im.height * z), Image.NEAREST)
        sheet.paste(big, (x, top))
        ImageDraw.Draw(sheet).text((x, 8), label, font=font(18), fill=(235, 235, 240))
        x += big.width + gap
    sheet.save(os.path.join(OUT, "boundary_polish_6x.png"))
    print("saved boundary_polish_6x.png", sheet.size)


if __name__ == "__main__":
    main()
