# -*- coding: utf-8 -*-
"""把导出的赛菲莉亚瓦片拼成带标注的图鉴（8 倍），看清真实画法。"""
import os

from PIL import Image, ImageDraw, ImageFont

STUDY = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(STUDY, "sephiria_extract", "tiles")
OUT = os.path.join(STUDY, "sephiria_extract")
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def main():
    items = [
        ("CaveFloor_10", "洞穴地面 16×16"),
        ("CaveFloor_19", "洞穴地面 16×16"),
        ("CaveFloor_32", "洞穴地面 16×16"),
        ("JailGround0_10", "牢房地面 16×16"),
        ("TownCliffTile_6", "城镇悬崖 16×16"),
        ("TownCliffTile_15", "城镇悬崖 16×16"),
        ("CaveWall_0", "洞穴墙 16×24"),
        ("CaveWall_4", "洞穴墙 16×24"),
        ("CaveWall_7", "洞穴墙 16×24"),
        ("Cliff_GroundShadow_Bay", "悬崖地影 16×16"),
        ("Qliphoth_Shadow_Tile_0", "阴影瓦片 16×32"),
        ("CostumeTile0_17", "装饰瓦片 16×16"),
    ]
    z, gap, top = 8, 16, 30
    cell = 44 * z // 2      # 统一格位（16×24 也能放下）
    cellw, cellh = 16 * z, 24 * z
    cols = 6
    rows = (len(items) + cols - 1) // cols
    W = cols * cellw + (cols - 1) * gap
    H = top + rows * cellh + (rows - 1) * gap + 26
    sheet = Image.new("RGB", (W, H), (20, 20, 24))
    d = ImageDraw.Draw(sheet)
    d.text((8, 6), "赛菲莉亚实机瓦片（从游戏资源导出，仅作规格学习）", font=font(20), fill=(235, 235, 240))
    for i, (name, label) in enumerate(items):
        path = None
        for f in os.listdir(SRC):
            if f.startswith(name + "_"):
                path = os.path.join(SRC, f)
        if not path:
            continue
        with Image.open(path) as im:
            im = im.convert("RGBA")
        big = im.resize((im.width * z, im.height * z), Image.NEAREST)
        r, c = divmod(i, cols)
        x = c * (cellw + gap)
        y = top + r * (cellh + gap) + (cellh - big.height)
        sheet.paste(big, (x, y), big)
        d.text((x, y + big.height + 2), label, font=font(14), fill=(180, 180, 188))
    sheet.save(os.path.join(OUT, "sephiria_tiles_8x.png"))
    print("saved", os.path.join(OUT, "sephiria_tiles_8x.png"), sheet.size)


if __name__ == "__main__":
    main()
