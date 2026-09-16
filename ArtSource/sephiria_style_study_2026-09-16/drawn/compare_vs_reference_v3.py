# -*- coding: utf-8 -*-
"""参考原生像素与 v3 画法并排（同倍率）。参考像素仅比对用，不进 Unity。"""
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
STUDY = os.path.dirname(HERE)
REFS = os.path.join(STUDY, "refs")
SCALE = 6


def ref_patch(name, box):
    with Image.open(os.path.join(REFS, "native_%s.png" % name)) as im:
        patch = im.convert("RGB").crop(box)
    return patch.resize((patch.width * SCALE, patch.height * SCALE), Image.NEAREST)


def drawn(name):
    with Image.open(os.path.join(HERE, name)) as im:
        im = im.convert("RGB")
    return im.resize((im.width * SCALE, im.height * SCALE), Image.NEAREST)


def row(items, gap=12, bg=(20, 20, 24)):
    w = sum(im.width for _n, im in items) + gap * (len(items) - 1)
    h = max(im.height for _n, im in items)
    out = Image.new("RGB", (w, h), bg)
    x = 0
    for _n, im in items:
        out.paste(im, (x, 0))
        x += im.width + gap
    return out


def main():
    rows = [
        row([("ref_dirt", ref_patch("field_e", (8, 2, 40, 34))),
             ("mine_dirt_a", drawn("dirt_a_32.png")),
             ("mine_dirt_b", drawn("dirt_b_32.png"))]),
        row([("ref_grass", ref_patch("field_e", (0, 20, 32, 52))),
             ("mine_grass_a", drawn("grass_a_32.png")),
             ("mine_grass_b", drawn("grass_b_32.png"))]),
        row([("ref_stone", ref_patch("dungeon_stone_wall", (0, 8, 32, 40))),
             ("mine_stone_a", drawn("stone_a_32.png")),
             ("mine_stone_b", drawn("stone_b_32.png"))]),
        row([("ref_wood", ref_patch("house_wood_floor", (4, 6, 36, 38))),
             ("mine_wood_a", drawn("wood_a_32.png")),
             ("mine_wood_b", drawn("wood_b_32.png"))]),
        row([("ref_carpet", ref_patch("dungeon_floor_rug", (4, 10, 36, 42))),
             ("mine_carpet_a", drawn("carpet_a_32.png")),
             ("mine_carpet_b", drawn("carpet_b_32.png"))]),
    ]
    w = max(r.width for r in rows)
    h = sum(r.height for r in rows) + 12 * (len(rows) - 1)
    out = Image.new("RGB", (w, h), (20, 20, 24))
    y = 0
    for r in rows:
        out.paste(r, (0, y))
        y += r.height + 12
    out.save(os.path.join(HERE, "compare_vs_reference_6x.png"))
    print("saved compare_vs_reference_6x.png", out.size)


if __name__ == "__main__":
    main()
