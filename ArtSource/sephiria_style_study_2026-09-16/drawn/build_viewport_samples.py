# -*- coding: utf-8 -*-
"""同一战场的三档显示倍率样板：2 / 4 / 6 倍，战场视口 1408×768（规格表 ART-VIEWPORT-OVERVIEW）。

像素颗粒度只由显示倍率决定：32px 素材在 2 倍下每个像素 = 2 屏幕像素，
在 6 倍下 = 6 屏幕像素（与赛道菲莉娅一致）。素材本身不变。
"""
import importlib.util
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
SPEC = importlib.util.spec_from_file_location("g3", os.path.join(HERE, "draw_ground_family_v3.py"))
g3 = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(g3)

CELL = 32
CANVAS = (1920, 1080)
VIEW = (16, 80, 1408, 768)      # 规格表：视口 16/80/1408/768
BOARD = (20, 12)
ZOOMS = (2, 4, 6)


def grid_board():
    gw, gh = BOARD
    g = [["g"] * gw for _ in range(gh)]
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
    for y in (10, 11):
        for x in (8, 9):
            g[y][x] = "s"
    return ["".join(r) for r in g]


def place_props(big, z):
    """把物件按底部中心锚点落到格上，并补 1px 接地点影（影子是场景层，不烘进素材）。"""
    props = [("prop_crate_32.png", 9, 4), ("prop_crate_32.png", 12, 6),
             ("prop_rubble_32.png", 7, 7), ("prop_rubble_32.png", 10, 8)]
    for name, gx, gy in props:
        with Image.open(os.path.join(HERE, name)) as im:
            im = im.convert("RGBA")
        scale_px = CELL * z
        big.alpha_composite(im.resize((scale_px, scale_px), Image.NEAREST), (gx * scale_px, gy * scale_px))
        px = big.load()
        for x in range(gx * scale_px + int(scale_px * 0.18), gx * scale_px + int(scale_px * 0.82)):
            for y in range((gy + 1) * scale_px - max(1, z), (gy + 1) * scale_px):
                if 0 <= x < big.width and 0 <= y < big.height:
                    r, g, b, a = px[x, y]
                    px[x, y] = (int(r * 0.55), int(g * 0.55), int(b * 0.55), a)


def render(view_zoom, with_grid=False, props=False):
    grid = grid_board()
    board = g3.render_board(grid, with_facade=True)
    z = view_zoom
    big = board.resize((board.width * z, board.height * z), Image.NEAREST).convert("RGBA")

    tufts = [Image.open(os.path.join(HERE, "decor_grass_tuft_%s_32.png" % n)).convert("RGBA")
             for n in ("a", "b", "c")]
    for gy in range(len(grid)):
        for gx in range(len(grid[0])):
            if grid[gy][gx] != "g":
                continue
            if g3.r01("clump", gx // 3, gy // 3) > 0.42:
                continue
            if g3.r01("intuft", gx, gy) > 0.55:
                continue
            tuft = tufts[g3.h("tuft", gx, gy) % 3]
            ox = gx * CELL * z + int(g3.r01("ofx", gx, gy) * 8) * z
            oy = gy * CELL * z + int(g3.r01("ofy", gx, gy) * 8) * z
            big.alpha_composite(tuft.resize((CELL * z, CELL * z), Image.NEAREST), (ox, oy))

    if props:
        place_props(big, z)

    vx, vy, vw, vh = VIEW
    if big.width >= vw and big.height >= vh:
        crop = big.crop(((big.width - vw) // 2, (big.height - vh) // 2,
                         (big.width - vw) // 2 + vw, (big.height - vh) // 2 + vh))
    else:
        crop = Image.new("RGBA", (vw, vh), (0, 0, 0, 0))
        crop.alpha_composite(big, ((vw - big.width) // 2, (vh - big.height) // 2))

    canvas = Image.new("RGB", CANVAS, (28, 28, 35))
    canvas.paste(crop.convert("RGB"), (vx, vy))
    step = CELL * z
    if with_grid:
        d = ImageDraw.Draw(canvas, "RGBA")
        ox = vx - (big.width - vw) // 2 if big.width >= vw else vx - (vw - big.width) // 2
        oy = vy - (big.height - vh) // 2 if big.height >= vh else vy - (vh - big.height) // 2
        x = ox
        while x <= vx + vw:
            if x >= vx:
                d.line([(x, vy), (x, vy + vh)], fill=(180, 220, 255, 46), width=max(1, z - 1))
            x += step
        y = oy
        while y <= vy + vh:
            if y >= vy:
                d.line([(vx, y), (vx + vw, y)], fill=(180, 220, 255, 46), width=max(1, z - 1))
            y += step
    return canvas, (vw // step, vh // step)


def set_feature_scale(fine):
    """切换结构类特征的尺度。细档 = 6 倍显示下等价于粗档在 2 倍显示下的每格密度。"""
    if fine:
        g3.STONE_PITCH, g3.STONE_SEAM = 8, 2
        g3.WOOD_PITCH, g3.WOOD_SEAM = 8, 2
        g3.DIRT_PERIODS = [(8, 14), (10, 16), (8, 18), (10, 14), (12, 18), (8, 16)]
        g3.BOUNDARY_DIV = 2
    else:
        g3.STONE_PITCH, g3.STONE_SEAM = 16, 3
        g3.WOOD_PITCH, g3.WOOD_SEAM = 16, 3
        g3.DIRT_PERIODS = [(4, 7), (5, 8), (4, 9), (5, 7), (6, 9), (4, 8)]
        g3.BOUNDARY_DIV = 1
    g3.rebuild_cache()


def main():
    cells = {}
    # 尺度统一用主族锁定值（8px 砖距），只变显示倍率；6 倍是选定口径
    jobs = [(2, True), (4, True), (6, True)]
    for z, fine in jobs:
        set_feature_scale(fine)
        img, visible = render(z)
        name = "viewport_zoom%d%s.png" % (z, "_fine" if fine else "")
        img.save(os.path.join(HERE, name))
        img, _ = render(z, with_grid=True)
        img.save(os.path.join(HERE, "viewport_zoom%d%s_grid.png" % (z, "_fine" if fine else "")))
        cells[(z, fine)] = visible
        print("%s -> 1 pixel = %d screen px, viewport shows %dx%d cells, 砖距 %dpx"
              % (name, z, visible[0], visible[1], g3.STONE_PITCH))

    panels = []
    for z, fine in jobs:
        name = "viewport_zoom%d%s.png" % (z, "_fine" if fine else "")
        with Image.open(os.path.join(HERE, name)) as im:
            vx, vy, vw, vh = VIEW
            panels.append(im.convert("RGB").crop((vx, vy, vx + vw, vy + vh)).resize((680, 371), Image.NEAREST))
    # 6 倍 + 物件的落地场景
    set_feature_scale(True)
    img, _ = render(6, props=True)
    img.save(os.path.join(HERE, "viewport_zoom6_props.png"))
    print("saved viewport_zoom6_props.png (6 倍 + 木箱/石堆落地)")
    gap = 12
    sheet = Image.new("RGB", (680 * len(panels) + gap * (len(panels) - 1), 371), (18, 18, 22))
    for i, p in enumerate(panels):
        sheet.paste(p, (i * (680 + gap), 0))
    sheet.save(os.path.join(HERE, "viewport_zoom_compare.png"))
    print("saved viewport_zoom_compare.png (2x / 4x / 6x / 6x+细特征)")


if __name__ == "__main__":
    main()
