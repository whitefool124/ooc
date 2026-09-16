# -*- coding: utf-8 -*-
"""版本 A：16 PPU 地面 + 现有 32 PPU 素材，全部 6 倍显示（颗粒统一 = 6），**不做光照/暗区**。

人物保持 32×64 原尺寸（约 3.2 格高，允许压过物件）；结构类直接用现有资产。
输出 1920×1080 全屏。
"""
import importlib.util
import os

from PIL import Image

ROOT = r"E:\数据库\OCC_Codex"
ART = os.path.join(ROOT, "UnityProject", "Assets", "Game", "Resources", "Art")
STUDY = os.path.join(ROOT, "ArtSource", "sephiria_style_study_2026-09-16")
HERE = os.path.join(STUDY, "drawn")
OUT = os.path.join(STUDY, "project_assets")
os.makedirs(OUT, exist_ok=True)

ZOOM = 6                 # 统一显示倍率：地面 16px 格与 32px 素材都按 6 倍
CANVAS = (1920, 1080)
MAP_W = 1440
CELL = 16                # 地面逻辑格（16 PPU）


def art(folder, name):
    return Image.open(os.path.join(ART, folder, name)).convert("RGBA")


def find(name):
    for dp, dn, fn in os.walk(ROOT):
        if any(s in dp for s in ("\\Library\\", "\\obj\\", "\\Temp\\", "\\.git\\", "\\Logs\\")):
            continue
        if name in fn:
            return os.path.join(dp, name)
    return None


# 单位（现有新批次，原尺寸 32×64 / 64×32）
UNITS = [("pyromancer.png", 3, 7), ("raider_idle_32x64.png", 1, 5),
         ("maintenance_guard_native.png", 5, 8), ("sephiria_unit_native_grid_64.png", 4, 4),
         ("tether_hound_idle_64x32_final.png", 7, 3)]
# 结构（现有资产，按底部中心落在指定格）
STRUCTS = [
    ("FormalAcademyStructures32", "academy_aether_device_2x2.png", 9, 2),
    ("FormalAcademyStructures32", "academy_aether_pump_2x2.png", 12, 5),
    ("FormalAcademyStructures32", "academy_alchemy_bench_2x1.png", 6, 6),
    ("FormalAcademyStructures32", "academy_archive_cabinet_2x1.png", 2, 2),
    ("FormalAcademyStructures32", "academy_broken_wall_3x1.png", 8, 9),
    ("FormalAcademyCombat32", "academy_aether_crystal_intact.png", 12, 8),
    ("FormalAcademyCombat32", "academy_aether_crystal_damaged.png", 10, 8),
]


def main():
    spec = importlib.util.spec_from_file_location("s16", os.path.join(HERE, "draw_spec16.py"))
    s16 = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(s16)
    b16 = s16.b16

    grid = s16.grid_board()
    board = b16.render_board(grid).convert("RGBA")
    # 我方 16px 墙体与道具（原样，保留描边）
    for gx, gy in s16.V_WALL:
        board.alpha_composite(s16.wall_v(), (gx * CELL, gy * CELL - 8))
    for gx, gy in s16.H_WALL:
        board.alpha_composite(s16.wall_h(), (gx * CELL, gy * CELL - 8))
    for name, gx, gy in s16.PROPS:
        im = {"crate": s16.crate(), "barrel": s16.barrel(), "torch": s16.torch(), "chest": s16.chest()}[name]
        board.alpha_composite(im, (gx * CELL + (CELL - im.width) // 2, (gy + 1) * CELL - im.height))

    disp = board.resize((board.width * ZOOM, board.height * ZOOM), Image.NEAREST).convert("RGBA")

    def place(im, gx, gy):
        big = im.resize((im.width * ZOOM, im.height * ZOOM), Image.NEAREST)
        cx = gx * CELL * ZOOM
        cy = (gy + 1) * CELL * ZOOM - big.height
        disp.alpha_composite(big, (cx, cy))

    for folder, name, gx, gy in STRUCTS:
        try:
            place(art(folder, name), gx, gy)
        except Exception as exc:  # noqa: BLE001
            print("skip", name, exc)
    for name, gx, gy in UNITS:
        p = find(name)
        if p:
            place(Image.open(p).convert("RGBA"), gx, gy)

    # 直接输出，不做光照 / 暗区
    page = Image.new("RGB", CANVAS, (26, 26, 32))
    page.paste(disp.convert("RGB"), ((MAP_W - disp.width) // 2, (CANVAS[1] - disp.height) // 2))
    page.save(os.path.join(OUT, "scene_A_nolight.png"))
    disp.convert("RGB").save(os.path.join(OUT, "scene_A_nolight_boardonly.png"))
    print("saved scene_A_nolight.png / scene_A_nolight_boardonly.png", disp.size)


if __name__ == "__main__":
    main()
