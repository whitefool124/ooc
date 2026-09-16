# -*- coding: utf-8 -*-
"""按"全面对标赛菲利亚"重做战场美术：原生一格 16px，6 倍显示 = 1920×1080 全屏。

推导：
- 用户截图是 1920×1080 全屏；赛菲莉亚原生画布 320×180，整数放大 6 倍。
  故 1 原生像素 = 6 屏幕像素，一格 16 原生像素 = 96 屏幕像素。
- 角色约 16 原生像素高（= 一格），墙砖 4px（= 角色高的 1/4），结构缝 1px。
- 地图区 1440 宽 = 15 格、1080 高 = 11 格 → 整块 15×11 盘面刚好铺满地图区。

产出：地面格 / 过渡 / 墙（直墙 + 墙角）/ 物件 / 单位剪影占位 + 一张 1920×1080 全屏样板。
状态：候选原料（ArtSource 阶段）。不进 UnityProject，不登记 manifest。
"""
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "b16")
os.makedirs(OUT, exist_ok=True)

CELL = 16          # 原生一格
ZOOM = 6           # 整数放大 = 赛菲莉亚的放大倍率
CANVAS = (1920, 1080)
MAP_W = 1440
BOARD = (15, 11)

MASK = (1 << 64) - 1


def key(k):
    if isinstance(k, str):
        v = 0
        for b in k.encode("utf-8"):
            v = (v * 131 + b) & 0xFFFFFFFF
        return v
    return int(k)


def h(*ks):
    x = 0x9E3779B97F4A7C15
    for k in ks:
        x = (x ^ ((key(k) & MASK) + 0x9E3779B97F4A7C15 + (x << 6) + (x >> 2))) & MASK
        x = (x * 0xBF58476D1CE4E5B9) & MASK
        x ^= x >> 31
        x = (x * 0x94D049BB133111EB) & MASK
        x ^= x >> 29
    return x


def r01(*ks):
    return (h(*ks) >> 11) / float(1 << 53)


# --------------------------------------------------------------------------
# 色板（每材质 5-6 色，取自参考原生取样）
# --------------------------------------------------------------------------
PAL = {
    "dirt": {"seam": (110, 84, 48), "shade": (126, 96, 56), "base": (146, 113, 62),
             "face": (161, 127, 68), "light": (176, 145, 85)},
    "stone": {"seam": (58, 58, 66), "shade": (74, 74, 84), "base": (86, 86, 96),
              "face": (99, 99, 110), "light": (116, 116, 127)},
    "grass": {"seam": (52, 96, 66), "shade": (64, 120, 78), "base": (76, 144, 90),
              "face": (90, 168, 104), "light": (112, 188, 122)},
    "wood": {"seam": (110, 66, 44), "shade": (131, 83, 59), "base": (155, 103, 68),
             "face": (185, 137, 96), "light": (199, 155, 118)},
    "carpet": {"seam": (72, 34, 32), "shade": (86, 37, 35), "base": (99, 40, 38),
               "face": (108, 42, 40), "light": (126, 54, 50)},
}
OUTLINE = (20, 18, 24)


def blank(w=CELL, hgt=CELL, rgba=False):
    return Image.new("RGBA" if rgba else "RGB", (w, hgt), (0, 0, 0, 0) if rgba else (0, 0, 0))


# --------------------------------------------------------------------------
# 地面格（16×16，缝 1px，砖距 4px —— 与参考原生同数）
# --------------------------------------------------------------------------
def tile_stone(seed=7):
    """石地面：参考里地牢地面几乎平色（整块只有 11 色），砖层只出现在墙上。"""
    p = PAL["stone"]
    im = Image.new("RGB", (CELL, CELL), p["face"])
    px = im.load()
    for x in range(CELL):
        px[x, 7] = p["base"]
    for y in range(CELL):
        px[7, y] = p["base"]
    if r01(seed, "slab") < 0.5:
        for x in range(0, 7):
            for y in range(0, 7):
                if r01(seed, "s2", x, y) < 0.12:
                    px[x, y] = p["base"]
    return im


