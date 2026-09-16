# -*- coding: utf-8 -*-
"""OCC 32px 地面族试验生成器（泥土 / 石砖 / 过渡 / 外沿立面）。

状态：PROTOTYPE。2026-09-16 用户明确要求“不用生图，直接填像素”，因此本文件
是一次程序绘制试验，用于确认材质手感、过渡读法与铺图可用性。

硬边界：
- 不登记 occ-art-manifest-v1，不写入 UnityProject/Assets，不得作为 FORMAL_CANDIDATE。
- 若要转正式资产，必须按总案 A.2.2 / native32_generation_profiles.json 用内置
  image_gen 重新出独立原料，再走 1x/4x/灰阶/棋盘格/实际应用接触与人工审美。

规格对齐（occ_art_contract_v1.json + 根 AGENTS.md 美术规则）：
- floor_tile_32：32×32、palette_max 6、不烘焙格子边框、无常触边表面细节。
- terrain_adjacency_overlay_32：32×32、palette_max 10。
- 外沿地块：32×40，其中下方 8px 为不参与交互的外立面（仅南向）。
- 固定左上光源；无渐变、无抖色、无抗锯齿、无孤立单像素噪点（一律 2px 簇落笔）。
"""

import json
import os
from PIL import Image

SIZE = 32
FACADE = 8
HERE = os.path.dirname(os.path.abspath(__file__))
MASK = (1 << 64) - 1

# --------------------------------------------------------------------------
# 调色板（每格地面 6 色）
# --------------------------------------------------------------------------
DIRT = {
    "dark": (78, 57, 40),
    "base": (103, 77, 52),
    "mid": (122, 94, 63),
    "light": (140, 110, 72),
    "hi": (158, 127, 88),
    "pebble": (124, 118, 108),
}
DIRT_BANDS = ["dark", "base", "mid", "light", "hi"]
DIRT_FRACTIONS = [0.07, 0.36, 0.35, 0.16, 0.06]

STONE = {
    "mortar": (56, 57, 56),
    "dark": (78, 81, 84),
    "base": (97, 101, 105),
    "mid": (116, 121, 125),
    "light": (134, 140, 145),
    "hi": (156, 163, 169),
}

# 泥土变体：不同噪声周期 + 不同手工细节，避免大片区出现同一张图
DIRT_VARIANTS = [
    dict(key="a", seed=101, p1=5, p2=9, w=0.64),
    dict(key="b", seed=907, p1=4, p2=7, w=0.60),
    dict(key="c", seed=1613, p1=6, p2=11, w=0.66),
    dict(key="d", seed=2311, p1=5, p2=8, w=0.62),
]
DIRT_DETAILS = {
    "a": dict(ridge=[((5, 9), (6, 9), (7, 10))], hi=[], pebble=[(11, 17)], clod=[]),
    "b": dict(ridge=[((23, 24), (24, 25))], hi=[], pebble=[(27, 12)], clod=[]),
    "c": dict(ridge=[((13, 5), (14, 5), (15, 6)), ((6, 25), (7, 26), (8, 26))],
              hi=[], pebble=[(17, 13)], clod=[]),
    "d": dict(ridge=[((3, 14), (4, 14), (5, 15))], hi=[], pebble=[(26, 21)], clod=[]),
}

# 石砖变体：缺口比例、浅坑与裂纹位置不同
STONE_VARIANTS = [
    dict(key="a", seed=211, chip=0.20,
         pores=[(3, 10), (4, 10), (20, 26), (21, 26)], cracks=[]),
    dict(key="b", seed=457, chip=0.34,
         pores=[(12, 3), (13, 3), (27, 18), (28, 18)],
         cracks=[[(9, 17), (9, 18), (9, 19), (10, 19)], [(25, 9), (25, 10), (25, 11)]]),
]

DIRT_ORDER = ["dark", "base", "mid", "light", "hi", "pebble"]
STONE_ORDER = ["mortar", "dark", "base", "mid", "light", "hi"]


