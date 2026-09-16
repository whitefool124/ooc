# -*- coding: utf-8 -*-
"""全屏对照：上=你的赛菲莉亚实机截图（1920×1080），下=我方 16px 口径 6 倍样板。

两者同为 1920×1080、同一屏幕像素尺寸，可直接对比颗粒与比例。
参考截图仅比对用，不进 Unity。
"""
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
STUDY = os.path.dirname(HERE)
REF = os.path.join(os.environ["USERPROFILE"], "Videos", "NVIDIA", "Sephiria",
                   "Sephiria Screenshot 2026.09.15 - 16.05.45.98.png")
MINE = os.path.join(HERE, "b16", "sample_with_unit_1920x1080_6x.png")
OUT = os.path.join(HERE, "b16", "compare_fullscreen.png")
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def bar(text, w, h=44):
    im = Image.new("RGB", (w, h), (16, 16, 20))
    ImageDraw.Draw(im).text((12, 8), text, font=font(22), fill=(235, 235, 240))
    return im


def main():
    with Image.open(REF) as im:
        ref = im.convert("RGB").resize((1920, 1080), Image.LANCZOS)
    with Image.open(MINE) as im:
        mine = im.convert("RGB")

    sheet = Image.new("RGB", (1920, 44 + 1080 + 44 + 1080), (12, 12, 16))
    sheet.paste(bar("参考：赛菲莉娅实机 1920×1080（原生 320×180，放大 6 倍）", 1920), (0, 0))
    sheet.paste(ref, (0, 44))
    sheet.paste(bar("我方：一格 16px、放大 6 倍（同为 1920×1080，1 像素 = 6 屏幕像素）", 1920), (0, 44 + 1080))
    sheet.paste(mine, (0, 44 + 1080 + 44))
    sheet.save(OUT)
    print("saved", OUT, sheet.size)


if __name__ == "__main__":
    main()