def _field16(seed, period, size=CELL):
    lat = [[r01(seed, i, j) for j in range(period)] for i in range(period)]
    out = [[0.0] * size for _ in range(size)]
    sc = float(period) / size
    for y in range(size):
        fy = y * sc
        y0, y1 = int(fy) % period, (int(fy) + 1) % period
        ty = fy - int(fy)
        ty = ty * ty * (3 - 2 * ty)
        for x in range(size):
            fx = x * sc
            x0, x1 = int(fx) % period, (int(fx) + 1) % period
            tx = fx - int(fx)
            tx = tx * tx * (3 - 2 * tx)
            a = lat[y0][x0] + (lat[y0][x1] - lat[y0][x0]) * tx
            b = lat[y1][x0] + (lat[y1][x1] - lat[y1][x0]) * tx
            out[y][x] = a + (b - a) * ty
    return out


def tile_dirt(seed=21):
    """土面：平滑场秩映射成 2-4px 团块（参考土块 2-3px）。"""
    p = PAL["dirt"]
    f1 = _field16(seed, 3)
    f2 = _field16(seed + 5, 5)
    val = [[0.6 * f1[y][x] + 0.4 * f2[y][x] for x in range(CELL)] for y in range(CELL)]
    flat = sorted((val[y][x], y, x) for y in range(CELL) for x in range(CELL))
    bounds, acc = [], 0.0
    dist = [("shade", 0.20), ("base", 0.20), ("face", 0.48), ("light", 0.12)]
    for _t, f in dist:
        acc += f
        bounds.append(acc)
    tones = [t for t, _f in dist]
    grid = [[""] * CELL for _ in range(CELL)]
    for rank, (_v, y, x) in enumerate(flat):
        q = (rank + 0.5) / (CELL * CELL)
        i = 0
        while i < len(bounds) - 1 and q > bounds[i]:
            i += 1
        grid[y][x] = tones[i]
    im = Image.new("RGB", (CELL, CELL))
    px = im.load()
    for y in range(CELL):
        for x in range(CELL):
            px[x, y] = p[grid[y][x]]
    # 上左受光：暗块北侧 1px 提亮
    for y in range(CELL):
        for x in range(CELL):
            if grid[y][x] == "shade" and grid[(y - 1) % CELL][x] in ("base", "face"):
                px[x, (y - 1) % CELL] = p["light"]
    return im


def tile_grass():
    return Image.new("RGB", (CELL, CELL), PAL["grass"]["face"])


