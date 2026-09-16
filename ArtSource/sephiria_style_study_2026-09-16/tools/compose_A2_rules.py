# -*- coding: utf-8 -*-
"""A2：按成文规则铺设的完整战场样板（A 版配置，不加光照/暗区）。

规则（v1，见 analysis 文档同节）：
R1 网格：玩法格 = 32×32 原生像素（32 PPU）；地面美术画在 16px 子网格上，每格 2×2 张。
R2 锚点：物件一律**底边中心**对齐所属格的底边中点；多格物件以**左下格**为所属格，
   底边对齐占格最下排的底边，水平中心对齐占格水平中心。
R3 越格：允许向上/左/右探出占格（武器、高机器）；**不允许**越过占格底边往下探（不得压到前排）。
R4 层次（由后到前）：地面 → 地面叠加 → 墙体 → 大型结构 → 小道具 → 单位 → 覆盖层。
R5 深度排序：同层内按**底边 y 升序**绘制（越靠上越先画）；同 y 按类别优先级（结构<道具<单位）。
R6 单位：32×64 画布，主体已贴画布底边，脚底偏移 = 0。
"""
import importlib.util
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = r"E:\数据库\OCC_Codex"
ART = os.path.join(ROOT, "UnityProject", "Assets", "Game", "Resources", "Art")
STUDY = os.path.join(ROOT, "ArtSource", "sephiria_style_study_2026-09-16")
HERE = os.path.join(STUDY, "drawn")
OUT = os.path.join(STUDY, "project_assets")
os.makedirs(OUT, exist_ok=True)

SPEC = 32           # 玩法格
TILE = 16           # 地面子格
ZOOM = 6
COLS, ROWS = 7, 4   # 盘面（玩法格）
CANVAS = (1920, 1080)
VIEW = (1408, 768)
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]

STRUCTS = [
    ("FormalAcademyStructures32", "academy_aether_device_2x2.png", (1, 0), (2, 2), "structure"),
    ("FormalAcademyStructures32", "academy_alchemy_bench_2x1.png", (4, 0), (2, 1), "structure"),
    ("FormalAcademyStructures32", "academy_archive_cabinet_2x1.png", (5, 3), (2, 1), "structure"),
    ("FormalAcademyCombat32", "academy_aether_inlay_a.png", (0, 2), (1, 1), "prop"),
    ("FormalAcademyCombat32", "academy_aether_inlay_b.png", (1, 2), (1, 1), "prop"),
    ("FormalAcademyCombat32", "academy_aether_inlay_c.png", (2, 2), (1, 1), "prop"),
]
# 16px 小道具（地面子格坐标，2 张 = 1 玩法格）
SMALL = [("crate", 5, 5), ("barrel", 7, 6), ("crate", 11, 5), ("chest", 3, 6), ("torch", 1, 1)]
UNITS = [("pyromancer.png", (4, 2)), ("raider_idle_32x64.png", (2, 1)),
         ("tether_hound_idle_64x32_final.png", (5, 2))]

TILE_KIND = [  # 每张地面子格（8 行 × 14 列，1 行 = 0.5 玩法格）
    "ssssssssssssss",
    "ssssssssssssss",
    "ssssssssssssss",
    "ssssssssssssss",
    "ssssssssssssss",
    "ssssssssssssss",
    "gggggggggggggg",
    "gggggggggggggg",
]


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def art(folder, name):
    return Image.open(os.path.join(ART, folder, name)).convert("RGBA")


def find(name):
    for dp, dn, fn in os.walk(ROOT):
        if any(s in dp for s in ("\\Library\\", "\\obj\\", "\\Temp\\", "\\.git\\", "\\Logs\\")):
            continue
        if name in fn:
            return os.path.join(dp, name)
    return None


def anchor_xy(cell, footprint, size):
    """R2：底边中心对齐。返回原生坐标左上角。"""
    cx, cy = cell
    fw, fh = footprint
    ox = cx * SPEC + (fw * SPEC - size[0]) // 2
    oy = (cy + fh) * SPEC - size[1]
    return ox, oy


def assert_rules(cell, footprint, size, ox, oy):
    """R3：不得越过占格底边往下探。"""
    bottom = (cell[1] + footprint[1]) * SPEC
    assert oy + size[1] <= bottom + 1e-6, "物件越过占格底边：%s" % ((ox, oy, size),)


