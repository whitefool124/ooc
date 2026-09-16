# -*- coding: utf-8 -*-
"""最新一批角色资产图鉴（native32 口径），含日期与尺寸标注。"""
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = r"E:\数据库\OCC_Codex"
STUDY = os.path.join(ROOT, "ArtSource", "sephiria_style_study_2026-09-16")
OUT = os.path.join(STUDY, "project_assets")
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]

CANDIDATES = [
    ("pyromancer.png", r"UnityProject\Assets\Game\Resources\Art\FormalUnits64", "09-15"),
    ("raider_idle_32x64.png", r"Worldbuilding\归档\2026-09-15_破坏者站姿正式化\round2", "09-15"),
    ("maintenance_guard_native.png", r"UnityProject\Reports\CombatTestArena\native32_batch_stability_v1", "09-14"),
    ("tether_hound_idle_64x32_final.png", r"Worldbuilding\归档\2026-09-14_寻迹兽站姿正式化\round2", "09-14"),
    ("sephiria_unit_native_grid_64.png", r"UnityProject\Assets\Game\Resources\Art\CombatTestArenaSephiriaDebug", "09-14"),
    ("sephiria_unit_side_64.png", r"UnityProject\Assets\Game\Resources\Art\CombatTestArenaSephiriaDebug", "09-13"),
    ("hero.png", r"UnityProject\Assets\Game\Resources\Art\FormalUnits64", "08-22"),
    ("elite.png", r"UnityProject\Assets\Game\Resources\Art\FormalUnits64", "08-22"),
]


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def checker(w, h, s=8):
    im = Image.new("RGB", (w, h), (56, 56, 64))
    d = ImageDraw.Draw(im)
    for y in range(0, h, s):
        for x in range(0, w, s):
            if ((x // s) + (y // s)) % 2:
                d.rectangle([x, y, x + s - 1, y + s - 1], fill=(68, 68, 78))
    return im


def find(name):
    for dp, dn, fn in os.walk(ROOT):
        if any(s in dp for s in ("\\Library\\", "\\obj\\", "\\Temp\\", "\\.git\\", "\\Logs\\")):
            continue
        if name in fn:
            return os.path.join(dp, name)
    return None


def main():
    items = []
    import datetime
    for name, rel, date in CANDIDATES:
        p = os.path.join(ROOT, rel, name)
        if not os.path.exists(p):
            p = find(name)
        if p and os.path.exists(p):
            st = os.stat(p)
            d = datetime.datetime.fromtimestamp(st.st_mtime).strftime("%m-%d %H:%M")
            items.append((name, d, Image.open(p).convert("RGBA")))
    z = 5
    cw, ch = max(i.width for _n, _d, i in items) * z, max(i.height for _n, _d, i in items) * z
    gap, top = 16, 34
    sheet = Image.new("RGB", (len(items) * cw + (len(items) - 1) * gap, top + ch + 40), (20, 20, 24))
    d = ImageDraw.Draw(sheet)
    d.text((8, 8), "最新一批角色资产（native32 口径，5 倍）", font=font(20), fill=(235, 235, 240))
    for i, (name, date, im) in enumerate(items):
        x = i * (cw + gap)
        bg = checker(cw, ch)
        big = im.resize((im.width * z, im.height * z), Image.NEAREST)
        bg.paste(big, ((cw - big.width) // 2, ch - big.height), big)
        sheet.paste(bg, (x, top))
        d.text((x, top + ch + 4), "%s  %dx%d  %s" % (name[:22], im.width, im.height, date),
               font=font(14), fill=(182, 182, 190))
    sheet.save(os.path.join(OUT, "newest_units_5x.png"))
    print("saved newest_units_5x.png", sheet.size, "items:", [n for n, _d, _i in items])


if __name__ == "__main__":
    main()
