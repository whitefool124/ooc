# -*- coding: utf-8 -*-
"""像素尺度对照：按"同一屏幕面积"比较，而不是按"每格像素数"。

赛菲莉娅 1 原生像素 = 6 屏幕像素；OCC 默认 2 倍显示 = 2 屏幕像素。
故 192 屏幕像素见方：赛菲莉娅 = 32 原生像素（2 个地面格），OCC = 96 原生像素（3×3 逻辑格）。
本脚本把两者放在同一屏幕尺寸下并排，用于挑特征尺寸。
参考裁切仅比对用，不进 Unity。
"""
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
STUDY = os.path.dirname(HERE)
REFS = os.path.join(STUDY, "refs")
SIZE = 32
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


def field(seed, period, size=SIZE):
    lat = [[r01(seed, i, j) for j in range(period)] for i in range(period)]
    out = [[0.0] * size for _ in range(size)]
    sc = float(period) / size
    for y in range(size):
        fy = y * sc
        y0 = int(fy) % period
        y1 = (y0 + 1) % period
        ty = fy - int(fy)
        ty = ty * ty * (3 - 2 * ty)
        for x in range(size):
            fx = x * sc
            x0 = int(fx) % period
            x1 = (x0 + 1) % period
            tx = fx - int(fx)
            tx = tx * tx * (3 - 2 * tx)
            a = lat[y0][x0] + (lat[y0][x1] - lat[y0][x0]) * tx
            b = lat[y1][x0] + (lat[y1][x1] - lat[y1][x0]) * tx
            out[y][x] = a + (b - a) * ty
    return out


STONE = {"seam": (58, 58, 66), "shade": (74, 74, 84), "base": (86, 86, 96),
         "face": (99, 99, 110), "light": (116, 116, 127)}
DIRT = {"seam": (110, 84, 48), "shade": (126, 96, 56), "base": (146, 113, 62),
        "face": (161, 127, 68), "light": (176, 145, 85)}
GRASS = {"seam": (52, 96, 66), "shade": (64, 120, 78), "base": (76, 144, 90),
         "face": (90, 168, 104), "light": (112, 188, 122)}


