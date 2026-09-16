# -*- coding: utf-8 -*-
"""按赛道菲莉娅语言画 OCC 32px 地面族 v3：泥土 / 石砖 / 草 / 木 / 地毯 + 过渡 + 外沿。

规范依据（2026-09-16 用户确认后已同步三处）：
- 总策划案 A.2 / A.2.1：低对比连续团块，同一材质主色约 45-55%，无孤立单像素，无每格边框。
- OCC_美术与界面规格表 ART-BATTLE-32 行。
- Tools/OCCArt/occ_art_contract_v1.json roles.floor_tile_32.surface_detail_policy 与
  primary_map_reference.measured_reference_2026_09_16（原生步长 6 屏幕像素、木层距 4px、
  石层距 3-4px、砖宽 4px；按逻辑格 ×2 换算 -> 层距 8px、缝 1px、团块 4-6px）。

状态：候选原料（ArtSource 阶段）。不进 UnityProject，不登记 manifest。
"""
import json
import os

from PIL import Image

SIZE = 32
FACADE = 8
HERE = os.path.dirname(os.path.abspath(__file__))
MASK = (1 << 64) - 1

# --------------------------------------------------------------------------
# 材质色板：统一 6 键（seam 最暗结构色 / shade 暗 / base / face 主色 / light / hi）
# --------------------------------------------------------------------------
MATERIALS = {
    "d": {"name": "dirt", "pal": {
        "seam": (110, 84, 48), "shade": (126, 96, 56), "base": (146, 113, 62),
        "face": (161, 127, 68), "light": (176, 145, 85), "hi": (190, 160, 100)}},
    "s": {"name": "stone", "pal": {
        "seam": (58, 58, 66), "shade": (74, 74, 84), "base": (86, 86, 96),
        "face": (99, 99, 110), "light": (116, 116, 127), "hi": (134, 138, 144)}},
    "g": {"name": "grass", "pal": {
        "seam": (52, 96, 66), "shade": (64, 120, 78), "base": (76, 144, 90),
        "face": (90, 168, 104), "light": (112, 188, 122), "hi": (136, 204, 142)}},
    "w": {"name": "wood", "pal": {
        "seam": (110, 66, 44), "shade": (131, 83, 59), "base": (155, 103, 68),
        "face": (185, 137, 96), "light": (199, 155, 118), "hi": (215, 178, 140)}},
    "c": {"name": "carpet", "pal": {
        "seam": (72, 34, 32), "shade": (86, 37, 35), "base": (99, 40, 38),
        "face": (108, 42, 40), "light": (126, 54, 50), "hi": (146, 72, 64)}},
}


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


def field(seed, period):
    """周期性平滑值场（32px 内无缝）。"""
    lat = [[r01(seed, i, j) for j in range(period)] for i in range(period)]
    out = [[0.0] * SIZE for _ in range(SIZE)]
    sc = float(period) / SIZE
    for y in range(SIZE):
        fy = y * sc
        y0 = int(fy) % period
        y1 = (y0 + 1) % period
        ty = fy - int(fy)
        ty = ty * ty * (3 - 2 * ty)
        for x in range(SIZE):
            fx = x * sc
            x0 = int(fx) % period
            x1 = (x0 + 1) % period
            tx = fx - int(fx)
            tx = tx * tx * (3 - 2 * tx)
            a = lat[y0][x0] + (lat[y0][x1] - lat[y0][x0]) * tx
            b = lat[y1][x0] + (lat[y1][x1] - lat[y1][x0]) * tx
            out[y][x] = a + (b - a) * ty
    return out


def mottled(pal, seed, periods, dist, keep_single=False):
    """平滑场 -> 秩映射 -> 去孤立像素；返回 tile。"""
    f1 = field(seed, periods[0])
    f2 = field(seed + 3, periods[1])
    val = [[0.60 * f1[y][x] + 0.40 * f2[y][x] for x in range(SIZE)] for y in range(SIZE)]
    flat = sorted((val[y][x], y, x) for y in range(SIZE) for x in range(SIZE))
    bounds, acc = [], 0.0
    for _t, frac in dist:
        acc += frac
        bounds.append(acc)
    tones = [t for t, _f in dist]
    grid = [[""] * SIZE for _ in range(SIZE)]
    for rank, (_v, y, x) in enumerate(flat):
        p = (rank + 0.5) / (SIZE * SIZE)
        i = 0
        while i < len(bounds) - 1 and p > bounds[i]:
            i += 1
        grid[y][x] = tones[i]
    if not keep_single:
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
    # 上左受光：暗块北侧 1px 提亮
    for y in range(SIZE):
        for x in range(SIZE):
            if grid[y][x] == "shade" and grid[(y - 1) % SIZE][x] in ("base", "face"):
                tile[(y - 1) % SIZE][x] = pal["light"]
    return tile


