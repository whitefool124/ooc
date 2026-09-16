# -*- coding: utf-8 -*-
"""战场样板：按实机口径（1920×1080、地图区 75%、逻辑格 2 倍显示 = 64px）拼一张整场画面。

用的是 v3 地面族与草丛装饰，目的是在真实倍率下判断"画面是否合格"，
而不是在放大截图里看单个素材。输出带/不带战术网格两版。
"""
import importlib.util
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
SPEC = importlib.util.spec_from_file_location("g3", os.path.join(HERE, "draw_ground_family_v3.py"))
g3 = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(g3)

CELL = 32          # 原生逻辑格
ZOOM = 2           # 默认战场显示倍率
CANVAS = (1920, 1080)
MAP_W = 1440       # 左侧地图区 75%
BOARD = (20, 12)


def h(*keys):
    return g3.h(*keys)


def grid_board():
    gw, gh = BOARD
    g = [["g"] * gw for _ in range(gh)]
    # 左侧泥土空地（不规则边）
    for y in range(2, 10):
        for x in range(0, 6):
            g[y][x] = "d"
    g[2][5] = "g"
    g[9][4] = "g"
    g[6][6] = "d"
    # 中部石台地 + 阶梯 + 缺口
    for y in range(2, 7):
        for x in range(8, 16):
            g[y][x] = "s"
    for i in range(3):
        for y in range(5 - i, 7):
            g[y][16 + i] = "s"
    g[4][10] = "d"
    g[5][13] = "d"
    # 右下木台 + 地毯
    for y in range(9, 12):
        for x in range(16, 19):
            g[y][x] = "w"
    for x in range(12, 15):
        g[9][x] = "c"
        g[10][x] = "c"
    # 中下小石堆
    for y in (10, 11):
        for x in (8, 9):
            g[y][x] = "s"
    return ["".join(r) for r in g]


def tuft_variant(gx, gy):
    return g3.h("tuft", gx, gy) % 3


def main():
    grid = grid_board()
    board = g3.render_board(grid, with_facade=True)
    bw, bh = board.width * ZOOM, board.height * ZOOM
    big = board.resize((bw, bh), Image.NEAREST)

    tufts = [Image.open(os.path.join(HERE, "decor_grass_tuft_%s_32.png" % n)).convert("RGBA")
             for n in ("a", "b", "c")]

    # 草丛成簇分布：先在 3×3 格区块里决定是否有草簇，再在区块内落到部分格上
    overlay = Image.new("RGBA", big.size, (0, 0, 0, 0))
    for gy in range(len(grid)):
        for gx in range(len(grid[0])):
            if grid[gy][gx] != "g":
                continue
            if g3.r01("clump", gx // 3, gy // 3) > 0.42:
                continue
            if g3.r01("intuft", gx, gy) > 0.55:
                continue
            tuft = tufts[tuft_variant(gx, gy)]
            ox = gx * CELL * ZOOM + int(g3.r01("ofx", gx, gy) * 8) * ZOOM
            oy = gy * CELL * ZOOM + int(g3.r01("ofy", gx, gy) * 8) * ZOOM
            overlay.alpha_composite(tuft.resize((CELL * ZOOM, CELL * ZOOM), Image.NEAREST), (ox, oy))
    big = big.convert("RGBA")
    big.alpha_composite(overlay)

    for with_grid in (False, True):
        canvas = Image.new("RGB", CANVAS, (35, 35, 43))
        x0 = 80
        y0 = 120
        canvas.paste(big.convert("RGB"), (x0, y0))
        if with_grid:
            d = ImageDraw.Draw(canvas, "RGBA")
            for cx in range(len(grid[0]) + 1):
                px = x0 + cx * CELL * ZOOM
                d.line([(px, y0), (px, y0 + len(grid) * CELL * ZOOM)], fill=(180, 220, 255, 46), width=ZOOM)
            for cy in range(len(grid) + 1):
                py = y0 + cy * CELL * ZOOM
                d.line([(x0, py), (x0 + len(grid[0]) * CELL * ZOOM, py)], fill=(180, 220, 255, 46), width=ZOOM)
        name = "battlefield_mock_1920_grid.png" if with_grid else "battlefield_mock_1920.png"
        canvas.save(os.path.join(HERE, name))
        print("saved", name, canvas.size)

    big.convert("RGB").crop((0, 0, 960, 560)).save(os.path.join(HERE, "battlefield_mock_zoom.png"))
    print("board", board.size, "-> display", (bw, bh), "cells", BOARD)


if __name__ == "__main__":
    main()