def stone_tile(pitch, seam_px, seed=7):
    pal = STONE
    tile = [[pal["face"]] * SIZE for _ in range(SIZE)]
    for y in range(SIZE):
        row = y // pitch
        off = (pitch // 2) * (row % 2)
        course = y % pitch
        for x in range(SIZE):
            bx = (x - off) % pitch
            if bx >= pitch - seam_px:
                tile[y][x] = pal["shade"]
                continue
            brick = ((x - off) // pitch) % (SIZE // pitch)
            if course >= pitch - seam_px:
                tone = "base"
            elif course == 0 and r01(seed, "lit", row, brick) < 0.35:
                tone = "light"
            else:
                tone = "base" if r01(seed, "b", row, brick) < 0.2 else "face"
            tile[y][x] = pal[tone]
    return tile


def mottle(pal, seed, periods, dist):
    f1 = field(seed, periods[0])
    f2 = field(seed + 3, periods[1])
    val = [[0.6 * f1[y][x] + 0.4 * f2[y][x] for x in range(SIZE)] for y in range(SIZE)]
    flat = sorted((val[y][x], y, x) for y in range(SIZE) for x in range(SIZE))
    bounds, acc = [], 0.0
    for _t, f in dist:
        acc += f
        bounds.append(acc)
    tones = [t for t, _f in dist]
    grid = [[""] * SIZE for _ in range(SIZE)]
    for rank, (_v, y, x) in enumerate(flat):
        p = (rank + 0.5) / (SIZE * SIZE)
        i = 0
        while i < len(bounds) - 1 and p > bounds[i]:
            i += 1
        grid[y][x] = tones[i]
    for _ in range(2):
        new = [row[:] for row in grid]
        for y in range(SIZE):
            for x in range(SIZE):
                counts, same = {}, 0
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        if dx == 0 and dy == 0:
                            continue
                        t = grid[(y + dy) % SIZE][(x + dx) % SIZE]
                        counts[t] = counts.get(t, 0) + 1
                        if t == grid[y][x]:
                            same += 1
                if same <= 1:
                    new[y][x] = max(counts.items(), key=lambda kv: (kv[1], kv[0]))[0]
        grid = new
    tile = [[pal[grid[y][x]] for x in range(SIZE)] for y in range(SIZE)]
    for y in range(SIZE):
        for x in range(SIZE):
            if grid[y][x] == "shade" and grid[(y - 1) % SIZE][x] in ("base", "face"):
                tile[(y - 1) % SIZE][x] = pal["light"]
    return tile


def grass_tile(seed, periods, dist):
    return mottle(GRASS, seed, periods, dist)


def field_of(tile, n=3):
    im = Image.new("RGB", (SIZE * n, SIZE * n))
    for gy in range(n):
        for gx in range(n):
            for y in range(SIZE):
                for x in range(SIZE):
                    im.putpixel((gx * SIZE + x, gy * SIZE + y), tile[y][x])
    return im


def ref_patch(name, box):
    with Image.open(os.path.join(REFS, name)) as im:
        return im.convert("RGB").crop(box)


def row(items, gap=10, bg=(20, 20, 24)):
    w = sum(im.width for _n, im in items) + gap * (len(items) - 1)
    hh = max(im.height for _n, im in items)
    out = Image.new("RGB", (w, hh), bg)
    x = 0
    for _n, im in items:
        out.paste(im, (x, 0))
        x += im.width + gap
    return out


def main():
    # 同一屏幕面积 192x192：参考 1:1；我方 3×3 格在 2 倍显示下 = 96*2
    ref_stone = ref_patch("dungeon_stone_wall.png", (0, 0, 192, 192))
    ref_grass = ref_patch("field_e.png", (0, 24, 192, 216))

    levels = [
        ("A 砖8/缝1 土5,9", stone_tile(8, 1), mottle(DIRT, 21, (5, 9), [("shade", .21), ("base", .17), ("face", .49), ("light", .13)]), mottle(GRASS, 31, (6, 10), [("shade", .09), ("base", .13), ("face", .72), ("light", .06)])),
        ("B 砖16/缝2 土3,5", stone_tile(16, 2), mottle(DIRT, 41, (3, 5), [("shade", .21), ("base", .17), ("face", .49), ("light", .13)]), mottle(GRASS, 51, (3, 5), [("shade", .10), ("base", .13), ("face", .71), ("light", .06)])),
        ("C 砖16/缝3 土2,4", stone_tile(16, 3), mottle(DIRT, 61, (2, 4), [("shade", .22), ("base", .17), ("face", .48), ("light", .13)]), mottle(GRASS, 71, (2, 4), [("shade", .11), ("base", .13), ("face", .70), ("light", .06)])),
        ("D 砖16/缝3 土4,7", stone_tile(16, 3), mottle(DIRT, 81, (4, 7), [("shade", .21), ("base", .17), ("face", .49), ("light", .13)]), mottle(GRASS, 91, (5, 8), [("shade", .10), ("base", .13), ("face", .71), ("light", .06)])),
    ]
    rows = [row([("ref", ref_stone)] + [(n, field_of(s).resize((192, 192), Image.NEAREST)) for n, s, _d, _g in levels])]
    rows.append(row([("ref", ref_grass)] + [(n, field_of(d).resize((192, 192), Image.NEAREST)) for n, _s, d, _g in levels]))
    rows.append(row([("ref", ref_grass)] + [(n, field_of(g).resize((192, 192), Image.NEAREST)) for n, _s, _d, g in levels]))

    def zoom4(tile):
        big = field_of(tile).resize((SIZE * 3 * 4, SIZE * 3 * 4), Image.NEAREST)
        return big.crop((0, 0, 192, 192))

    rows.append(row([("ref", ref_stone)] + [(n, zoom4(s)) for n, s, _d, _g in levels]))
    w = max(r.width for r in rows)
    hh = sum(r.height for r in rows) + 12 * (len(rows) - 1)
    out = Image.new("RGB", (w, hh), (20, 20, 24))
    y = 0
    for r in rows:
        out.paste(r, (0, y))
        y += r.height + 12
    out.save(os.path.join(HERE, "pixel_scale_probe.png"))
    print("saved pixel_scale_probe.png", out.size, "(左起：赛菲莉娅 1:1，A/B/C 我方 2× 显示)")


if __name__ == "__main__":
    main()