# ---- 地面格 -----------------------------------------------------------------
def build_dirt(seed, variant=0):
    dists = [[("shade", 0.20), ("base", 0.17), ("face", 0.50), ("light", 0.13)],
             [("shade", 0.18), ("base", 0.18), ("face", 0.51), ("light", 0.13)],
             [("shade", 0.22), ("base", 0.16), ("face", 0.49), ("light", 0.13)],
             [("shade", 0.19), ("base", 0.19), ("face", 0.49), ("light", 0.13)],
             [("shade", 0.21), ("base", 0.15), ("face", 0.51), ("light", 0.13)],
             [("shade", 0.17), ("base", 0.20), ("face", 0.50), ("light", 0.13)]]
    periods = DIRT_PERIODS[variant % 6]
    # 地面格 palette_max 6：不使用额外的石子灰，避免第 7 色
    # 有机斑块不随结构类放大：参考土块仅 2-3 原生像素（12-18 屏幕像素），
    # OCC 默认 2 倍显示下对应 6-9px 原生，故周期取 4-9 而不是更粗的场。
    tile = mottled(MATERIALS["d"]["pal"], seed, periods, dists[variant % 6])
    # 土粒：稀疏 2×2 深/浅小块，让土面有颗粒感而不是大块软迷彩
    pal = MATERIALS["d"]["pal"]
    for cy in range(3, SIZE - 5, 2):
        for cx in range(3, SIZE - 5, 2):
            r = r01(seed, "grain", cx, cy)
            if r < 0.09:
                for dx in (0, 1):
                    for dy in (0, 1):
                        tile[cy + dy][cx + dx] = pal["shade"]
            elif r < 0.15:
                for dx in (0, 1):
                    for dy in (0, 1):
                        tile[cy + dy][cx + dx] = pal["light"]
            elif r < 0.19:
                tile[cy][cx] = pal["shade"]
                tile[cy][cx + 1] = pal["shade"]
    return tile


# 结构类特征尺度：按**角色比例尺**定。赛菲莉娅角色约 16 原生像素高、砖 4px（角色的 1/4）；
# OCC 角色 32×64（64px 高）→ 砖 16px、缝 4px。逻辑格仍是 32×32，故一格放 2 块砖。
STONE_PITCH, STONE_SEAM = 16, 4
WOOD_PITCH, WOOD_SEAM = 16, 4
DIRT_PERIODS = [(3, 5), (2, 4), (3, 6), (2, 5), (3, 5), (2, 4)]


def rebuild_cache():
    global TILE_CACHE
    TILE_CACHE = {}
    for mk, (fn, n) in FLOORS.items():
        for i in range(n):
            TILE_CACHE[(mk, i)] = fn(h("v3seed", mk) % 997 + i * 17 + 21, i)


