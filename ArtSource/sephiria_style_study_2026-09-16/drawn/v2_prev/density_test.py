# -*- coding: utf-8 -*-
"""石砖行距 / 泥土对比度对照试验：按实测的赛道菲莉娅密度挑参数。"""
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SIZE = 32

DIRT = {
    "deep": (104, 78, 48),
    "shade": (126, 96, 56),
    "base": (146, 113, 62),
    "face": (161, 127, 68),
    "light": (176, 145, 85),
    "hi": (190, 160, 100),
    "pebble": (140, 133, 120),
}
STONE = {
    "seam": (58, 58, 66),
    "shade": (74, 74, 84),
    "base": (86, 86, 96),
    "face": (99, 99, 110),
    "light": (116, 116, 127),
    "hi": (134, 138, 144),
}
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


# ---- 石砖：三种行距 ----
def stone(course_pitch, brick_pitch, seed=7):
    tile = [[STONE["face"]] * SIZE for _ in range(SIZE)]
    for y in range(SIZE):
        row = y // course_pitch
        off = (brick_pitch // 2) * (row % 2)
        course = y % course_pitch
        for x in range(SIZE):
            bx = (x - off) % brick_pitch
            if bx == brick_pitch - 1:
                tile[y][x] = STONE["shade"]
                continue
            brick = ((x - off) // brick_pitch) % (SIZE // brick_pitch)
            if course == course_pitch - 1:
                tone = "base"
            else:
                tone = "base" if r01(seed, "b", row, brick) < 0.22 else "face"
            tile[y][x] = STONE[tone]
    return tile


# ---- 泥土：8px 细胞 + 平滑场 + 低对比 ----
def dirt(seed, dist, period, tones):
    n = SIZE // 8
    lat = [[r01(seed, i, j) for j in range(period)] for i in range(period)]
    cells = [[0.0] * n for _ in range(n)]
    sc = period / n
    for cy in range(n):
        for cx in range(n):
            fx, fy = cx * sc, cy * sc
            x0, y0 = int(fx) % period, int(fy) % period
            x1, y1 = (x0 + 1) % period, (y0 + 1) % period
            tx, ty = fx - int(fx), fy - int(fy)
            tx = tx * tx * (3 - 2 * tx)
            ty = ty * ty * (3 - 2 * ty)
            a = lat[y0][x0] + (lat[y0][x1] - lat[y0][x0]) * tx
            b = lat[y1][x0] + (lat[y1][x1] - lat[y1][x0]) * tx
            cells[cy][cx] = a + (b - a) * ty
    flat = sorted((cells[y][x], y, x) for y in range(n) for x in range(n))
    bounds, acc = [], 0.0
    for _t, frac in dist:
        acc += frac
        bounds.append(acc)
    names = [t for t, _f in dist]
    grid = [[""] * n for _ in range(n)]
    for rank, (_v, y, x) in enumerate(flat):
        p = (rank + 0.5) / (n * n)
        i = 0
        while i < len(bounds) - 1 and p > bounds[i]:
            i += 1
        grid[y][x] = names[i]
    tile = [[DIRT["face"]] * SIZE for _ in range(SIZE)]
    for cy in range(n):
        for cx in range(n):
            for dy in range(8):
                for dx in range(8):
                    tile[cy * 8 + dy][cx * 8 + dx] = DIRT[grid[cy][cx]]
    return tile


def img_of(tile):
    im = Image.new("RGB", (SIZE, SIZE))
    im.putdata([tile[y][x] for y in range(SIZE) for x in range(SIZE)])
    return im


def tiles(grid_n, tile):
    im = Image.new("RGB", (SIZE * grid_n, SIZE * grid_n))
    for gy in range(grid_n):
        for gx in range(grid_n):
            im.paste(img_of(tile), (gx * SIZE, gy * SIZE))
    return im


def main():
    dirt_low = {
        "shade": (140, 108, 58),
        "base": (150, 118, 63),
        "face": (161, 127, 68),
        "light": (172, 139, 76),
        "hi": (190, 160, 100),
        "deep": (120, 92, 52),
        "pebble": (140, 133, 120),
    }
    items = [
        ("stone 8x8", tiles(2, stone(8, 8))),
        ("stone 8x16", tiles(2, stone(8, 16))),
        ("dirt smooth p3 c4", tiles(2, dirt(21, [("shade", .22), ("base", .18), ("face", .48), ("light", .12)], 3, DIRT))),
        ("dirt smooth p2 c4", tiles(2, dirt(33, [("shade", .26), ("base", .16), ("face", .44), ("light", .14)], 2, DIRT))),
        ("dirt smooth p3 lowcon", tiles(2, dirt(21, [("shade", .22), ("base", .18), ("face", .48), ("light", .12)], 3, dirt_low))),
    ]
    scale, gap = 3, 10
    cw = SIZE * 2 * scale
    out = Image.new("RGB", (len(items) * cw + (len(items) - 1) * gap, cw), (24, 24, 28))
    for i, (_n, im) in enumerate(items):
        out.paste(im.resize((cw, cw), Image.NEAREST), (i * (cw + gap), 0))
    out.save(os.path.join(HERE, "density_compare2.png"))
    print("saved density_compare2.png", out.size)


if __name__ == "__main__":
    main()
