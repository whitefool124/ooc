# -*- coding: utf-8 -*-
"""按实机显示尺寸对照：参考 1:1 屏幕像素 vs 我方 3×3 格在 2 倍显示下。

192 屏幕像素见方对齐：赛菲莉娅 = 32 原生像素（2 个 16px 地面格）；
OCC 默认 2 倍显示 = 96 原生像素（3×3 个 32px 逻辑格）。参考仅比对用。
"""
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
STUDY = os.path.dirname(HERE)
REFS = os.path.join(STUDY, "refs")
SIZE = 32
BOX = 192


def ref_patch(name, box):
    with Image.open(os.path.join(REFS, name)) as im:
        return im.convert("RGB").crop(box)


def field3x3(name):
    with Image.open(os.path.join(HERE, name)) as im:
        tile = im.convert("RGB")
    field = Image.new("RGB", (SIZE * 3, SIZE * 3))
    for gy in range(3):
        for gx in range(3):
            field.paste(tile, (gx * SIZE, gy * SIZE))
    return field.resize((BOX, BOX), Image.NEAREST)   # 2 倍显示


def row(items, gap=10, bg=(20, 20, 24)):
    w = sum(im.width for _n, im in items) + gap * (len(items) - 1)
    hh = max(im.height for _n, im in items)
    out = Image.new("RGB", (w, hh), bg)
    x = 0
    for _n, im in items:
        out.paste(im, (x, 0))
        x += im.width + gap
    return out


def main():
    pairs = [
        ("dungeon_stone_wall.png", (0, 0, BOX, BOX), "stone_a_32.png"),
        ("field_e.png", (0, 24, BOX, BOX + 24), "dirt_a_32.png"),
        ("field_e.png", (0, 24, BOX, BOX + 24), "grass_a_32.png"),
        ("house_wood_floor.png", (0, 0, BOX, BOX), "wood_a_32.png"),
        ("dungeon_floor_rug.png", (0, 0, BOX, BOX), "carpet_a_32.png"),
    ]
    rows = [row([("ref", ref_patch(n, b)), ("mine", field3x3(m))]) for n, b, m in pairs]
    w = max(r.width for r in rows)
    hh = sum(r.height for r in rows) + 12 * (len(rows) - 1)
    out = Image.new("RGB", (w, hh), (20, 20, 24))
    y = 0
    for r in rows:
        out.paste(r, (0, y))
        y += r.height + 12
    out.save(os.path.join(HERE, "compare_at_display_scale.png"))
    print("saved compare_at_display_scale.png", out.size, "(每行：左=赛菲莉娅 1:1，右=我方 2 倍显示)")


if __name__ == "__main__":
    main()
