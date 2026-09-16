# -*- coding: utf-8 -*-
"""物件层样板：按赛道菲莉娅的物件语言画（顶面 + 前沿 + 1 原生像素黑描边 + 高饱和）。

规则来源（记录见 analysis/赛菲莉娅语言提炼_2026-09-16.md）：
- 小型物件＝一个主剪影 + 一个识别特征，不堆徽记/铆钉/多层框架。
- 透明物件用 1 原生像素纯黑外轮廓与场地分离；内部材质缝用同材质深色。
- 可见顶面 + 朝屏幕下方的前沿／立面表达厚度。
- 画布 32×32、至少 1px 安全边；角色定位 single_cell_prop_32（palette_max 10）。

状态：候选原料（ArtSource 阶段）。不进 UnityProject，不登记 manifest。
"""
import json
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SIZE = 32

# 木箱：木色 4 + 金属 2 + 描边 1 = 7 色
CRATE = {
    "k": (18, 14, 20),      # 外轮廓（纯黑系）
    "d": (110, 66, 44),     # 木深（缝）
    "b": (155, 103, 68),    # 木中
    "f": (185, 137, 96),    # 木面
    "l": (206, 164, 124),   # 木受光（顶面）
    "m": (128, 126, 136),   # 金属带
    "n": (78, 76, 86),      # 金属带暗边
}

# 石堆：石色 4 + 描边 1 = 5 色
RUBBLE = {
    "k": (18, 14, 20),
    "d": (58, 58, 66),
    "b": (86, 86, 96),
    "f": (116, 116, 127),
    "l": (146, 150, 156),
}


def blank():
    return Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))


def crate():
    """木箱：顶面 7px + 前沿 16px + 两道金属带，底部贴地。"""
    im = blank()
    px = im.load()
    L, R = 6, 25          # 前沿左右
    TL, TR = 8, 23        # 顶面左右（略窄，形成厚度）
    TOP0, TOP1 = 5, 11    # 顶面
    FRONT0, FRONT1 = 12, 27
    for x in range(TL, TR + 1):
        for y in range(TOP0, TOP1 + 1):
            px[x, y] = CRATE["l"] + (255,)
    for x in range(TL, TR + 1):
        px[x, TOP0] = CRATE["f"] + (255,)
    for x in range(L, R + 1):
        for y in range(FRONT0, FRONT1 + 1):
            px[x, y] = CRATE["f"] + (255,)
    # 前沿板缝：每 6px 一道同材质深色
    for x in range(L + 6, R, 6):
        for y in range(FRONT0, FRONT1 + 1):
            px[x, y] = CRATE["b"] + (255,)
    # 金属带两道
    for y0 in (15, 22):
        for x in range(L, R + 1):
            for y in (y0, y0 + 1):
                px[x, y] = CRATE["m"] + (255,)
            px[x, y0 + 2] = CRATE["n"] + (255,)
    # 右侧与底部压暗
    for y in range(FRONT0, FRONT1 + 1):
        px[R, y] = CRATE["b"] + (255,)
        px[R - 1, y] = CRATE["f"] + (255,)
    for x in range(L, R + 1):
        px[x, FRONT1] = CRATE["d"] + (255,)
    # 外轮廓：最外一圈不透明像素换成纯黑系
    return outline(im)


def _stone(px, x0, y0, w, h):
    """一块圆角石头：顶部两行受光、底部压暗、右侧同材质深色。"""
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            dx = min(x - x0, x0 + w - 1 - x)
            dy = min(y - y0, y0 + h - 1 - y)
            if dx == 0 and dy == 0:
                continue
            if y < y0 + 2:
                tone = "l"
            elif y >= y0 + h - 2:
                tone = "d"
            elif x >= x0 + w - 2:
                tone = "b"
            else:
                tone = "f"
            px[x, y] = RUBBLE[tone] + (255,)


def rubble():
    """石堆：后排一块 + 前排两块，低矮堆叠，顶面受光。"""
    im = blank()
    px = im.load()
    _stone(px, 12, 10, 9, 8)    # 后排
    _stone(px, 7, 14, 10, 10)   # 左下
    _stone(px, 17, 15, 8, 9)    # 右下
    return outline(im, RUBBLE)


def outline(im, pal=None):
    """把最外一圈不透明像素替换为描边色（不扩张 Alpha、不加第二圈）。"""
    pal = pal or CRATE
    src = im.copy()
    px_src = src.load()
    px = im.load()
    for y in range(SIZE):
        for x in range(SIZE):
            if px_src[x, y][3] == 0:
                continue
            edge = False
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if nx < 0 or ny < 0 or nx >= SIZE or ny >= SIZE or px_src[nx, ny][3] == 0:
                    edge = True
            if edge:
                px[x, y] = pal["k"] + (255,)
    return im


def main():
    made = {}
    for name, im in (("crate", crate()), ("rubble", rubble())):
        path = "prop_%s_32.png" % name
        im.save(os.path.join(HERE, path))
        cols = {c[:3] for c in im.getdata() if c[3] > 0}
        made[path] = {"role": "single_cell_prop_32", "colors": len(cols), "bbox": list(im.getbbox())}
        print("%-18s colors=%d bbox=%s" % (path, len(cols), im.getbbox()))

    # 图鉴：放在草地色底上看剪影
    scale, gap = 8, 10
    ground = Image.new("RGB", (SIZE * 2, SIZE), (90, 168, 104))
    for i, name in enumerate(("crate", "rubble")):
        with Image.open(os.path.join(HERE, "prop_%s_32.png" % name)) as im:
            ground.paste(im, (i * SIZE, 0), im)
    ground.resize((ground.width * scale, ground.height * scale), Image.NEAREST).save(
        os.path.join(HERE, "contact_props_8x.png"))
    with open(os.path.join(HERE, "props_report_v1.json"), "w", encoding="utf-8") as fh:
        json.dump(made, fh, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
