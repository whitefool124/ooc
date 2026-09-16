# -*- coding: utf-8 -*-
"""按赛菲莉娅语言重画 OCC 32px 地面族（泥土 / 石砖 / 过渡 / 外沿立面）。

依据 = ArtSource/sephiria_style_study_2026-09-16 的原生网格测量：
- 原生步长 6 屏幕像素；赛道菲莉娅地面格约 16 原生像素，OCC 地面格 32 原生像素，
  因此特征尺寸按 2 倍换算：砖 4×2 → 8×4，缝隙 1px，簇 2-3px → 4-6px。
- 主色调占材质面积 55-65%；其余由 1px 结构缝与离散簇分掉。
- 无逐像素噪点、无渐变、无抖动、无抗锯齿；只有硬边。
- 材质交界是互锁的阶梯边 + 1px 暗缝，不画"碎石投影"这类臆造细节。

状态：候选原料（ArtSource 阶段）。不进 UnityProject，不登记 manifest。
"""
import json
import os

from PIL import Image

SIZE = 32
FACADE = 8
HERE = os.path.dirname(os.path.abspath(__file__))

# --------------------------------------------------------------------------
# 调色板：取自截图原生取样后按 OCC 明度整理（同一材质 6 色，步距 8-14 明度）
# --------------------------------------------------------------------------
DIRT = {
    "deep": (120, 92, 52),
    "shade": (126, 96, 56),    # 与主色差 ~31 明度，和参考实测的材质内步距一致
    "base": (146, 113, 62),
    "face": (161, 127, 68),    # A17F44 主色
    "light": (176, 145, 85),
    "hi": (190, 160, 100),
    "pebble": (140, 133, 120),
}
STONE = {
    "seam": (58, 58, 66),      # 3A3A42  结构缝
    "shade": (74, 74, 84),     # 4A4A54
    "base": (86, 86, 96),      # 565660  主色
    "face": (99, 99, 110),     # 63636E
    "light": (116, 116, 127),  # 74747F  砖顶受光
    "hi": (134, 138, 144),     # 868A90  极少高光
}

MASK = (1 << 64) - 1


def key(k):
    if isinstance(k, str):
        v = 0
        for b in k.encode("utf-8"):
            v = (v * 131 + b) & 0xFFFFFFFF
        return v
    return int(k)


def h(*keys):
    x = 0x9E3779B97F4A7C15
    for k in keys:
        x = (x ^ ((key(k) & MASK) + 0x9E3779B97F4A7C15 + (x << 6) + (x >> 2))) & MASK
        x = (x * 0xBF58476D1CE4E5B9) & MASK
        x ^= x >> 31
        x = (x * 0x94D049BB133111EB) & MASK
        x ^= x >> 29
    return x


def r01(*keys):
    return (h(*keys) >> 11) / float(1 << 53)


# --------------------------------------------------------------------------
# 手工簇形状（赛菲莉娅的土块是方圆形小块，不是逐像素噪点）
# --------------------------------------------------------------------------
CLUSTERS = {
    "c2x2": [".XX.", "XXXX", "XXXX", ".XX."],
    "c3x2": [".XX.", "XXXX", "XXXX"],
    "c3x3": [".XX.", "XXXX", "XXXX", ".XX."],
    "c4x3": ["..XX..", ".XXXX.", "XXXXXX", ".XXXX."],
    "c5x4": ["..XXX..", ".XXXXX.", "XXXXXXX", "XXXXXXX", ".XXXXX."],
    "c2x1": ["XX", "XX"],
    "sq3": ["XXX", "XXX", "XXX"],
    "sq2": ["XX", "XX"],
}


def stamp(mask, x, y, put):
    for dy, row in enumerate(CLUSTERS[mask]):
        for dx, ch in enumerate(row):
            if ch == "X":
                put(x + dx, y + dy)


