# -*- coding: utf-8 -*-
"""把参考原生像素与我方画法并排：同一放大倍率下比较材质语言。

参考像素仅用于比对，不进入 Unity、不作为资产。
"""
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
STUDY = os.path.dirname(HERE)
REFS = os.path.join(STUDY, "refs")
SCALE = 6


def ref_patch(name, box, scale=SCALE):
    with Image.open(os.path.join(REFS, "native_%s.png" % name)) as im:
        patch = im.convert("RGB").crop(box)
    return patch.resize((patch.width * scale, patch.height * scale), Image.NEAREST)


def drawn(name, scale=SCALE):
    with Image.open(os.path.join(HERE, name)) as im:
        im = im.convert("RGB")
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST)


def labeled_row(items, gap=12, bg=(20, 20, 24)):
    w = sum(im.width for _n, im in items) + gap * (len(items) - 1)
    h = max(im.height for _n, im in items)
    out = Image.new("RGB", (w, h), bg)
    x = 0
    for _n, im in items:
        out.paste(im, (x, 0))
        x += im.width + gap
    return out


def main():
    rows = []
    # 泥土：参考土坡 vs 我方泥土（密 / 疏两种口径）
    rows.append(labeled_row([
        ("ref_dirt", ref_patch("field_e", (8, 2, 40, 34))),
        ("mine_dense", drawn("dirt_a_32.png")),
        ("mine_sparse", drawn("dirt_sparse_contract_32.png")),
    ]))
    # 石砖：参考墙面 vs 我方石砖
    rows.append(labeled_row([
        ("ref_stone", ref_patch("dungeon_stone_wall", (0, 8, 32, 40))),
        ("mine_stone", drawn("stone_a_32.png")),
        ("mine_stone_b", drawn("stone_b_32.png")),
    ]))
    # 木地板：参考结构（层距 4px）vs 我方石砖（层距 8px = 2 倍换算）
    rows.append(labeled_row([
        ("ref_wood", ref_patch("house_wood_floor", (4, 6, 36, 38))),
        ("mine_outer_dirt", drawn("dirt_outer_s_32x40.png")),
        ("mine_outer_stone", drawn("stone_outer_s_32x40.png")),
    ]))
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
