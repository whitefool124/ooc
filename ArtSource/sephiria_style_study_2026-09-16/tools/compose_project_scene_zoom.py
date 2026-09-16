# -*- coding: utf-8 -*-
"""把现有 32px 规格素材"放大使用"来适配 16px 规格：同一套资产分别按 3 倍与 6 倍显示。

- 3 倍：一格 32px → 96 屏幕像素（与参考一格同屏尺寸），但每像素只有 3 屏幕像素。
- 6 倍：一格 32px → 192 屏幕像素（放大使用），每像素 6 屏幕像素，颗粒与参考一致，
  代价是一屏只看到约 7×4 格，且 64px 单位会占 2 格高。
再与 16 PPU 重画版（6 倍、一格 96 屏幕像素）并排。
"""
import os

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ART = r"E:\数据库\OCC_Codex\UnityProject\Assets\Game\Resources\Art"
STUDY = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HERE = os.path.join(STUDY, "drawn")
OUT = os.path.join(STUDY, "project_assets")
os.makedirs(OUT, exist_ok=True)

CELL = 32
CANVAS = (1920, 1080)
MAP_W = 1440
VIEW = (1408, 768)
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
UNITS = [("hero.png", 4, 4), ("shieldguard.png", 5, 5), ("rune_arbalist.png", 3, 5), ("elite.png", 6, 2)]
LIGHTS = [(2 * CELL, 3 * CELL), (9 * CELL, 3 * CELL), (7 * CELL, 5 * CELL)]


def compose_native():
    field = art("FormalAcademyFloorFields", "academy_floorfield_courtyard_12x9.png")
    base = field.copy()
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
        if cx - r >= w or cy - r >= h:
            continue
        grad = Image.radial_gradient("L").resize((2 * r, 2 * r)).point(lambda v: int((1 - v / 255.0) ** 2.4 * 255))
        warm = Image.new("RGB", (2 * r, 2 * r), (255, 205, 142))
        warm.putalpha(grad)
        box = (cx - r, cy - r, cx + r, cy + r)
        region = acc.crop(box).convert("RGBA")
        region.alpha_composite(warm)
        acc.paste(region.convert("RGB"), (box[0], box[1]))
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


def place(disp):
    """把场景按 1408×768 视口居中放进 1920×1080。"""
    vw, vh = VIEW
    if disp.width >= vw and disp.height >= vh:
        x0, y0 = (disp.width - vw) // 2, (disp.height - vh) // 2
        crop = disp.crop((x0, y0, x0 + vw, y0 + vh))
    else:
        crop = disp
    canvas = Image.new("RGB", CANVAS, (10, 10, 14))
    canvas.paste(crop, (16 + (vw - crop.width) // 2, 80 + (vh - crop.height) // 2))
    return canvas


def bar(text, w=1920, h=44):
    im = Image.new("RGB", (w, h), (14, 14, 18))
    ImageDraw.Draw(im).text((12, 8), text, font=font(22), fill=(235, 235, 240))
    return im


def main():
    native = compose_native()
    outs = {}
    for scale in (3, 6):
        disp = native.resize((native.width * scale, native.height * scale), Image.NEAREST).convert("RGB")
        lit = simulate_light(disp, scale)
        page = place(lit)
        name = "project_assets_%dx_lit.png" % scale
        page.save(os.path.join(OUT, name))
        outs[scale] = page
        print("saved", name, "cell =", CELL * scale, "screen px")

    mine = Image.open(os.path.join(HERE, "spec16", "scene_1920x1080_lit.png")).convert("RGB")

    rows = [
        ("A 现有 32px 资产 · 3 倍显示（一格 96 屏幕像素，每像素 3）", outs[3]),
        ("B 现有 32px 资产 · 6 倍显示（放大使用，一格 192 屏幕像素，每像素 6）", outs[6]),
        ("C 按实测规格重画 16 PPU · 6 倍显示（一格 96 屏幕像素，每像素 6）", mine),
    ]
    sheet = Image.new("RGB", (1920, sum(44 + 1080 for _ in rows)), (8, 8, 12))
    y = 0
    for text, img in rows:
        sheet.paste(bar(text), (0, y))
        sheet.paste(img, (0, y + 44))
        y += 44 + 1080
    sheet.save(os.path.join(OUT, "compare_3x_6x_spec16.png"))
    print("saved compare_3x_6x_spec16.png", sheet.size)


if __name__ == "__main__":
    main()