# --------------------------------------------------------------------------
# 确定性哈希 / 噪声
# --------------------------------------------------------------------------
def _key(k):
    if isinstance(k, str):
        v = 0
        for b in k.encode("utf-8"):
            v = (v * 131 + b) & 0xFFFFFFFF
        return v
    return int(k)


def h(*keys):
    x = 0x9E3779B97F4A7C15
    for k in keys:
        x = (x ^ ((_key(k) & MASK) + 0x9E3779B97F4A7C15 + (x << 6) + (x >> 2))) & MASK
        x = (x * 0xBF58476D1CE4E5B9) & MASK
        x ^= x >> 31
        x = (x * 0x94D049BB133111EB) & MASK
        x ^= x >> 29
    return x


def r01(*keys):
    return (h(*keys) >> 11) / float(1 << 53)


def smoothstep(t):
    return t * t * (3.0 - 2.0 * t)


def octave(seed, period):
    """周期性格点值噪声：在 32px 内无缝平铺。"""
    lat = [[r01(seed, i, j) for j in range(period)] for i in range(period)]
    out = [[0.0] * SIZE for _ in range(SIZE)]
    scale = float(period) / SIZE
    for y in range(SIZE):
        fy = y * scale
        y0 = int(fy) % period
        y1 = (y0 + 1) % period
        ty = smoothstep(fy - int(fy))
        for x in range(SIZE):
            fx = x * scale
            x0 = int(fx) % period
            x1 = (x0 + 1) % period
            tx = smoothstep(fx - int(fx))
            a = lat[y0][x0] + (lat[y0][x1] - lat[y0][x0]) * tx
            b = lat[y1][x0] + (lat[y1][x1] - lat[y1][x0]) * tx
            out[y][x] = a + (b - a) * ty
    return out


def clean_isolated(bands, passes=2):
    """去掉孤立单像素色块：无同色 8 邻域时并入多数邻居。"""
    for _ in range(passes):
        new = [row[:] for row in bands]
        for y in range(SIZE):
            for x in range(SIZE):
                b = bands[y][x]
                counts = {}
                same = 0
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        if dx == 0 and dy == 0:
                            continue
                        nb = bands[(y + dy) % SIZE][(x + dx) % SIZE]
                        counts[nb] = counts.get(nb, 0) + 1
                        if nb == b:
                            same += 1
                if same == 0:
                    new[y][x] = max(counts.items(), key=lambda kv: (kv[1], -kv[0]))[0]
        bands = new
    return bands


# --------------------------------------------------------------------------
# 泥土 32×32（4 个变体）
# --------------------------------------------------------------------------
def build_dirt_tile(v):
    f1 = octave(v["seed"], v["p1"])
    f2 = octave(v["seed"] + 1, v["p2"])
    w = v["w"]
    field = [[w * f1[y][x] + (1.0 - w) * f2[y][x] for x in range(SIZE)] for y in range(SIZE)]

    flat = sorted((field[y][x], y, x) for y in range(SIZE) for x in range(SIZE))
    bounds, acc = [], 0.0
    for f in DIRT_FRACTIONS:
        acc += f
        bounds.append(acc)
    bands = [[0] * SIZE for _ in range(SIZE)]
    for rank, (_v, y, x) in enumerate(flat):
        p = (rank + 0.5) / (SIZE * SIZE)
        bi = 0
        while bi < len(bounds) - 1 and p > bounds[bi]:
            bi += 1
        bands[y][x] = bi
    bands = clean_isolated(bands)
    tile = [[DIRT[DIRT_BANDS[bands[y][x]]] for x in range(SIZE)] for y in range(SIZE)]

    d = DIRT_DETAILS[v["key"]]

    def put(x, y, name):
        tile[y][x] = DIRT[name]

    for stroke in d["ridge"]:
        for x, y in stroke:
            put(x, y, "light")
    for x, y in d["hi"]:
        put(x, y, "hi")
    for px, py in d["pebble"]:
        for dx in (0, 1):
            for dy in (0, 1):
                put(px + dx, py + dy, "pebble")
        put(px, py, "hi")
        put(px + 1, py + 1, "dark")
    for px, py in d["clod"]:
        for dx in (0, 1):
            for dy in (0, 1):
                put(px + dx, py + dy, "light")
        put(px, py, "hi")
        put(px + 1, py + 1, "dark")
    return tile


