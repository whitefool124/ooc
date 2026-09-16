# -*- coding: utf-8 -*-
"""版本 B′：全套现有 32 PPU 资产（预绘地面场 + 结构 + 单位），统一 6 倍显示，不加光照/暗区。

与版本 A（16 PPU 地面 + 32 PPU 物件）对照：看清"地面与物件同一 PPU"与"混用"的差别。
"""
import os

from PIL import Image

ROOT = r"E:\数据库\OCC_Codex"
ART = os.path.join(ROOT, "UnityProject", "Assets", "Game", "Resources", "Art")
STUDY = os.path.join(ROOT, "ArtSource", "sephiria_style_study_2026-09-16")
OUT = os.path.join(STUDY, "project_assets")
os.makedirs(OUT, exist_ok=True)

CELL = 32                # 现有资产的逻辑格（32 PPU）
ZOOM = 6                 # 统一 6 倍 → 颗粒 6
CANVAS = (1920, 1080)
VIEW = (1408, 768)


def art(folder, name):
    return Image.open(os.path.join(ART, folder, name)).convert("RGBA")


def find(name):
    for dp, dn, fn in os.walk(ROOT):
        if any(s in dp for s in ("\\Library\\", "\\obj\\", "\\Temp\\", "\\.git\\", "\\Logs\\")):
            continue
        if name in fn:
            return os.path.join(dp, name)
    return None


STRUCTS = [
    ("FormalAcademyStructures32", "academy_cloister_wall_4x1.png", 0, 0),
    ("FormalAcademyStructures32", "academy_broken_wall_3x1.png", 5, 0),
    ("FormalAcademyStructures32", "academy_aether_device_2x2.png", 1, 2),
    ("FormalAcademyStructures32", "academy_aether_pump_2x2.png", 8, 2),
    ("FormalAcademyStructures32", "academy_archive_cabinet_2x1.png", 4, 3),
    ("FormalAcademyStructures32", "academy_alchemy_bench_2x1.png", 4, 6),
    ("FormalAcademyStructures32", "academy_archive_sorter_2x2.png", 1, 6),
    ("FormalAcademyCombat32", "academy_aether_crystal_intact.png", 7, 5),
    ("FormalAcademyCombat32", "academy_aether_crystal_damaged.png", 10, 4),
]
UNITS = [("hero.png", 4, 4), ("shieldguard.png", 5, 5), ("rune_arbalist.png", 3, 5), ("elite.png", 6, 2),
         ("pyromancer.png", 7, 6), ("raider_idle_32x64.png", 2, 3),
         ("tether_hound_idle_64x32_final.png", 8, 7)]


def main():
    base = art("FormalAcademyFloorFields", "academy_floorfield_courtyard_12x9.png").copy()
    for folder, name, gx, gy in STRUCTS:
        im = art(folder, name)
        base.alpha_composite(im, (gx * CELL, (gy + 1) * CELL - im.height))
    for name, gx, gy in UNITS:
        p = find(name)
        if not p:
            continue
        u = Image.open(p).convert("RGBA")
        base.alpha_composite(u, (gx * CELL + (CELL - u.width) // 2, (gy + 1) * CELL - u.height + 4))

    disp = base.resize((base.width * ZOOM, base.height * ZOOM), Image.NEAREST).convert("RGB")
    vw, vh = VIEW
    if disp.width >= vw and disp.height >= vh:
        x0, y0 = (disp.width - vw) // 2, (disp.height - vh) // 2
        disp = disp.crop((x0, y0, x0 + vw, y0 + vh))
    page = Image.new("RGB", CANVAS, (26, 26, 32))
    page.paste(disp, (16 + (vw - disp.width) // 2, 80 + (vh - disp.height) // 2))
    page.save(os.path.join(OUT, "scene_B_existing_6x_nolight.png"))
    print("saved scene_B_existing_6x_nolight.png", page.size, "cell =", CELL * ZOOM, "screen px")


if __name__ == "__main__":
    main()