def build_stone(seed, variant=0):
    """石砖：按"屏幕尺寸对齐"取 16px 层距 + 3px 结构缝。

    赛菲莉娅石砖 4px + 1px 缝 = 5 原生像素 = 30 屏幕像素；OCC 默认 2 倍显示下
    1 原生像素 = 2 屏幕像素，故层距取 16px（32 屏幕像素）、缝取 3px（6 屏幕像素）。
    """
    pal = MATERIALS["s"]["pal"]
    tile = [[pal["face"]] * SIZE for _ in range(SIZE)]
    pitch, seam = STONE_PITCH, STONE_SEAM
    chip = 0.45 if variant == 0 else 0.7
    for y in range(SIZE):
        row = y // pitch
        off = (pitch // 2) * (row % 2)
        course = y % pitch
        for x in range(SIZE):
            bx = (x - off) % pitch
            if bx >= pitch - seam:
                tile[y][x] = pal["shade"]
                continue
            brick = ((x - off) // pitch) % (SIZE // pitch)
            if course >= pitch - seam:
                tone = "base"
            elif course < 2 and r01(seed, "lit", row, brick) < 0.5:
                tone = "light"
            else:
                tone = "base" if r01(seed, "brick", row, brick) < 0.25 else "face"
            tile[y][x] = pal[tone]
    for y in range(0, SIZE, pitch):
        row = y // pitch
        off = (pitch // 2) * (row % 2)
        for brick in range(SIZE // pitch):
            if r01(seed, "chip", row, brick) < chip:
                for k in range(2):
                    tile[y][(brick * pitch + off + pitch - seam - k) % SIZE] = pal["shade"]
    return tile


def build_grass(seed, variant=0):
    """草：参考里草地就是一块平色，草丛/草叶是独立的装饰物，不属于地面格。

    若把草簇画进地面格，32px 周期会让它们排成规则网点（已实测），
    因此地面格保持平色，草簇留给后续的装饰物批次。
    """
    pal = MATERIALS["g"]["pal"]
    return [[pal["face"]] * SIZE for _ in range(SIZE)]


def build_wood(seed, variant=0):
    """木板：按屏幕尺寸对齐取 16px 板层距 + 3px 板缝，板端错缝 8px。"""
    pal = MATERIALS["w"]["pal"]
    tile = [[pal["face"]] * SIZE for _ in range(SIZE)]
    pitch, seam = WOOD_PITCH, WOOD_SEAM
    for y in range(SIZE):
        row = y // pitch
        off = (pitch // 2) * (row % 2)
        course = y % pitch
        for x in range(SIZE):
            plank = ((x - off) // pitch) % (SIZE // pitch)
            if course >= pitch - seam:
                tone = "shade"
            elif course == 0:
                tone = "light"
            else:
                tone = "base" if r01(seed, "plank", row, plank) < 0.14 else "face"
            tile[y][x] = pal[tone]
        for x in range(SIZE):
            if (x - off) % (pitch * 2) >= pitch * 2 - seam and course < pitch - seam:
                tile[y][x] = pal["shade"]
    if variant == 1:  # 旧木：两处磨损
        for x, y in ((7, 3), (8, 3), (9, 3), (23, 19), (24, 19)):
            tile[y][x] = pal["base"]
    return tile


def build_carpet(seed, variant=0):
    """地毯：参考里是近乎纯色，只留几处磨痕，不做织纹方格。"""
    pal = MATERIALS["c"]["pal"]
    tile = [[pal["face"]] * SIZE for _ in range(SIZE)]
    for cx, cy in ((9, 13), (24, 5), (17, 27)):
        for dx in (0, 1):
            for dy in (0, 1):
                tile[cy + dy][cx + dx] = pal["base"]
        tile[cy][cx] = pal["shade"]
    if variant == 1:
        for x, y in ((14, 2), (15, 2), (16, 2), (5, 20), (6, 20)):
            tile[y][x] = pal["light"]
    return tile


FLOORS = {
    "dirt": (build_dirt, 6),
    "stone": (build_stone, 2),
    "grass": (build_grass, 1),
    "wood": (build_wood, 2),
    "carpet": (build_carpet, 2),
}

KEY_OF = {"d": "dirt", "s": "stone", "g": "grass", "w": "wood", "c": "carpet"}
MAT_OF = {v: k for k, v in KEY_OF.items()}

TILE_CACHE = {}
for mk, (fn, n) in FLOORS.items():
    for i in range(n):
        TILE_CACHE[(mk, i)] = fn(h("v3seed", mk) % 997 + i * 17 + 21, i)


def tile_of(mat, gx, gy):
    mk = KEY_OF[mat]
    n = FLOORS[mk][1]
    idx = (gx + 2 * gy + h("%sblock" % mk, gx // 2, gy // 2)) % n
    return TILE_CACHE[(mk, idx)]


# ---- 过渡 -------------------------------------------------------------------
DIRS = {"n": (0, -1), "e": (1, 0), "s": (0, 1), "w": (-1, 0)}


def _edge(d, i, k):
    if d == "n":
        return i, k
    if d == "s":
        return i, SIZE - 1 - k
    if d == "w":
        return k, i
    return SIZE - 1 - k, i


BOUNDARY_DIV = 1   # 交界阶梯 8-16px：与 16px 砖距同级


def stamp_boundary(px, ox, oy, mat, d, seed, straight=False):
    own = MATERIALS[mat]["pal"]
    other_mat = None
    for m in MATERIALS:
        if m != mat and MATERIALS[m]["name"] == OTHER_NAME[mat]:
            other_mat = m
    other = MATERIALS[other_mat]["pal"]
    if straight:
        # 人工铺装（木、地毯）：直边收口 + 2px 本材质缝，不做互锁咬边
        for i in range(SIZE):
            for k in range(2):
                cx, cy = _edge(d, i, k)
                px[ox + cx, oy + cy] = own["seam"]
        return
    x = 0
    blob = 3 if BOUNDARY_DIV == 1 else 2
    while x < SIZE:
        run = max(3, (8 + int(r01(seed, "run", d, x) * 9)) // BOUNDARY_DIV)
        depth = int(r01(seed, "depth", d, x) * 7) // BOUNDARY_DIV
        for i in range(x, min(x + run, SIZE)):
            for k in range(depth):
                cx, cy = _edge(d, i, k)
                px[ox + cx, oy + cy] = other["face"]
            for k in range(max(1, 2 // BOUNDARY_DIV)):
                cx, cy = _edge(d, i, depth + k)
                px[ox + cx, oy + cy] = own["seam"]
        if r01(seed, "blob", d, x) < 0.22:
            for dx in range(blob):
                for dy in range(blob):
                    cx, cy = _edge(d, x + dx, min(depth + 3, 9) + dy)
                    if 0 <= cx < SIZE and 0 <= cy < SIZE:
                        px[ox + cx, oy + cy] = other["face"]
        x += run


# 人工铺装材质：与任何材质相接都是直边收口
TRIMMED = {"w", "c"}


OTHER_NAME = {}


def render_board(grid, with_facade=False, seed=333):
    gh, gw = len(grid), len(grid[0])
    height = gh * SIZE + (FACADE if with_facade else 0)
    img = Image.new("RGB", (gw * SIZE, height))
    px = img.load()
    for gy in range(gh):
        for gx in range(gw):
            tile = tile_of(grid[gy][gx], gx, gy)
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
                    OTHER_NAME[mat] = KEY_OF[grid[ny][nx]]
                    straight = mat in TRIMMED or grid[ny][nx] in TRIMMED
                    stamp_boundary(px, ox, oy, mat, d, seed, straight)
    if with_facade:
        for gx in range(gw):
            img.paste(facade_band(grid[gh - 1][gx]), (gx * SIZE, gh * SIZE))
    return img


NEIGHBORS = (("n", 0, -1), ("e", 1, 0), ("s", 0, 1), ("w", -1, 0))


def facade_band(mat):
    """南向外立面（8px）：2px 转折缝 + 1px 受光上沿 + 3px 主体（16px 竖缝）+ 2px 底部收暗。"""
    pal = MATERIALS[mat]["pal"]
    im = Image.new("RGB", (SIZE, FACADE))
    px = im.load()
    face = pal["shade"]
    for x in range(SIZE):
        for y in (0, 1):
            px[x, y] = pal["seam"]
        px[x, 2] = pal["light"] if mat != "c" else pal["base"]
        for y in (3, 4, 5):
            px[x, y] = face
        if x % 16 >= 13:
            for y in (3, 4, 5):
                px[x, y] = pal["seam"]
        if mat in ("d", "g") and r01("clod", mat, x // 3) < 0.30:
            for y in (4, 5):
                px[x, y] = pal["seam"]
        for y in (6, 7):
            px[x, y] = pal["seam"]
    return im


def piece(own, dirs, others):
    others = [o for o in others]
    grid = [["?"] * 3 for _ in range(3)]
    grid[1][1] = own
    for dy in range(3):
        for dx in range(3):
            if grid[dy][dx] == "?":
                grid[dy][dx] = own
    for d in dirs:
        dx, dy = DIRS[d]
        grid[1 + dy][1 + dx] = others[0]
    if len(dirs) == 2:
        dx = sum(DIRS[d][0] for d in dirs)
        dy = sum(DIRS[d][1] for d in dirs)
        grid[1 + dy][1 + dx] = others[0]
    OTHER_NAME.clear()
    board = render_board(["".join(r) for r in grid])
    return board.crop((SIZE, SIZE, SIZE * 2, SIZE * 2))


def outer_piece(mat):
    board = render_board([mat])
    out = Image.new("RGB", (SIZE, SIZE + FACADE))
    out.paste(board.crop((0, 0, SIZE, SIZE)), (0, 0))
    out.paste(facade_band(mat), (0, SIZE))
    return out


# ---- 输出 -------------------------------------------------------------------
def upscale(img, f):
    return img.resize((img.width * f, img.height * f), Image.NEAREST)


def unique_colors(img):
    return len({c for _n, c in img.convert("RGB").getcolors(maxcolors=1 << 20)})


def tile_image(t):
    im = Image.new("RGB", (SIZE, SIZE))
    im.putdata([t[y][x] for y in range(SIZE) for x in range(SIZE)])
    return im


def sheet(items, cols, scale, gap=10, bg=(20, 20, 24)):
    cw = max(im.width for _n, im in items) * scale
    ch = max(im.height for _n, im in items) * scale
    rows = (len(items) + cols - 1) // cols
    out = Image.new("RGB", (cols * cw + (cols - 1) * gap, rows * ch + (rows - 1) * gap), bg)
    for i, (_n, im) in enumerate(items):
        r, c = divmod(i, cols)
        out.paste(upscale(im, scale), (c * (cw + gap), r * (ch + gap)))
    return out


PAIRS = [("d", "s"), ("d", "g"), ("s", "g"), ("s", "w"), ("s", "c"), ("d", "c")]


def main():
    report = {"materials": {k: {kk: "#%02X%02X%02X" % vv for kk, vv in v["pal"].items()}
                            for k, v in MATERIALS.items()}, "assets": {}, "coverage": {}}

    floors = []
    for mk, (fn, n) in FLOORS.items():
        for i in range(n):
            t = TILE_CACHE[(mk, i)]
            im = tile_image(t)
            name = "%s_%s_32.png" % (mk, chr(ord("a") + i))
            im.save(os.path.join(HERE, name))
            report["assets"][name] = {"role": "floor_tile_32", "material": mk, "colors": unique_colors(im)}
            floors.append((name, im))
            if i == 0:
                from collections import Counter
                cnt = Counter(tuple(px) for row in t for px in row)
                report["coverage"][mk] = {("%02X%02X%02X" % c): round(v / 1024, 3)
                                          for c, v in cnt.most_common(4)}
    sheet(floors, 6, 5).save(os.path.join(HERE, "contact_floors_5x.png"))

    edges, corners = [], []
    for a, b in PAIRS:
        for own, other, tag in ((a, b, "%s_%s" % (KEY_OF[a], KEY_OF[b])),
                                (b, a, "%s_%s" % (KEY_OF[b], KEY_OF[a]))):
            for d in ("n", "e", "s", "w"):
                im = piece(own, [d], [other])
                name = "%s_%s_32.png" % (tag, d)
                im.save(os.path.join(HERE, name))
                report["assets"][name] = {"role": "terrain_adjacency_overlay_32", "colors": unique_colors(im)}
                edges.append((name, im))
            for d1, d2 in (("n", "e"), ("s", "e"), ("s", "w"), ("n", "w")):
                im = piece(own, [d1, d2], [other])
                name = "%s_%s%s_32.png" % (tag, d1, d2)
                im.save(os.path.join(HERE, name))
                report["assets"][name] = {"role": "terrain_adjacency_overlay_32", "colors": unique_colors(im)}
                corners.append((name, im))
    sheet(edges, 8, 4).save(os.path.join(HERE, "contact_edges_4x.png"))
    sheet(corners, 8, 4).save(os.path.join(HERE, "contact_corners_4x.png"))

    outer = []
    for mat in MATERIALS:
        im = outer_piece(mat)
        name = "%s_outer_s_32x40.png" % KEY_OF[mat]
        im.save(os.path.join(HERE, name))
        report["assets"][name] = {"role": "directional_outer_edge_32x40", "colors": unique_colors(im)}
        outer.append((name, im))
    sheet(outer, 5, 5).save(os.path.join(HERE, "contact_outer_5x.png"))

    # 演示地图：草->土->石台地->木/地毯室内
    gw, gh = 26, 15
    g = [["g"] * gw for _ in range(gh)]
    for y in range(4, 10):
        for x in range(3, 12):
            g[y][x] = "d"
    for y in range(2, 7):
        for x in range(14, 23):
            g[y][x] = "s"
    for x in range(17, 21):
        g[3][x] = "c"
    for y in range(7, 10):
        for x in range(15, 19):
            g[y][x] = "s"
    for x in range(19, 22):
        g[8][x] = "w"
    grid = ["".join(r) for r in g]
    demo = render_board(grid, with_facade=True)
    demo.save(os.path.join(HERE, "demo_map_1x.png"))
    upscale(demo, 2).save(os.path.join(HERE, "demo_map_2x.png"))
    upscale(demo.crop((SIZE * 12, SIZE * 1, SIZE * 24, SIZE * 10)), 5).save(
        os.path.join(HERE, "mix_zoom_5x.png"))
    upscale(demo.crop((SIZE * 2, SIZE * 11, SIZE * 14, SIZE * 15 + FACADE)), 5).save(
        os.path.join(HERE, "facade_zoom_5x.png"))

    with open(os.path.join(HERE, "drawn_report_v3.json"), "w", encoding="utf-8") as fh:
        json.dump(report, fh, ensure_ascii=False, indent=1)
    print("assets=%d" % len(report["assets"]))
    for k, v in report["coverage"].items():
        print("  %-7s" % k, " ".join("%s:%s" % (a, b) for a, b in list(v.items())[:3]))


if __name__ == "__main__":
    main()