def main():
    spec = importlib.util.spec_from_file_location("s16", os.path.join(HERE, "draw_spec16.py"))
    s16 = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(s16)
    b16 = s16.b16

    W, H = COLS * SPEC // TILE * TILE, ROWS * SPEC // TILE * TILE   # 原生尺寸
    board = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    # R1：地面 16px 子网格，2×2 张 / 玩法格
    kinds = {"s": b16.TILES["s"], "g": b16.TILES["g"], "d": b16.TILES["d"]}
    for ty in range(H // TILE):
        for tx in range(W // TILE):
            row = TILE_KIND[ty] if ty < len(TILE_KIND) else "g"
            kind = row[tx] if tx < len(row) else "g"
            board.paste(kinds[kind], (tx * TILE, ty * TILE))
    # 地面材质交界（沿用同一套过渡）
    grid_tiles = []
    for ty in range(H // TILE):
        row = TILE_KIND[ty] if ty < len(TILE_KIND) else "g"
        grid_tiles.append("".join((row[tx] if tx < len(row) else "g") for tx in range(W // TILE)))
    board = b16.render_board(grid_tiles).convert("RGBA")

    # R4/R5：分层收集，最后按底边 y 排序绘制
    items = []
    # 墙体（16px 片子，沿北边与西边）
    for gx in range(COLS * 2):
        w = s16.wall_h()
        items.append(((gx * TILE, 0 * SPEC - 8), w, -10 ** 6 + gx, "wall"))
    for gy in range(ROWS * 2):
        v = s16.wall_v()
        items.append(((0 * SPEC, gy * TILE - 8), v, -10 ** 6 + gy, "wall"))

    for folder, name, cell, footprint, kind in STRUCTS:
        im = art(folder, name)
        ox, oy = anchor_xy(cell, footprint, im.size)
        assert_rules(cell, footprint, im.size, ox, oy)
        items.append(((ox, oy), im, oy, kind))
    # 16px 小道具：底边对齐所在子格底边
    small = {"crate": s16.crate(), "barrel": s16.barrel(), "torch": s16.torch(), "chest": s16.chest()}
    for name, tx, ty in SMALL:
        im = small[name]
        ox = tx * TILE + (TILE - im.width) // 2
        oy = (ty + 1) * TILE - im.height
        items.append(((ox, oy), im, oy, "prop"))
    for name, cell in UNITS:
        p = find(name)
        if not p:
            continue
        im = Image.open(p).convert("RGBA")
        # 单位画布 32 宽，占 1×2 格；底边对齐所属格底边
        footprint = (max(1, im.width // SPEC), max(1, im.height // SPEC))
        ox, oy = cell[0] * SPEC + (SPEC - im.width) // 2, (cell[1] + 1) * SPEC - im.height
        items.append(((ox, oy), im, oy, "unit"))

    for (ox, oy), im, sorty, kind in sorted(items, key=lambda t: (t[2], {"wall": 0, "structure": 1, "prop": 2, "unit": 3}[t[3]])):
        board.alpha_composite(im, (ox, oy))

    disp = board.resize((board.width * ZOOM, board.height * ZOOM), Image.NEAREST).convert("RGB")
    vw, vh = VIEW
    page = Image.new("RGB", CANVAS, (26, 26, 32))
    page.paste(disp, (16 + (vw - disp.width) // 2, 80 + (vh - disp.height) // 2))
    page.save(os.path.join(OUT, "scene_A2_rules_1920x1080.png"))

    # 规则示意：把每格的底边中点画出来，便于核对锚点
    ann = page.copy()
    d = ImageDraw.Draw(ann)
    ox0 = 16 + (vw - disp.width) // 2
    oy0 = 80 + (vh - disp.height) // 2
    for cy in range(ROWS):
        for cx in range(COLS):
            px = ox0 + cx * SPEC * ZOOM + SPEC * ZOOM // 2
            py = oy0 + (cy + 1) * SPEC * ZOOM
            d.line([(px - 10, py), (px + 10, py)], fill=(255, 220, 120), width=3)
            d.line([(px, py - 10), (px, py + 10)], fill=(255, 220, 120), width=3)
    ann.save(os.path.join(OUT, "scene_A2_rules_anchors.png"))
    print("saved scene_A2_rules_1920x1080.png / scene_A2_rules_anchors.png", board.size, "->", disp.size)
    print("盘面 %d×%d 玩法格 = %d×%d 地面子格；一屏可见 %d×%d 格" % (COLS, ROWS, COLS * 2, ROWS * 2, vw // (SPEC * ZOOM), vh // (SPEC * ZOOM)))


if __name__ == "__main__":
    main()