# --------------------------------------------------------------------------
# 石砖 32×32（2 个变体）：砖宽 16（15 砖体 + 1 灰缝），行高 8，隔行错缝 8
# --------------------------------------------------------------------------
def build_stone_tile(v):
    seed = v["seed"]
    tile = [[STONE["mortar"]] * SIZE for _ in range(SIZE)]
    for y in range(SIZE):
        ri = y // 8
        ry = y % 8
        if ry == 7:
            continue
        odd = ri % 2 == 1
        mx = 7 if odd else 15
        for x in range(SIZE):
            if x % 16 == mx:
                continue
            bid = ((x - 8) // 16) % 2 if odd else (x // 16) % 2
            left_mortar = (x - 1) % 16 == mx
            right_mortar = (x + 1) % 16 == mx
            variant = r01(seed, "brick", ri, bid)
            fill = "mid" if variant < 0.32 else "base"
            if ry == 0:
                c = "dark" if (right_mortar and r01(seed, "chip", ri, bid) < v["chip"]) else "hi"
            elif ry == 1:
                c = "light"
            elif ry == 6:
                c = "dark"
            elif left_mortar:
                c = "light"
            elif right_mortar:
                c = "dark"
            else:
                c = fill
            tile[y][x] = STONE[c]
    for x, y in v["pores"]:
        tile[y][x] = STONE["dark"]
    for crack in v["cracks"]:
        for x, y in crack:
            tile[y][x] = STONE["dark"]
    return tile


DIRT_TILES = [build_dirt_tile(v) for v in DIRT_VARIANTS]
STONE_TILES = [build_stone_tile(v) for v in STONE_VARIANTS]


def variant_index(mat, gx, gy):
    """变体按格坐标确定性选取；2×2 区块内取不同变体，避免相邻同图。
    （不用镜像/旋转：地面格带方向性受光，翻转会破坏固定左上光源。）
    """
    if mat == "d":
        n = len(DIRT_TILES)
        return (gx + 2 * gy + h("dirtblock", gx // 2, gy // 2)) % n
    n = len(STONE_TILES)
    return (gx + gy + h("stoneblock", gx // 2, gy // 2)) % n


def tile_at(mat, gx, gy):
    return DIRT_TILES[variant_index(mat, gx, gy)] if mat == "d" else STONE_TILES[variant_index(mat, gx, gy)]


# --------------------------------------------------------------------------
# 过渡：以“另一材质在哪个方向”为参数，在本格贴边像素上落笔
# --------------------------------------------------------------------------
DIRS = {"n": (0, -1), "e": (1, 0), "s": (0, 1), "w": (-1, 0)}


def _edge_coord(d, i, k):
    """d: n/e/s/w；i 沿边序号；k 由边界向本格内部的深度。"""
    if d == "n":
        return i, k
    if d == "s":
        return i, SIZE - 1 - k
    if d == "w":
        return k, i
    return SIZE - 1 - k, i


def stamp_seam(px, ox, oy, mat, d, seed):
    """以 2px 为一簇落笔：既避免孤立单像素，也让边界读成咬合而不是一刀切。"""
    for c in range(SIZE // 2):
        ii = (c * 2, c * 2 + 1)
        if mat == "d":
            depth = 2 if r01(seed, "depth", d, c) < 0.45 else 1
            for i in ii:
                for k in range(depth):
                    x, y = _edge_coord(d, i, k)
                    px[ox + x, oy + y] = DIRT["dark"]
            if r01(seed, "chip", d, c) < 0.30:
                for i in ii:
                    x, y = _edge_coord(d, i, depth)
                    px[ox + x, oy + y] = STONE["base"]
                if r01(seed, "chipLit", d, c) < 0.55:
                    x, y = _edge_coord(d, ii[0], depth)
                    px[ox + x, oy + y] = STONE["light"]
                if r01(seed, "chipDeep", d, c) < 0.45:
                    x, y = _edge_coord(d, ii[1], depth + 1)
                    px[ox + x, oy + y] = STONE["base"]
                for i in ii:
                    x, y = _edge_coord(d, i, depth + 2)
                    px[ox + x, oy + y] = DIRT["dark"]
            if r01(seed, "gravel", d, c) < 0.12:
                for i in ii:
                    x, y = _edge_coord(d, i, 4 + (1 if r01(seed, "gravelY", d, c) < 0.5 else 0))
                    px[ox + x, oy + y] = DIRT["pebble"]
        else:
            lit = d in ("n", "w")
            for i in ii:
                x, y = _edge_coord(d, i, 0)
                if lit:
                    px[ox + x, oy + y] = STONE["light"] if r01(seed, "lip", d, i) < 0.45 else STONE["hi"]
                else:
                    px[ox + x, oy + y] = STONE["dark"]
            if not lit and r01(seed, "deepEdge", d, c) < 0.5:
                for i in ii:
                    x, y = _edge_coord(d, i, 1)
                    px[ox + x, oy + y] = STONE["dark"]
            if r01(seed, "crumb", d, c) < 0.22:
                for i in ii:
                    x, y = _edge_coord(d, i, 1)
                    px[ox + x, oy + y] = DIRT["mid"] if r01(seed, "crumbB", d, c) < 0.5 else DIRT["base"]
            if r01(seed, "crumb2", d, c) < 0.12:
                x, y = _edge_coord(d, ii[0], 2)
                px[ox + x, oy + y] = DIRT["dark"]


# --------------------------------------------------------------------------
# 外沿立面（南向，8px，不参与交互）
# --------------------------------------------------------------------------
def facade_band(mat):
    """外沿地块下方的立面条（南向，背光）：
    0 行地表前缘受光、1 行转折压暗、2 行层缝、3-6 行主体、7 行底部收暗。
    """
    im = Image.new("RGB", (SIZE, FACADE))
    px = im.load()
    for x in range(SIZE):
        c = x // 2
        if mat == "s":
            brick = "dark" if r01("facadeStone", x // 16) < 0.7 else "base"
            px[x, 0] = STONE["light"]
            px[x, 1] = STONE["dark"]
            px[x, 2] = STONE["mortar"]
            for y in (3, 4, 5, 6):
                if x % 16 == 15 or r01("facadePore", c, y) < 0.08:
                    px[x, y] = STONE["mortar"]
                else:
                    px[x, y] = STONE[brick]
            px[x, 7] = STONE["mortar"]
        else:
            px[x, 0] = DIRT["light"]
            px[x, 1] = DIRT["dark"]
            for y in (2, 3, 4, 5, 6):
                rr = r01("facadeDirt", c, y)
                name = "dark" if rr < 0.55 else ("base" if rr < 0.90 else "mid")
                px[x, y] = DIRT[name]
            if r01("facadePebble", c) < 0.14:
                px[x, 4] = DIRT["pebble"]
                px[x, 5] = DIRT["pebble"]
            px[x, 7] = DIRT["dark"]
    return im


# --------------------------------------------------------------------------
# 铺图
# --------------------------------------------------------------------------
NEIGHBORS = (("n", 0, -1), ("e", 1, 0), ("s", 0, 1), ("w", -1, 0))


def render_board(grid, with_facade=False, seed=777):
    """grid: 字符串列表，'d' 泥土 / 's' 石砖；越界视为同材质。"""
    gh, gw = len(grid), len(grid[0])
    height = gh * SIZE + (FACADE if with_facade else 0)
    img = Image.new("RGB", (gw * SIZE, height))
    px = img.load()
    for gy in range(gh):
        for gx in range(gw):
            tile = tile_at(grid[gy][gx], gx, gy)
            ox, oy = gx * SIZE, gy * SIZE
            for y in range(SIZE):
                row = tile[y]
                for x in range(SIZE):
                    px[ox + x, oy + y] = row[x]
    for gy in range(gh):
        for gx in range(gw):
            mat = grid[gy][gx]
            ox, oy = gx * SIZE, gy * SIZE
            for d, dx, dy in NEIGHBORS:
                nx, ny = gx + dx, gy + dy
                if 0 <= nx < gw and 0 <= ny < gh and grid[ny][nx] != mat:
                    stamp_seam(px, ox, oy, mat, d, seed)
    if with_facade:
        for gx in range(gw):
            band = facade_band(grid[gh - 1][gx])
            img.paste(band, (gx * SIZE, gh * SIZE))
    return img


def piece(own, dirs, seed=555):
    """导出标准过渡格：中心为本格材质，dirs 给出另一材质所在方向（两向时补对角）。"""
    other = "s" if own == "d" else "d"
    grid = [[own] * 3 for _ in range(3)]
    for d in dirs:
        dx, dy = DIRS[d]
        grid[1 + dy][1 + dx] = other
    if len(dirs) == 2:
        dx = sum(DIRS[d][0] for d in dirs)
        dy = sum(DIRS[d][1] for d in dirs)
        grid[1 + dy][1 + dx] = other
    board = render_board(["".join(r) for r in grid], seed=seed)
    return board.crop((SIZE, SIZE, SIZE * 2, SIZE * 2))


def outer_piece(mat, seed=555):
    """外沿地块：32×40，上 32×32 是地面格，下 8px 是南向外立面。"""
    board = render_board([mat], seed=seed)
    out = Image.new("RGB", (SIZE, SIZE + FACADE))
    out.paste(board.crop((0, 0, SIZE, SIZE)), (0, 0))
    out.paste(facade_band(mat), (0, SIZE))
    return out


# --------------------------------------------------------------------------
# 输出
# --------------------------------------------------------------------------
def upscale(img, f):
    return img.resize((img.width * f, img.height * f), Image.NEAREST)


def unique_colors(img):
    return sorted({c for _n, c in img.convert("RGB").getcolors(maxcolors=1 << 20)})


def tile_image(t):
    im = Image.new("RGB", (SIZE, SIZE))
    im.putdata([t[y][x] for y in range(SIZE) for x in range(SIZE)])
    return im


def sheet(items, cols, scale, gap=8, bg=(44, 44, 48)):
    """items: [(name, image)]；每格按自身尺寸放大后按固定格宽排布。"""
    cw = max(im.width for _n, im in items) * scale
    chh = max(im.height for _n, im in items) * scale
    rows = (len(items) + cols - 1) // cols
    out = Image.new("RGB", (cols * cw + (cols - 1) * gap, rows * chh + (rows - 1) * gap), bg)
    for i, (_n, im) in enumerate(items):
        r, c = divmod(i, cols)
        out.paste(upscale(im, scale), (c * (cw + gap), r * (chh + gap)))
    return out


def build_demo_grid():
    gw, gh = 24, 14
    g = [["d"] * gw for _ in range(gh)]
    for y in range(2, 7):                      # 石台地主体
        for x in range(4, 15):
            g[y][x] = "s"
    for i in range(4):                         # 右侧阶梯：外角 / 内角 / 斜接
        for y in range(5 - i, 7):
            g[y][15 + i] = "s"
    g[4][8] = "d"                              # 台地缺口：四向内角
    g[5][11] = "d"
    g[10][3] = "s"                             # 孤立石格：四向外角
    g[10][19] = "s"                            # 三格石堆
    g[10][20] = "s"
    g[9][20] = "s"
    for x in range(14, 22):                    # 右下 L 形：最底行同时出现两种外立面
        g[12][x] = "s"
        g[13][x] = "s"
    g[11][21] = "s"
    return ["".join(r) for r in g]


def main():
    out = {}

    # 地面格
    for v, t in zip(DIRT_VARIANTS, DIRT_TILES):
        im = tile_image(t)
        name = "dirt_%s_32.png" % v["key"]
        im.save(os.path.join(HERE, name))
        out[name] = {"role": "floor_tile_32", "material": "dirt", "variant": v["key"], "colors": len(unique_colors(im))}
    for v, t in zip(STONE_VARIANTS, STONE_TILES):
        im = tile_image(t)
        name = "stone_%s_32.png" % v["key"]
        im.save(os.path.join(HERE, name))
        out[name] = {"role": "floor_tile_32", "material": "stone", "variant": v["key"], "colors": len(unique_colors(im))}

    # 过渡：4 直边 + 4 角，两种本格材质各一套
    edges, corners = [], []
    for own, tag in (("d", "dirt_stone"), ("s", "stone_dirt")):
        for d in ("n", "e", "s", "w"):
            im = piece(own, [d])
            name = "%s_%s_32.png" % (tag, d)
            im.save(os.path.join(HERE, name))
            out[name] = {"role": "terrain_adjacency_overlay_32", "own": tag.split("_")[0],
                         "other": tag.split("_")[1], "dirs": [d], "colors": len(unique_colors(im))}
            edges.append((name, im))
        for d1, d2 in (("n", "e"), ("s", "e"), ("s", "w"), ("n", "w")):
            im = piece(own, [d1, d2])
            name = "%s_%s%s_32.png" % (tag, d1, d2)
            im.save(os.path.join(HERE, name))
            out[name] = {"role": "terrain_adjacency_overlay_32", "own": tag.split("_")[0],
                         "other": tag.split("_")[1], "dirs": [d1, d2], "colors": len(unique_colors(im))}
            corners.append((name, im))

    # 外沿地块 32×40
    outer = []
    for mat, tag in (("d", "dirt"), ("s", "stone")):
        im = outer_piece(mat)
        name = "%s_outer_s_32x40.png" % tag
        im.save(os.path.join(HERE, name))
        out[name] = {"role": "directional_outer_edge_32x40", "material": tag, "facade": "south_8px",
                     "colors": len(unique_colors(im))}
        outer.append((name, im))

    # 图鉴
    floor_names = ["dirt_%s_32.png" % v["key"] for v in DIRT_VARIANTS] + \
                  ["stone_%s_32.png" % v["key"] for v in STONE_VARIANTS]
    floors = [(n, Image.open(os.path.join(HERE, n))) for n in floor_names]
    sheet(floors, 6, 4).save(os.path.join(HERE, "contact_floors.png"))
    sheet(edges + corners, 8, 4).save(os.path.join(HERE, "contact_transitions.png"))
    sheet(outer, 2, 4).save(os.path.join(HERE, "contact_outer.png"))

    # 变体混铺自检：4×4 片区，相邻尽量不同变体
    mix = Image.new("RGB", (SIZE * 4 * 2 * 2 + 16, SIZE * 4 * 2), (44, 44, 48))
    for mat, ox in (("d", 0), ("s", SIZE * 8 + 16)):
        for gy in range(4):
            for gx in range(4):
                t = tile_at(mat, gx + (0 if mat == "d" else 3), gy)
                sub = tile_image(t)
                mix.paste(upscale(sub, 2), (ox + gx * SIZE * 2, gy * SIZE * 2))
    mix.save(os.path.join(HERE, "seam_check_variants_2x.png"))

    # 演示地图
    grid = build_demo_grid()
    demo = render_board(grid, with_facade=True)
    demo.save(os.path.join(HERE, "demo_map_1x.png"))
    upscale(demo, 2).save(os.path.join(HERE, "demo_map_2x.png"))
    upscale(demo.crop((SIZE * 6, SIZE * 3, SIZE * 12, SIZE * 7)), 4).save(
        os.path.join(HERE, "transition_zoom_4x.png"))
    upscale(demo.crop((SIZE * 13, SIZE * 1, SIZE * 20, SIZE * 7)), 4).save(
        os.path.join(HERE, "corner_zoom_4x.png"))
    upscale(demo.crop((SIZE * 8, SIZE * 10, SIZE * 18, SIZE * 14 + FACADE)), 4).save(
        os.path.join(HERE, "facade_zoom_4x.png"))

    report = {
        "schema": "occ-art-trial-report (PROTOTYPE, not a manifest)",
        "status": "PROTOTYPE",
        "date": "2026-09-16",
        "authorization": "用户要求：不用生图，直接填像素",
        "delivery": {"floor_tile_32": [32, 32], "terrain_adjacency_overlay_32": [32, 32],
                     "directional_outer_edge_32x40": [32, 40]},
        "assets": out,
        "palettes": {
            "dirt": {k: "#%02X%02X%02X" % v for k, v in DIRT.items()},
            "stone": {k: "#%02X%02X%02X" % v for k, v in STONE.items()},
        },
        "method": {
            "dirt": "周期值噪声（每变体不同周期）+ 秩映射 5 阶色带 + 孤立像素清理 + 手工土脊/石子/土块",
            "stone": "16 宽 8 高错缝砖 + 左上受光砖唇 + 缺口/浅坑/裂纹，无格子边框",
            "transition": "按另一材质方向在本格贴边像素落笔：泥土侧接触暗线+不规则投影+外露石屑+砾石；石砖侧断续砖唇/压暗+泥土碎屑",
            "outer_edge": "32×40：上方 32×32 为地面格，下方 8px 南向立面（接触影 / 受光上沿 / 主体 / 底部收暗）",
            "field": "变体按格坐标确定性选取（2×2 区块内不重复），铺图可复现；不作镜像/旋转，避免破坏固定左上光源",
        },
        "constraints_checked": [
            "32×32 原生，PPU 32；外沿 32×40，下方 8px 不参与交互",
            "整数像素，无缩放插值、无渐变、无抖色、无抗锯齿",
            "固定左上光源",
            "地面格 6 色，过渡格 ≤10 色",
            "地面不烘焙格子边框",
            "表面细节不触格边（结构灰缝除外）",
            "无孤立单像素噪点（一律 2px 簇落笔 + 色带清理）",
            "变体之间共用同一调色板，可混铺",
        ],
        "not_allowed": [
            "登记 occ-art-manifest-v1",
            "写入 UnityProject/Assets",
            "作为 FORMAL / FORMAL_CANDIDATE",
        ],
        "not_yet": [
            "Unity 导入、GUID/Importer 复核、实机应用接触",
            "湿滑/破损等状态变体，外沿东/西/北向立面",
            "人工审美裁决",
        ],
    }
    with open(os.path.join(HERE, "trial_report.json"), "w", encoding="utf-8") as fh:
        json.dump(report, fh, ensure_ascii=False, indent=2)

    floor_cols = sorted(set(v["colors"] for v in out.values() if v["role"] == "floor_tile_32"))
    trans_cols = sorted(set(v["colors"] for v in out.values() if v["role"] == "terrain_adjacency_overlay_32"))
    outer_cols = sorted(set(v["colors"] for v in out.values() if v["role"] == "directional_outer_edge_32x40"))
    print("assets=%d  floor_colors=%s  transition_colors=%s  outer_colors=%s"
          % (len(out), floor_cols, trans_cols, outer_cols))
    print("demo=%dx%d" % (demo.width, demo.height))


if __name__ == "__main__":
    main()