# --------------------------------------------------------------------------
# 泥土：主色铺满，离散深块/浅块/洼地，细节留边
# --------------------------------------------------------------------------
DIRT_VARIANTS = {
    # 4px 细胞 + 周期 3 的平滑场：斑块连成 8-24px 的土色块，避免方格感
    "a": dict(seed=21, period=5, dist=[("shade", 0.21), ("base", 0.17), ("face", 0.49), ("light", 0.13)],
              pebble=[(11, 21)]),
    "b": dict(seed=33, period=5, dist=[("shade", 0.19), ("base", 0.18), ("face", 0.50), ("light", 0.13)],
              pebble=[]),
    "c": dict(seed=55, period=4, dist=[("shade", 0.22), ("base", 0.16), ("face", 0.49), ("light", 0.13)],
              pebble=[]),
    # 对照用：OCC 合同 floor_tile_32 的 sparse_low_contrast_non_connecting_marks_only 口径
    "sparse": dict(seed=77, period=3,
                   dist=[("shade", 0.10), ("base", 0.12), ("face", 0.72), ("light", 0.06)],
                   pebble=[(9, 23)]),
}

CELL = 4


def build_dirt_tile(v):
    """逐像素平滑场 + 秩映射四阶 + 孤立像素清理：得到无台阶感的土面斑驳。

    参考里土面斑块 2-3px（换算到 OCC 4-6px），所以用两段周期噪声而不是 4px 方格。
    """
    n = SIZE
    f1 = _field(v["seed"], v["period"])
    f2 = _field(v["seed"] + 3, v["period"] + 4)
    field = [[0.60 * f1[y][x] + 0.40 * f2[y][x] for x in range(n)] for y in range(n)]

    flat = sorted((field[y][x], y, x) for y in range(n) for x in range(n))
    bounds, acc = [], 0.0
    for _tone, frac in v["dist"]:
        acc += frac
        bounds.append(acc)
    tones = [t for t, _f in v["dist"]]
    grid = [[""] * n for _ in range(n)]
    for rank, (_val, y, x) in enumerate(flat):
        p = (rank + 0.5) / (n * n)
        i = 0
        while i < len(bounds) - 1 and p > bounds[i]:
            i += 1
        grid[y][x] = tones[i]

    # 去孤立单像素（合同禁止），并把 1px 凹口抹平，避免噪点感
    for _ in range(2):
        new = [row[:] for row in grid]
        for y in range(n):
            for x in range(n):
                counts = {}
                same = 0
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        if dx == 0 and dy == 0:
                            continue
                        t = grid[(y + dy) % n][(x + dx) % n]
                        counts[t] = counts.get(t, 0) + 1
                        if t == grid[y][x]:
                            same += 1
                if same <= 1:
                    new[y][x] = max(counts.items(), key=lambda kv: (kv[1], kv[0]))[0]
        grid = new

    tile = [[DIRT[grid[y][x]] for x in range(n)] for y in range(n)]

    def put(x, y, tone):
        if 0 <= x < SIZE and 0 <= y < SIZE:
            tile[y][x] = DIRT[tone]

    # 受光土块：沿暗块的北侧 1px 提亮，制造上左受光
    for y in range(n):
        for x in range(n):
            if grid[y][x] == "shade" and grid[(y - 1) % n][x] in ("base", "face"):
                put(x, (y - 1) % n, "light")
    for x, y in v["pebble"]:
        for dx in (0, 1):
            for dy in (0, 1):
                put(x + dx, y + dy, "pebble")
        put(x, y, "hi")
        put(x + 1, y + 1, "shade")
    return tile


def _field(seed, period):
    """周期性平滑值场（32px 内无缝）。"""
    lat = [[r01(seed, i, j) for j in range(period)] for i in range(period)]
    out = [[0.0] * SIZE for _ in range(SIZE)]
    scale = float(period) / SIZE
    for y in range(SIZE):
        fy = y * scale
        y0 = int(fy) % period
        y1 = (y0 + 1) % period
        ty = fy - int(fy)
        ty = ty * ty * (3 - 2 * ty)
        for x in range(SIZE):
            fx = x * scale
            x0 = int(fx) % period
            x1 = (x0 + 1) % period
            tx = fx - int(fx)
            tx = tx * tx * (3 - 2 * tx)
            a = lat[y0][x0] + (lat[y0][x1] - lat[y0][x0]) * tx
            b = lat[y1][x0] + (lat[y1][x1] - lat[y1][x0]) * tx
            out[y][x] = a + (b - a) * ty
    return out


