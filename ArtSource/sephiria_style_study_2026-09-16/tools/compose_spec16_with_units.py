# -*- coding: utf-8 -*-
"""新单位降半测试 + 用降半后的单位拼 16 PPU 场景。

新单位（09-14/09-15）是粗颗粒 + 黑描边，与 Sephiria 语言同族；画布 32×64、身体高 34-51px。
降到 16×32（身体 17-26px）后正好接近参考"角色约 1.2 格"的比例。
"""
import os

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = r"E:\数据库\OCC_Codex"
STUDY = os.path.join(ROOT, "ArtSource", "sephiria_style_study_2026-09-16")
HERE = os.path.join(STUDY, "drawn")
OUT = os.path.join(STUDY, "project_assets")
os.makedirs(OUT, exist_ok=True)
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]

UNITS = ["pyromancer.png", "raider_idle_32x64.png", "maintenance_guard_native.png",
         "sephiria_unit_native_grid_64.png", "tether_hound_idle_64x32_final.png"]


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def find(name):
    for dp, dn, fn in os.walk(ROOT):
        if any(s in dp for s in ("\\Library\\", "\\obj\\", "\\Temp\\", "\\.git\\", "\\Logs\\")):
            continue
        if name in fn:
            return os.path.join(dp, name)
    return None


def halve(im):
    return im.resize((im.width // 2, im.height // 2), Image.NEAREST)


def checker(w, h, s=8):
    im = Image.new("RGB", (w, h), (54, 54, 62))
    d = ImageDraw.Draw(im)
    for y in range(0, h, s):
        for x in range(0, w, s):
            if ((x // s) + (y // s)) % 2:
                d.rectangle([x, y, x + s - 1, y + s - 1], fill=(66, 66, 76))
    return im


def main():
    # ---- 降半对照：原图 4 倍 vs 降半 8 倍（显示尺寸接近，便于比细节）----
    pairs = []
    for name in UNITS:
        p = find(name)
        if not p:
            continue
        src = Image.open(p).convert("RGBA")
        half = halve(src)
        a = src.resize((src.width * 4, src.height * 4), Image.NEAREST)
        b = half.resize((half.width * 8, half.height * 8), Image.NEAREST)
        pairs.append((name, a, b))
    if pairs:
        cw = max(max(a.width, b.width) for _n, a, b in pairs)
        ch = max(max(a.height, b.height) for _n, a, b in pairs)
        gap, top = 18, 34
        sheet = Image.new("RGB", (len(pairs) * (cw * 2 + 8) + (len(pairs) - 1) * gap + 20, top + ch + 26), (20, 20, 24))
        d = ImageDraw.Draw(sheet)
        d.text((8, 8), "新单位：原尺寸 4×（左） vs 最近邻降半 8×（右）——显示尺寸接近，用于比细节损失",
               font=font(20), fill=(235, 235, 240))
        x = 10
        for name, a, b in pairs:
            bg = checker(cw * 2 + 8, ch)
            bg.paste(a, (0, ch - a.height), a)
            bg.paste(b, (cw + 8, ch - b.height), b)
            sheet.paste(bg, (x, top))
            d.text((x, top + ch + 3), name[:26], font=font(13), fill=(180, 180, 188))
            x += cw * 2 + 8 + gap
        sheet.save(os.path.join(OUT, "new_unit_halving_4x_vs_8x.png"))
        print("saved new_unit_halving_4x_vs_8x.png", sheet.size)

    # ---- 用降半单位拼 16 PPU 场景 ----
    import importlib.util
    spec = importlib.util.spec_from_file_location("s16", os.path.join(HERE, "draw_spec16.py"))
    s16 = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(s16)
    b16 = s16.b16

    grid = s16.grid_board()
    board = b16.render_board(grid).convert("RGBA")
    for gx, gy in s16.V_WALL:
        board.alpha_composite(s16.wall_v(), (gx * s16.CELL, gy * s16.CELL - 8))
    for gx, gy in s16.H_WALL:
        board.alpha_composite(s16.wall_h(), (gx * s16.CELL, gy * s16.CELL - 8))
    for name, gx, gy in s16.PROPS:
        im = {"crate": s16.crate(), "barrel": s16.barrel(), "torch": s16.torch(), "chest": s16.chest()}[name]
        board.alpha_composite(im, (gx * s16.CELL + (s16.CELL - im.width) // 2, (gy + 1) * s16.CELL - im.height))
    # 降半新单位落位（底边对齐所属格底边）
    placed = [("pyromancer.png", 9, 7), ("raider_idle_32x64.png", 6, 6),
              ("maintenance_guard_native.png", 11, 8), ("sephiria_unit_native_grid_64.png", 8, 9)]
    for name, gx, gy in placed:
        p = find(name)
        if not p:
            continue
        u = halve(Image.open(p).convert("RGBA"))
        board.alpha_composite(u, (gx * s16.CELL + (s16.CELL - u.width) // 2, (gy + 1) * s16.CELL - u.height))

    disp = board.resize((board.width * 6, board.height * 6), Image.NEAREST).convert("RGB")
    lit = s16.simulate_light(disp)
    page = Image.new("RGB", s16.CANVAS, (10, 10, 14))
    page.paste(lit, ((s16.MAP_W - lit.width) // 2, (s16.CANVAS[1] - lit.height) // 2))
    page.save(os.path.join(OUT, "scene_spec16_project_units.png"))
    print("saved scene_spec16_project_units.png", page.size)


if __name__ == "__main__":
    main()
