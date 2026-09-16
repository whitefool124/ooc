# -*- coding: utf-8 -*-
"""风格样板 v2（6 倍显示）：地面（角色比例尺）+ 草丛 + 木箱/石堆 + 墙角 + 单位占位块。

单位占位块不是美术资产，只用来判断"角色与墙砖的比例"是否符合参考（砖 = 角色高的 1/4）。
"""
import importlib.util
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
SPEC = importlib.util.spec_from_file_location("g3", os.path.join(HERE, "draw_ground_family_v3.py"))
g3 = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(g3)
SPEC2 = importlib.util.spec_from_file_location("gw", os.path.join(HERE, "draw_wall_v1.py"))
gw = importlib.util.module_from_spec(SPEC2)
SPEC2.loader.exec_module(gw)

CELL = 32
ZOOM = 6
CANVAS = (1920, 1080)
VIEW = (16, 80, 1408, 768)
BOARD = (20, 12)
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def grid_board():
    gw_, gh = BOARD
    g = [["g"] * gw_ for _ in range(gh)]
    for y in range(2, 10):
        for x in range(0, 6):
            g[y][x] = "d"
    g[2][5] = "g"
    g[9][4] = "g"
    g[6][6] = "d"
    for y in range(2, 7):
        for x in range(8, 16):
            g[y][x] = "s"
    for i in range(3):
        for y in range(5 - i, 7):
            g[y][16 + i] = "s"
    g[4][10] = "d"
    g[5][13] = "d"
    for y in range(9, 12):
        for x in range(16, 19):
            g[y][x] = "w"
    for x in range(12, 15):
        g[9][x] = "c"
        g[10][x] = "c"
    return ["".join(r) for r in g]


def unit_block(w_native=32, h_native=64):
    im = Image.new("RGBA", (w_native, h_native), (52, 56, 70, 255))
    px = im.load()
    for x in range(w_native):
        for y in (0, 1, h_native - 2, h_native - 1):
            px[x, y] = (22, 20, 26, 255)
    for y in range(h_native):
        for x in (0, 1, w_native - 2, w_native - 1):
            px[x, y] = (22, 20, 26, 255)
    return im


def main():
    grid = grid_board()
    board = g3.render_board(grid, with_facade=True)
    big = board.resize((board.width * ZOOM, board.height * ZOOM), Image.NEAREST).convert("RGBA")

    # 草丛
    tufts = [Image.open(os.path.join(HERE, "decor_grass_tuft_%s_32.png" % n)).convert("RGBA")
             for n in ("a", "b", "c")]
    for gy in range(len(grid)):
        for gx in range(len(grid[0])):
            if grid[gy][gx] != "g":
                continue
            if g3.r01("clump", gx // 3, gy // 3) > 0.42 or g3.r01("intuft", gx, gy) > 0.55:
                continue
            tuft = tufts[g3.h("tuft", gx, gy) % 3]
            ox = gx * CELL * ZOOM + int(g3.r01("ofx", gx, gy) * 8) * ZOOM
            oy = gy * CELL * ZOOM + int(g3.r01("ofy", gx, gy) * 8) * ZOOM
            big.alpha_composite(tuft.resize((CELL * ZOOM, CELL * ZOOM), Image.NEAREST), (ox, oy))

    # 墙角：落在石台地上，格 (10,3)-(11,4)
    corner = gw.build({(0, 0), (1, 0), (1, 1)}, 2, 2, shadow=True)
    big.alpha_composite(corner.resize((corner.width * ZOOM, corner.height * ZOOM), Image.NEAREST),
                        (10 * CELL * ZOOM, 3 * CELL * ZOOM))

    # 物件
    for name, gx, gy in (("prop_crate_32.png", 13, 5), ("prop_rubble_32.png", 9, 6)):
        with Image.open(os.path.join(HERE, name)) as im:
            p = im.convert("RGBA")
        big.alpha_composite(p.resize((CELL * ZOOM, CELL * ZOOM), Image.NEAREST), (gx * CELL * ZOOM, gy * CELL * ZOOM))

    # 单位占位块（32×64，64px 高 = 2 格）
    unit = unit_block()
    ux, uy = 12 * CELL * ZOOM, (6 * CELL - 64) * ZOOM
    big.alpha_composite(unit.resize((unit.width * ZOOM, unit.height * ZOOM), Image.NEAREST), (ux, uy))

    vx, vy, vw, vh = VIEW
    off_x = (big.width - vw) // 2
    off_y = (big.height - vh) // 2
    crop = big.crop((off_x, off_y, off_x + vw, off_y + vh))
    canvas = Image.new("RGB", CANVAS, (28, 28, 35))
    canvas.paste(crop.convert("RGB"), (vx, vy))
    canvas.save(os.path.join(HERE, "style_sample_v2_zoom6.png"))

    # 标注版：把判据写在图上
    ann = canvas.copy()
    d = ImageDraw.Draw(ann)
    d.rectangle([0, 0, CANVAS[0], 62], fill=(20, 20, 26))
    d.text((14, 8), "6 倍显示 · 一格 32px = 192 屏幕像素 · 墙砖 16px = 96 屏幕像素 · 单位占位 64px = 384 屏幕像素",
           font=font(22), fill=(232, 232, 236))
    d.text((14, 36), "砖 : 角色高 = 1 : 4（与参考一致）——参考角色 16px 高、砖 4px，我方角色 64px 高、砖 16px",
           font=font(17), fill=(160, 160, 168))
    ann.save(os.path.join(HERE, "style_sample_v2_zoom6_annotated.png"))
    print("saved style_sample_v2_zoom6.png / _annotated.png", canvas.size)


if __name__ == "__main__":
    main()