# --------------------------------------------------------------------------
# 石砖：连续错缝砖场，砖 8×4（含 1px 缝），砖顶 1px 受光，主色占多数
# --------------------------------------------------------------------------
STONE_VARIANTS = {
    "a": dict(seed=71, chip=0.22, wear=[("sq2", 6, 14), ("sq2", 22, 26)]),
    "b": dict(seed=83, chip=0.36, wear=[("c3x2", 12, 5), ("sq2", 2, 24), ("sq2", 25, 10)]),
}


def build_stone_tile(v):
    """8×8 石砖：层距与砖宽按参考实测 2 倍换算（3-4px → 8px），1px 缝，砖面平色为主。"""
    tile = [[STONE["face"]] * SIZE for _ in range(SIZE)]
    pitch = 8
    for y in range(SIZE):
        row = y // pitch
        off = (pitch // 2) * (row % 2)
        course = y % pitch
        for x in range(SIZE):
            bx = (x - off) % pitch
            if bx == pitch - 1:
                tile[y][x] = STONE["shade"]
                continue
            brick = ((x - off) // pitch) % (SIZE // pitch)
            if course == pitch - 1:
                tone = "base"
            elif course == 0 and r01(v["seed"], "lit", row, brick) < 0.35:
                tone = "light"
            else:
                tone = "base" if r01(v["seed"], "brick", row, brick) < 0.20 else "face"
            tile[y][x] = STONE[tone]
    # 缺口：砖顶右角崩掉 1px
    for y in range(0, SIZE, pitch):
        row = y // pitch
        off = (pitch // 2) * (row % 2)
        for brick in range(SIZE // pitch):
            if r01(v["seed"], "chip", row, brick) < v["chip"]:
                x = (brick * pitch + off + pitch - 2) % SIZE
                tile[y][x] = STONE["shade"]

    def put(x, y, tone):
        if 0 <= x < SIZE and 0 <= y < SIZE:
            tile[y][x] = STONE[tone]

    for maskname, x, y in v["wear"]:
        stamp(maskname, x, y, lambda px, py: put(px, py, "shade"))
    return tile


# --------------------------------------------------------------------------
# 过渡：互锁阶梯边 + 1px 暗缝 + 少量离散簇越界
# --------------------------------------------------------------------------
DIRS = {"n": (0, -1), "e": (1, 0), "s": (0, 1), "w": (-1, 0)}


def _edge(d, i, k):
    if d == "n":
        return i, k
    if d == "s":
        return i, SIZE - 1 - k
    if d == "w":
        return k, i
    return SIZE - 1 - k, i


def stamp_boundary(px, ox, oy, mat, d, seed):
    """本格贴边像素：对侧材质以 2-4px 阶梯咬入，交界压 1px 暗缝。"""
    other = STONE if mat == "d" else DIRT
    own = DIRT if mat == "d" else STONE
    x = 0
    while x < SIZE:
        run = 2 + int(r01(seed, "run", d, x) * 3)  # 2-4px 一段
        depth = int(r01(seed, "depth", d, x) * 4)  # 0-3px 咬入
        for i in range(x, min(x + run, SIZE)):
            for k in range(depth):
                cx, cy = _edge(d, i, k)
                px[ox + cx, oy + cy] = other["face"] if mat == "d" else other["base"]
            cx, cy = _edge(d, i, depth)
            # 交界压本材质自身的暗色，而不是对方的最暗缝，避免出现近黑描边
            px[ox + cx, oy + cy] = own["deep"] if mat == "d" else own["shade"]
        # 越界的离散簇：约 1/4 段在缝外再落 2×2
        if r01(seed, "blob", d, x) < 0.25:
            depth2 = min(depth + 2, 6)
            for dx in (0, 1):
                for dy in (0, 1):
                    cx, cy = _edge(d, x + dx, depth2 + dy)
                    if 0 <= cx < SIZE and 0 <= cy < SIZE:
                        px[ox + cx, oy + cy] = other["face"] if mat == "d" else other["base"]
        x += run


NEIGHBORS = (("n", 0, -1), ("e", 1, 0), ("s", 0, 1), ("w", -1, 0))


def render_board(grid, with_facade=False, seed=333):
    gh, gw = len(grid), len(grid[0])
    height = gh * SIZE + (FACADE if with_facade else 0)
    img = Image.new("RGB", (gw * SIZE, height))
    px = img.load()
    for gy in range(gh):
        for gx in range(gw):
            mat = grid[gy][gx]
            tile = DIRT_TILES[variant_index("d", gx, gy)] if mat == "d" else STONE_TILES[variant_index("s", gx, gy)]
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
                    stamp_boundary(px, ox, oy, mat, d, seed)
    if with_facade:
        for gx in range(gw):
            img.paste(facade_band(grid[gh - 1][gx], gx), (gx * SIZE, gh * SIZE))
    return img


def facade_band(mat, gx=0):
    """南向外立面：1px 转折缝 + 受光上沿 + 主体（竖缝）+ 底部收暗。"""
    im = Image.new("RGB", (SIZE, FACADE))
    px = im.load()
    pal = DIRT if mat == "d" else STONE
    for x in range(SIZE):
        face = pal["shade"] if mat == "d" else pal["base"]
        px[x, 0] = pal["seam"] if mat == "s" else DIRT["shade"]
        px[x, 1] = pal["base"] if mat == "d" else pal["light"]
        for y in (2, 3, 4, 5):
            px[x, y] = face
        if x % 8 == 7:
            for y in (2, 3, 4, 5):
                px[x, y] = pal["seam"] if mat == "s" else DIRT["shade"]
        if mat == "d" and r01("bandclod", gx, x // 2) < 0.3:
            for y in (3, 4):
                px[x, y] = DIRT["shade"]
        px[x, 6] = pal["seam"] if mat == "s" else DIRT["shade"]
        px[x, 7] = pal["seam"] if mat == "s" else DIRT["shade"]
    return im


def variant_index(mat, gx, gy):
    if mat == "d":
        n = len(DIRT_TILES)
        return (gx + 2 * gy + h("dirtblock", gx // 2, gy // 2)) % n
    n = len(STONE_TILES)
    return (gx + gy + h("stoneblock", gx // 2, gy // 2)) % n


DIRT_TILES = [build_dirt_tile(DIRT_VARIANTS[k]) for k in ("a", "b", "c")]
DIRT_KEYS = ["a", "b", "c"]
STONE_TILES = [build_stone_tile(STONE_VARIANTS[k]) for k in ("a", "b")]
STONE_KEYS = ["a", "b"]


def tile_image(t):
    im = Image.new("RGB", (SIZE, SIZE))
    im.putdata([t[y][x] for y in range(SIZE) for x in range(SIZE)])
    return im


def piece(own, dirs):
    other = "s" if own == "d" else "d"
    grid = [[own] * 3 for _ in range(3)]
    for d in dirs:
        dx, dy = DIRS[d]
        grid[1 + dy][1 + dx] = other
    if len(dirs) == 2:
        dx = sum(DIRS[d][0] for d in dirs)
        dy = sum(DIRS[d][1] for d in dirs)
        grid[1 + dy][1 + dx] = other
    board = render_board(["".join(r) for r in grid])
    return board.crop((SIZE, SIZE, SIZE * 2, SIZE * 2))


def outer_piece(mat):
    board = render_board([mat])
    out = Image.new("RGB", (SIZE, SIZE + FACADE))
    out.paste(board.crop((0, 0, SIZE, SIZE)), (0, 0))
    out.paste(facade_band(mat), (0, SIZE))
    return out


def upscale(img, f):
    return img.resize((img.width * f, img.height * f), Image.NEAREST)


def unique_colors(img):
    return len({c for _n, c in img.convert("RGB").getcolors(maxcolors=1 << 20)})


def coverage(tile):
    from collections import Counter
    cnt = Counter(tuple(px) for row in tile for px in row)
    return {("%02X%02X%02X" % c): round(v / (SIZE * SIZE), 3) for c, v in cnt.most_common()}


def sheet(items, cols, scale, gap=8, bg=(24, 24, 28)):
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
    for y in range(2, 7):
        for x in range(4, 15):
            g[y][x] = "s"
    for i in range(4):
        for y in range(5 - i, 7):
            g[y][15 + i] = "s"
    g[4][8] = "d"
    g[5][11] = "d"
    g[10][3] = "s"
    for x in range(14, 22):
        g[12][x] = "s"
        g[13][x] = "s"
    g[11][21] = "s"
    return ["".join(r) for r in g]


def main():
    report = {"palettes": {"dirt": {k: "#%02X%02X%02X" % v for k, v in DIRT.items()},
                           "stone": {k: "#%02X%02X%02X" % v for k, v in STONE.items()}},
              "assets": {}, "coverage": {}}

    for k, t in zip(DIRT_KEYS, DIRT_TILES):
        im = tile_image(t)
        im.save(os.path.join(HERE, "dirt_%s_32.png" % k))
        report["assets"]["dirt_%s_32.png" % k] = {"role": "floor_tile_32", "colors": unique_colors(im)}
        report["coverage"]["dirt_%s" % k] = coverage(t)
    sparse = build_dirt_tile(DIRT_VARIANTS["sparse"])
    sparse_img = tile_image(sparse)
    sparse_img.save(os.path.join(HERE, "dirt_sparse_contract_32.png"))
    report["assets"]["dirt_sparse_contract_32.png"] = {"role": "floor_tile_32", "colors": unique_colors(sparse_img)}
    report["coverage"]["dirt_sparse"] = coverage(sparse)
    for k, t in zip(STONE_KEYS, STONE_TILES):
        im = tile_image(t)
        im.save(os.path.join(HERE, "stone_%s_32.png" % k))
        report["assets"]["stone_%s_32.png" % k] = {"role": "floor_tile_32", "colors": unique_colors(im)}
        report["coverage"]["stone_%s" % k] = coverage(t)

    edges, corners = [], []
    for own, tag in (("d", "dirt_stone"), ("s", "stone_dirt")):
        for d in ("n", "e", "s", "w"):
            im = piece(own, [d])
            name = "%s_%s_32.png" % (tag, d)
            im.save(os.path.join(HERE, name))
            report["assets"][name] = {"role": "terrain_adjacency_overlay_32", "colors": unique_colors(im)}
            edges.append((name, im))
        for d1, d2 in (("n", "e"), ("s", "e"), ("s", "w"), ("n", "w")):
            im = piece(own, [d1, d2])
            name = "%s_%s%s_32.png" % (tag, d1, d2)
            im.save(os.path.join(HERE, name))
            report["assets"][name] = {"role": "terrain_adjacency_overlay_32", "colors": unique_colors(im)}
            corners.append((name, im))

    outer = []
    for mat, tag in (("d", "dirt"), ("s", "stone")):
        im = outer_piece(mat)
        name = "%s_outer_s_32x40.png" % tag
        im.save(os.path.join(HERE, name))
        report["assets"][name] = {"role": "directional_outer_edge_32x40", "colors": unique_colors(im)}
        outer.append((name, im))

    sheet([("dirt_%s" % k, tile_image(t)) for k, t in zip(DIRT_KEYS, DIRT_TILES)] +
          [("stone_%s" % k, tile_image(t)) for k, t in zip(STONE_KEYS, STONE_TILES)],
          5, 6).save(os.path.join(HERE, "contact_floors_6x.png"))
    sheet(edges + corners, 8, 6).save(os.path.join(HERE, "contact_transitions_6x.png"))
    sheet(outer, 2, 6).save(os.path.join(HERE, "contact_outer_6x.png"))

    demo = render_board(build_demo_grid(), with_facade=True)
    demo.save(os.path.join(HERE, "demo_map_1x.png"))
    upscale(demo, 2).save(os.path.join(HERE, "demo_map_2x.png"))
    demo.save(os.path.join(HERE, "demo_map_native.png"))
    upscale(demo.crop((SIZE * 13, SIZE * 1, SIZE * 20, SIZE * 7)), 6).save(
        os.path.join(HERE, "corner_zoom_6x.png"))
    upscale(demo.crop((SIZE * 8, SIZE * 10, SIZE * 18, SIZE * 14 + FACADE)), 6).save(
        os.path.join(HERE, "facade_zoom_6x.png"))

    with open(os.path.join(HERE, "drawn_report.json"), "w", encoding="utf-8") as fh:
        json.dump(report, fh, ensure_ascii=False, indent=1)
    print("assets=%d" % len(report["assets"]))
    for k in ("dirt_a", "stone_a"):
        top = list(report["coverage"][k].items())[:3]
        print(k, "top coverage:", top)


if __name__ == "__main__":
    main()
