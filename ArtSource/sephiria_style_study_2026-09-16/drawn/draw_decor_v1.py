# -*- coding: utf-8 -*-
"""地面装饰物：草丛（透明叠加层，32×32，手写像素）。

草地面格是平色，草丛按参考的做法作为独立装饰物存在，不画进地面格。
角色定位＝terrain decoration overlay（32×32、透明、≤6 色、留 2px 安全边）。
"""
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SIZE = 32

PAL = {
    "s": (52, 96, 66),     # seam  接触暗色
    "d": (64, 120, 78),    # shade 草体
    "l": (112, 188, 122),  # light 受光叶
    "h": (136, 204, 142),  # hi    叶尖
}

# 三丛草：左下压暗、左上受光，底部 1px 接触影。
# 尺寸按实机显示对准：约 14 原生像素宽 = 28 屏幕像素，读作草丛而不是小点。
TUFTS = {
    "a": [
        "..............",
        "....h....h....",
        "...hl..dl.....",
        "..hld..dld....",
        "..hldd.ddld...",
        ".sldddddddl...",
        "slddddddddds..",
        "sddddddddddds.",
        ".sddddddddds..",
        "..sssssssss...",
    ],
    "b": [
        "..............",
        "..h......h....",
        ".hl.h...dl....",
        ".hldlh..dld...",
        "sldddld.dddl..",
        "sdddddd.dddds.",
        "sddddddddddds.",
        ".sddddddddds..",
        "..sddddddds...",
        "...sssssss....",
    ],
    "c": [
        "..............",
        "...h...h...h..",
        "..hl..dl..hl..",
        ".hldl.dld.hl..",
        ".hlddlddlddl..",
        "sldddddddddds.",
        "sddddddddddds.",
        "sddddddddddds.",
        ".sddddddddds..",
        "..sssssssss...",
    ],
}


def build(name, origin=(0, 0)):
    rows = TUFTS[name]
    im = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    px = im.load()
    ox, oy = origin
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch == ".":
                continue
            px[ox + x, oy + y] = PAL[ch] + (255,)
    return im


def main():
    made = []
    for i, name in enumerate(("a", "b", "c")):
        im = build(name, (9, 20 - i))
        im.save(os.path.join(HERE, "decor_grass_tuft_%s_32.png" % name))
        made.append((name, im))
    # 图鉴：放在草地色底上看读法
    scale, gap = 6, 10
    ground = Image.new("RGB", (SIZE * len(made), SIZE), (90, 168, 104))
    for i, (_n, im) in enumerate(made):
        ground.paste(im, (i * SIZE, 0), im)
    ground.resize((ground.width * scale, ground.height * scale), Image.NEAREST).save(
        os.path.join(HERE, "contact_decor_6x.png"))
    for name, im in made:
        cols = {c for c in im.getdata() if c[3] > 0}
        print("decor_grass_tuft_%s_32.png  colors=%d  bbox=%s" % (name, len(cols), im.getbbox()))


if __name__ == "__main__":
    main()