def tile_wood(seed=31):
    p = PAL["wood"]
    im = Image.new("RGB", (CELL, CELL), p["face"])
    px = im.load()
    for y in range(CELL):
        if y % 4 == 3:
            for x in range(CELL):
                px[x, y] = p["shade"]
        elif y % 4 == 0:
            for x in range(CELL):
                px[x, y] = p["light"]
        else:
            for x in range(CELL):
                if (x + (2 if (y // 4) % 2 else 0)) % 8 == 7:
                    px[x, y] = p["shade"]
    return im


def tile_carpet():
    p = PAL["carpet"]
    im = Image.new("RGB", (CELL, CELL), p["face"])
    px = im.load()
    px[3, 3] = p["base"]
    px[11, 9] = p["shade"]
    return im


TILES = {"d": tile_dirt(), "s": tile_stone(), "g": tile_grass(), "w": tile_wood(), "c": tile_carpet()}
VARIANTS = {"d": [tile_dirt(21), tile_dirt(43), tile_dirt(65)], "s": [tile_stone(7), tile_stone(19)]}


def tile_for(mat, gx, gy):
    if mat in VARIANTS:
        v = VARIANTS[mat]
        return v[(gx + 2 * gy + h("%sblk" % mat, gx // 2, gy // 2)) % len(v)]
    return TILES[mat]


DIRS = {"n": (0, -1), "e": (1, 0), "s": (0, 1), "w": (-1, 0)}


def _edge(d, i, k):
    if d == "n":
        return i, k
    if d == "s":
        return i, CELL - 1 - k
    if d == "w":
        return k, i
    return CELL - 1 - k, i


OLD_STAMP = False


def stamp(px, ox, oy, mat, other_mat, d, seed):
    """材质交界：3-6px 长阶梯 + 1px 接触暗线 + 阶梯上沿 1px 受光。

    打磨点：段长从 2-4px 提到 3-6px（大段台阶更像参考）；咬入量以 1-2px 为主、
    少数 3px（贴边碎石块）；阶梯顶面给 1px 对方材质的受光色，读作高度差。
    """
    own, other = PAL[mat], PAL[other_mat]
    x = 0
    while x < CELL:
        if OLD_STAMP:
            run = 2 + int(r01(seed, "run", d, x) * 3)
            depth = int(r01(seed, "dep", d, x) * 3)
        else:
            run = 3 + int(r01(seed, "run", d, x) * 4)
            depth = int(r01(seed, "dep", d, x) * 2) + (1 if r01(seed, "chip", d, x) < 0.35 else 0)
        for i in range(x, min(x + run, CELL)):
            for k in range(depth):
                cx, cy = _edge(d, i, k)
                px[ox + cx, oy + cy] = other["light"] if (k == 0 and not OLD_STAMP) else other["face"]
            cx, cy = _edge(d, i, depth)
            px[ox + cx, oy + cy] = own["seam"]
        if not OLD_STAMP and r01(seed, "blob", d, x) < 0.22 and depth + 1 < 5:
            for dx in (0, 1):
                cx, cy = _edge(d, x + dx, depth + 1)
                if 0 <= cx < CELL and 0 <= cy < CELL:
                    px[ox + cx, oy + cy] = other["face"]
        x += run


NEI = (("n", 0, -1), ("e", 1, 0), ("s", 0, 1), ("w", -1, 0))
NAME = {"d": "dirt", "s": "stone", "g": "grass", "w": "wood", "c": "carpet"}


def render_board(grid, seed=5):
    gh, gw = len(grid), len(grid[0])
    im = Image.new("RGB", (gw * CELL, gh * CELL))
    px = im.load()
    for gy in range(gh):
        for gx in range(gw):
            t = tile_for(grid[gy][gx], gx, gy)
            tp = t.load()
            for y in range(CELL):
                for x in range(CELL):
                    px[gx * CELL + x, gy * CELL + y] = tp[x, y]
    for gy in range(gh):
        for gx in range(gw):
            for d, dx, dy in NEI:
                nx, ny = gx + dx, gy + dy
                if 0 <= nx < gw and 0 <= ny < gh and grid[ny][nx] != grid[gy][gx]:
                    stamp(px, gx * CELL, gy * CELL, NAME[grid[gy][gx]], NAME[grid[ny][nx]], d, seed)
    return im


# --------------------------------------------------------------------------
# 墙体（16px 格：顶面 5px + 前沿 11px，砖距 4px、层高 4px）
# --------------------------------------------------------------------------
def wall_cell(px, ox, oy, west_open, east_open):
    """16px 墙格：顶面 4px（受光）+ 前沿 12px（砖层 4px、竖缝每 8px 一道）。"""
    p = PAL["stone"]
    for y in range(CELL):
        for x in range(CELL):
            if y < 4:
                tone = "light" if y == 0 else ("seam" if y == 3 else "face")
            else:
                rel = y - 4
                if rel == 0 or y >= CELL - 1:
                    tone = "seam"
                elif x % 8 == 7:
                    tone = "shade"
                elif rel % 4 == 0:
                    tone = "light"
                elif rel % 4 == 3:
                    tone = "base"
                else:
                    tone = "face"
            px[ox + x, oy + y] = p[tone]
    if west_open:
        for y in range(4, CELL):
            px[ox, oy + y] = p["light"]
    if east_open and not west_open:
        for y in range(4, CELL):
            px[ox + CELL - 1, oy + y] = p["seam"]


def outline(im):
    src = im.copy()
    a = src.load()
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            if a[x, y][3] == 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if nx < 0 or ny < 0 or nx >= im.width or ny >= im.height or a[nx, ny][3] == 0:
                    px[x, y] = OUTLINE + (255,)
                    break
    return im


def wall(cells, gw, gh):
    im = Image.new("RGBA", (gw * CELL, gh * CELL), (0, 0, 0, 0))
    px = im.load()
    cs = set(cells)
    for gx, gy in sorted(cs):
        wall_cell(px, gx * CELL, gy * CELL, (gx - 1, gy) not in cs, (gx + 1, gy) not in cs)
    return outline(im)


# --------------------------------------------------------------------------
# 物件与单位占位
# --------------------------------------------------------------------------
def crate():
    im = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    px = im.load()
    w = PAL["wood"]
    for x in range(2, 14):
        for y in range(4, 6):                    # 顶面
            px[x, y] = w["light"] + (255,)
    for x in range(1, 15):
        for y in range(6, 15):                   # 前沿
            px[x, y] = w["face"] + (255,)
        for y in (8, 9, 12, 13):                 # 铁带
            for x2 in range(1, 15):
                px[x2, y] = (128, 126, 136, 255) if y in (8, 12) else (78, 76, 86, 255)
    for x in range(1, 15):
        px[x, 14] = w["shade"] + (255,)
    return outline(im)


def rubble():
    im = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    px = im.load()
    p = PAL["stone"]
    for x0, y0, wd, ht in ((3, 6, 5, 5), (8, 8, 5, 4), (6, 10, 4, 4)):
        for x in range(x0, x0 + wd):
            for y in range(y0, y0 + ht):
                tone = "light" if y < y0 + 2 else ("base" if y >= y0 + ht - 1 else "face")
                px[x, y] = p[tone] + (255,)
    return outline(im)


def unit_placeholder():
    """单位剪影占位（16×20 ≈ 一格高，与参考角色同高），只用于核对比例。"""
    w, hgt = 16, 20
    im = Image.new("RGBA", (w, hgt), (0, 0, 0, 0))
    px = im.load()
    body = (74, 82, 104, 255)
    for x in range(5, 11):
        for y in range(0, 6):        # 头
            px[x, y] = body
    for x in range(4, 12):
        for y in range(6, 15):       # 躯干
            px[x, y] = body
    for x in range(4, 7):
        for y in range(15, 19):      # 腿
            px[x, y] = body
    for x in range(9, 12):
        for y in range(15, 19):
            px[x, y] = body
    return outline(im)


# --------------------------------------------------------------------------
# 全屏样板
# --------------------------------------------------------------------------
def grid_board():
    gw, gh = BOARD
    g = [["g"] * gw for _ in range(gh)]
    for y in range(3, 10):
        for x in range(1, 6):
            g[y][x] = "d"
    g[3][5] = "g"
    g[9][4] = "g"
    for y in range(2, 7):
        for x in range(7, 13):
            g[y][x] = "s"
    for i in range(3):
        for y in range(5 - i, 7):
            if 13 + i < gw:
                g[y][13 + i] = "s"
    g[4][9] = "d"
    for y in range(8, 11):
        for x in range(11, 14):
            g[y][x] = "w"
    for x in range(8, 11):
        g[8][x] = "c"
        g[9][x] = "c"
    return ["".join(r) for r in g]


def main():
    grid = grid_board()
    board = render_board(grid)
    big = board.resize((board.width * ZOOM, board.height * ZOOM), Image.NEAREST).convert("RGBA")

    # 草簇（3-5px 成团，独立装饰物层）
    TUFTS = [[(5, 4), (6, 4), (5, 5), (6, 5), (7, 5), (5, 6), (6, 6)],
             [(9, 8), (10, 8), (9, 9), (10, 9), (11, 9)],
             [(4, 10), (5, 10), (4, 11), (5, 11), (6, 11), (5, 12)]]
    for gy in range(len(grid)):
        for gx in range(len(grid[0])):
            if grid[gy][gx] != "g" or r01("clump", gx // 3, gy // 3) > 0.5 or r01("t2", gx, gy) > 0.55:
                continue
            p = PAL["grass"]
            mask = TUFTS[h("tuftv", gx, gy) % len(TUFTS)]
            for x, y in mask:
                tone = "shade" if (x + y) % 3 else "base"
                if y == min(pt[1] for pt in mask):
                    tone = "light"
                X = gx * CELL * ZOOM + x * ZOOM
                Y = gy * CELL * ZOOM + y * ZOOM
                big.paste(p[tone] + (255,), (X, Y, X + ZOOM, Y + ZOOM))

    # 墙：直墙 + 墙角，落在石台地上
    for cells, gx0, gy0 in ((((0, 0), (1, 0), (2, 0)), 7, 2), (((0, 0), (1, 0), (1, 1)), 11, 4)):
        w = wall(cells, 3, 2)
        big.alpha_composite(w.resize((w.width * ZOOM, w.height * ZOOM), Image.NEAREST),
                            (gx0 * CELL * ZOOM, gy0 * CELL * ZOOM))

    # 物件
    for im, gx, gy in ((crate(), 9, 6), (crate(), 8, 3), (rubble(), 5, 8)):
        big.alpha_composite(im.resize((CELL * ZOOM, CELL * ZOOM), Image.NEAREST),
                            (gx * CELL * ZOOM, gy * CELL * ZOOM))

    canvas = Image.new("RGB", CANVAS, (32, 32, 40))
    canvas.paste(big.convert("RGB"), (0, (CANVAS[1] - big.height) // 2))
    canvas.save(os.path.join(OUT, "sample_1920x1080_6x.png"))

    # 单位占位（16×24）+ 落地对照
    u = unit_placeholder()
    unit_canvas = Image.new("RGB", CANVAS, (32, 32, 40))
    unit_canvas.paste(big.convert("RGB"), (0, (CANVAS[1] - big.height) // 2))
    unit_canvas.paste(u.resize((u.width * ZOOM, u.height * ZOOM), Image.NEAREST),
                      (9 * CELL * ZOOM, (CANVAS[1] - big.height) // 2 + 6 * CELL * ZOOM + (CELL - u.height) * ZOOM))
    unit_canvas.save(os.path.join(OUT, "sample_with_unit_1920x1080_6x.png"))

    # 素材图鉴（统一格位，1:1 与 8 倍）
    items = [("dirt", TILES["d"]), ("stone", TILES["s"]), ("grass", TILES["g"]),
             ("wood", TILES["w"]), ("carpet", TILES["c"]),
             ("wall", wall(((0, 0), (1, 0), (1, 1)), 2, 2)), ("crate", crate()), ("rubble", rubble())]
    scale, gap, box = 8, 12, 32 * 8
    w = box * len(items) + gap * (len(items) - 1)
    sheet = Image.new("RGB", (w, box), (24, 24, 28))
    for i, (_n, im) in enumerate(items):
        big_item = im.convert("RGBA").resize((im.width * scale, im.height * scale), Image.NEAREST)
        sheet.paste(big_item, (i * (box + gap), box - big_item.height), big_item)
    sheet.save(os.path.join(OUT, "contact_16px_8x.png"))
    print("board native", board.size, "-> display", big.size)
    print("saved sample_1920x1080_6x.png / sample_with_unit_1920x1080_6x.png / contact_16px_8x.png")


if __name__ == "__main__":
    main()
