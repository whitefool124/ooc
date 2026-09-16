# -*- coding: utf-8 -*-
"""拼接尝试：用本工程现有 32 PPU 资产，按赛菲莉亚的取景（一格 96 屏幕像素）与光照拼一张战场。

目的：看清"保留现有资产 + 采用参考的取景与光照"会得到什么，与 16 PPU 新版并排对比。
只读现有资产，输出到 ArtSource 学习目录。
"""
import os

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ART = r"E:\数据库\OCC_Codex\UnityProject\Assets\Game\Resources\Art"
STUDY = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HERE = os.path.join(STUDY, "drawn")
OUT = os.path.join(STUDY, "project_assets")
os.makedirs(OUT, exist_ok=True)

CELL = 32                    # 现有资产为 32 PPU
CELL_SCREEN = 96             # 参考里一格在屏幕上是 96 像素
SCALE = CELL_SCREEN // CELL  # = 3 倍显示
CANVAS = (1920, 1080)
MAP_W = 1440
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]


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


# 结构摆放：(文件, 格坐标x, 格坐标y) —— 尺寸决定它占几格
STRUCTS = [
    ("FormalAcademyStructures32", "academy_cloister_wall_4x1.png", 0, 0),
    ("FormalAcademyStructures32", "academy_broken_wall_3x1.png", 5, 0),
    ("FormalAcademyStructures32", "academy_cloister_wall_4x1.png", 8, 0),
    ("FormalAcademyStructures32", "academy_aether_device_2x2.png", 1, 2),
    ("FormalAcademyStructures32", "academy_aether_pump_2x2.png", 8, 2),
    ("FormalAcademyStructures32", "academy_archive_cabinet_2x1.png", 4, 3),
    ("FormalAcademyStructures32", "academy_alchemy_bench_2x1.png", 4, 6),
    ("FormalAcademyStructures32", "academy_archive_sorter_2x2.png", 1, 6),
    ("FormalAcademyCombat32", "academy_aether_crystal_intact.png", 7, 5),
    ("FormalAcademyCombat32", "academy_aether_crystal_damaged.png", 10, 4),
    ("FormalAcademyCombat32", "academy_aether_inlay_a.png", 6, 4),
    ("FormalAcademyCombat32", "academy_aether_inlay_b.png", 6, 5),
]
UNITS = [("hero.png", 4, 4), ("shieldguard.png", 5, 5), ("rune_arbalist.png", 3, 5), ("elite.png", 6, 2)]
LIGHTS = [(2 * CELL, 3 * CELL), (9 * CELL, 3 * CELL), (7 * CELL, 5 * CELL)]   # 灯位（原生像素）


def compose_native():
    field = art("FormalAcademyFloorFields", "academy_floorfield_courtyard_12x9.png")
    W, H = field.size                                     # 384×288 = 12×9 格
    base = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    base.alpha_composite(field)
    for folder, name, gx, gy in STRUCTS:
        try:
            im = art(folder, name)
        except Exception:
            continue
        base.alpha_composite(im, (gx * CELL, (gy + 1) * CELL - im.height))
    for name, gx, gy in UNITS:
        try:
            im = art("FormalUnits64", name)
        except Exception:
            continue
        base.alpha_composite(im, (gx * CELL + (CELL - im.width) // 2, (gy + 1) * CELL - im.height + 6))
    return base


def simulate_light(disp, scale):
    w, h = disp.size
    acc = Image.new("RGB", (w, h), (26, 30, 44))
    for (lx, ly) in LIGHTS:
        cx, cy = lx * scale, ly * scale
        r = int(9 * CELL * scale)
        grad = Image.radial_gradient("L").resize((2 * r, 2 * r)).point(lambda v: int((1 - v / 255.0) ** 2.4 * 255))
        warm = Image.new("RGB", (2 * r, 2 * r), (255, 205, 142))
        warm.putalpha(grad)
        box = (cx - r, cy - r, cx + r, cy + r)
        region = acc.crop(box).convert("RGBA")
        region.alpha_composite(warm)
        acc.paste(region.convert("RGB"), (box[0], box[1]))
    # 单位随身光
    ux, uy = 5 * CELL * scale, 5 * CELL * scale
    r = int(5 * CELL * scale)
    grad = Image.radial_gradient("L").resize((2 * r, 2 * r)).point(lambda v: int((1 - v / 255.0) ** 2.2 * 150))
    cool = Image.new("RGB", (2 * r, 2 * r), (206, 224, 255))
    cool.putalpha(grad)
    box = (ux - r, uy - r, ux + r, uy + r)
    region = acc.crop(box).convert("RGBA")
    region.alpha_composite(cool)
    acc.paste(region.convert("RGB"), (box[0], box[1]))
    lit = ImageChops.multiply(disp, acc)
    return ImageChops.multiply(lit, acc.point(lambda v: min(255, int(v * 1.35))))


def centred(img, bg=(10, 10, 14)):
    canvas = Image.new("RGB", CANVAS, bg)
    canvas.paste(img, ((MAP_W - img.width) // 2, (CANVAS[1] - img.height) // 2))
    return canvas


def main():
    native = compose_native()
    disp = native.resize((native.width * SCALE, native.height * SCALE), Image.NEAREST).convert("RGB")
    centred(disp).save(os.path.join(OUT, "scene_project_assets_nolight.png"))
    lit = simulate_light(disp, SCALE)
    centred(lit).save(os.path.join(OUT, "scene_project_assets_lit.png"))

    # 与 16 PPU 新版并排（上下）
    mine = Image.open(os.path.join(HERE, "spec16", "scene_1920x1080_lit.png")).convert("RGB")
    proj = Image.open(os.path.join(OUT, "scene_project_assets_lit.png")).convert("RGB")
    sheet = Image.new("RGB", (1920, 44 + 1080 + 44 + 1080), (10, 10, 14))
    d = ImageDraw.Draw(sheet)
    d.rectangle([0, 0, 1920, 44], fill=(14, 14, 18))
    d.text((12, 8), "A 本工程现有资产（32 PPU）· 按参考取景 3 倍显示 + 光照模拟", font=font(22), fill=(235, 235, 240))
    sheet.paste(proj, (0, 44))
    d.rectangle([0, 1124, 1920, 1168], fill=(14, 14, 18))
    d.text((12, 1132), "B 按实测规格重画（16 PPU）· 同一取景 + 同类光照", font=font(22), fill=(235, 235, 240))
    sheet.paste(mine, (0, 1168))
    sheet.save(os.path.join(OUT, "compare_project_vs_spec16.png"))
    print("saved scene_project_assets_nolight/lit.png, compare_project_vs_spec16.png")
    print("native", native.size, "-> display", disp.size, "scale", SCALE)


if __name__ == "__main__":
    main()
