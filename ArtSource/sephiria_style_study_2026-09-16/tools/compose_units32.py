# -*- coding: utf-8 -*-
"""人物保持 32 PPU 原尺寸（不降半），与 16 PPU 地面在同一倍率下拼接。

人物层：现有新批次单位 32×64 原生像素，按 6 倍显示 → 1 像素 = 6 屏幕像素（与地面颗粒一致）；
尺寸上人物会比格子大（身体约 3 格高），这是本次允许的取舍。
另出一版人物按 4 倍显示的对照，便于比较"大一点"与"更协调"。
"""
import importlib.util
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = r"E:\数据库\OCC_Codex"
STUDY = os.path.join(ROOT, "ArtSource", "sephiria_style_study_2026-09-16")
HERE = os.path.join(STUDY, "drawn")
OUT = os.path.join(STUDY, "project_assets")
os.makedirs(OUT, exist_ok=True)
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]

UNITS = [("pyromancer.png", 9, 7), ("raider_idle_32x64.png", 6, 6),
         ("maintenance_guard_native.png", 11, 8), ("sephiria_unit_native_grid_64.png", 8, 9),
         ("tether_hound_idle_64x32_final.png", 10, 6)]


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


def build_scene(unit_scale, ground_scale=None):
    ground_scale = ground_scale or 6
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

    Z = ground_scale                 # 地面倍率
    disp = board.resize((board.width * Z, board.height * Z), Image.NEAREST).convert("RGBA")
    for name, gx, gy in UNITS:
        p = find(name)
        if not p:
            continue
        u = Image.open(p).convert("RGBA")
        big = u.resize((u.width * unit_scale, u.height * unit_scale), Image.NEAREST)
        # 底边对齐所属格底边，横向居中于所属格
        cx = gx * s16.CELL * Z + (s16.CELL * Z - big.width) // 2
        cy = (gy + 1) * s16.CELL * Z - big.height
        disp.alpha_composite(big, (cx, cy))
    lit = s16.simulate_light(disp.convert("RGB"))
    page = Image.new("RGB", s16.CANVAS, (10, 10, 14))
    page.paste(lit, ((s16.MAP_W - lit.width) // 2, (s16.CANVAS[1] - lit.height) // 2))
    return page


def main():
    a = build_scene(6)              # 人物与地面同为 6 倍：颗粒一致，人物最大（约 3 格高）
    b = build_scene(4)              # 人物 4 倍 + 地面 6 倍：人物约 2 格高，颗粒 4 vs 6
    c = build_scene(4, 4)           # 全部 4 倍：颗粒统一为 4，人物约 2 格高
    a.save(os.path.join(OUT, "scene_units32_at6x.png"))
    b.save(os.path.join(OUT, "scene_units32_at4x.png"))
    c.save(os.path.join(OUT, "scene_all_at4x.png"))
    rows = [("A 人物 6 倍（与地面同颗粒 6）· 人物约 3.2 格高", a),
            ("B 人物 4 倍 · 地面 6 倍 · 人物约 2.1 格高（颗粒 4 vs 6）", b),
            ("C 全部 4 倍（颗粒统一 4）· 人物约 2.1 格高", c)]
    sheet = Image.new("RGB", (1920, sum(44 + 1080 for _ in rows)), (8, 8, 12))
    d = ImageDraw.Draw(sheet)
    y = 0
    for text, img in rows:
        d.rectangle([0, y, 1920, y + 44], fill=(14, 14, 18))
        d.text((12, y + 8), text, font=font(22), fill=(235, 235, 240))
        sheet.paste(img, (0, y + 44))
        y += 44 + 1080
    sheet.save(os.path.join(OUT, "compare_units32_options.png"))
    print("saved scene_units32_at6x/at4x, scene_all_at4x, compare_units32_options.png")


if __name__ == "__main__":
    main()
