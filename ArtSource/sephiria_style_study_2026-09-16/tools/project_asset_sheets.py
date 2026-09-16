# -*- coding: utf-8 -*-
"""把本工程现有美术资产拼成图鉴，并做"降半能否用于 16 PPU"的测试。

只读现有资产，输出到 ArtSource 学习目录，不改动 Unity 工程内文件。
"""
import os

from PIL import Image, ImageDraw, ImageFont

ART = r"E:\数据库\OCC_Codex\UnityProject\Assets\Game\Resources\Art"
STUDY = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(STUDY, "project_assets")
os.makedirs(OUT, exist_ok=True)
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def load(folder, name):
    return Image.open(os.path.join(ART, folder, name)).convert("RGBA")


def checker(w, h, s=8):
    im = Image.new("RGB", (w, h), (58, 58, 66))
    d = ImageDraw.Draw(im)
    for y in range(0, h, s):
        for x in range(0, w, s):
            if ((x // s) + (y // s)) % 2:
                d.rectangle([x, y, x + s - 1, y + s - 1], fill=(70, 70, 80))
    return im


def sheet(items, cols, scale, path, title, sub_font=13):
    cw = max(i.width for _n, i in items) * scale
    ch = max(i.height for _n, i in items) * scale
    gap, top = 14, 34
    rows = (len(items) + cols - 1) // cols
    W = cols * cw + (cols - 1) * gap
    H = top + rows * (ch + 20) + (rows - 1) * gap
    out = Image.new("RGB", (W, H), (22, 22, 26))
    d = ImageDraw.Draw(out)
    d.text((8, 8), title, font=font(20), fill=(235, 235, 240))
    for i, (name, im) in enumerate(items):
        r, c = divmod(i, cols)
        x, y = c * (cw + gap), top + r * (ch + 20 + gap)
        bg = checker(cw, ch)
        big = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        bg.paste(big, ((cw - big.width) // 2, ch - big.height), big)
        out.paste(bg, (x, y))
        d.text((x, y + ch + 3), name[:26], font=font(sub_font), fill=(178, 178, 186))
    out.save(path)
    print("saved", os.path.basename(path), out.size)


def main():
    units = sorted(f for f in os.listdir(os.path.join(ART, "FormalUnits64")) if f.endswith(".png"))
    structs = sorted(f for f in os.listdir(os.path.join(ART, "FormalAcademyStructures32")) if f.endswith(".png"))
    props = sorted(f for f in os.listdir(os.path.join(ART, "FormalAcademyCombat32")) if f.endswith(".png"))
    floors = sorted(f for f in os.listdir(os.path.join(ART, "FormalAcademyIndependentFloors32")) if f.endswith(".png"))

    unit_items = [(n, load("FormalUnits64", n)) for n in units]
    sheet(unit_items, 7, 3, os.path.join(OUT, "units_64.png"), "现有单位（64×64，32 PPU）")

    pick = ["academy_alchemy_bench_2x1.png", "academy_archive_cabinet_2x1.png", "academy_broken_wall_3x1.png",
            "academy_aether_device_2x2.png", "academy_aether_pump_2x2.png", "academy_archive_sorter_2x2.png"]
    pick = [p for p in pick if p in structs] + structs[:8]
    sheet([(n, load("FormalAcademyStructures32", n)) for n in pick], 7, 3,
          os.path.join(OUT, "structures.png"), "现有结构（32 / 64 / 96px，32 PPU）")

    sheet([(n, load("FormalAcademyCombat32", n)) for n in props[:21]], 7, 3,
          os.path.join(OUT, "combat_props.png"), "现有战斗物件（32×32，32 PPU）")
    sheet([(n, load("FormalAcademyIndependentFloors32", n)) for n in floors], 8, 4,
          os.path.join(OUT, "floors_32.png"), "现有独立地砖（32×32，32 PPU）")

    # 降半测试：取 5 个 32px 物件 + 1 个单位，原尺寸 vs 最近邻降半
    test = [("academy_aether_crystal_intact.png", "FormalAcademyCombat32"),
            ("academy_test_book_crate_intact_32.png", "CombatTestArenaGround")]
    pairs = []
    for name, folder in test:
        try:
            src = load(folder, name)
        except Exception:
            continue
        pairs.append((name.replace(".png", "") + " 原尺寸", src))
        pairs.append((name.replace(".png", "") + " 降半", src.resize((max(1, src.width // 2), max(1, src.height // 2)), Image.NEAREST)))
    for n in structs[:3]:
        src = load("FormalAcademyStructures32", n)
        pairs.append((n.replace(".png", "")[:20] + " 原", src))
        pairs.append((n.replace(".png", "")[:20] + " 降半", src.resize((max(1, src.width // 2), max(1, src.height // 2)), Image.NEAREST)))
    unit = load("FormalUnits64", "hero.png")
    pairs.append(("hero 原尺寸 64", unit))
    pairs.append(("hero 降半 32", unit.resize((32, 32), Image.NEAREST)))
    sheet(pairs, 4, 5, os.path.join(OUT, "halving_test.png"), "降半测试：原尺寸 vs 最近邻 1/2（用于 16 PPU）")


if __name__ == "__main__":
    main()
