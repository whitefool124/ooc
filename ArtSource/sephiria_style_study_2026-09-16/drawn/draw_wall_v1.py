# -*- coding: utf-8 -*-
"""墙体首样：直墙 + 墙角（按"角色比例尺"画，特征 = 赛菲莉娅原生 ×4）。

比例推导（详见 analysis/赛菲莉娅语言提炼_2026-09-16.md）：
- 赛菲莉娅：角色约 16 原生像素高，一格约 16px，砖 4px（= 角色的 1/4），缝 1px。
- OCC：角色 32×64（64px 高）→ 同样比例下砖 16px、缝 4px、墙顶带与前沿按同比例。
- 逻辑格仍是 32×32，因此一格只放 2 块砖；墙的水平接缝落在格边界上，可拼。

角色定位 modular_structure_32：交付 = 逻辑格数 × 32，palette_max 12。
状态：候选原料（ArtSource 阶段）。不进 UnityProject，不登记 manifest。
"""
import json
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
CELL = 32

WALL = {
    "k": (20, 18, 24),      # 外轮廓
    "n": (48, 48, 56),      # 最暗：顶面与前沿之间的深缝、墙根
    "s": (66, 66, 74),      # 砖缝
    "db": (86, 88, 96),     # 砖块下沿
    "bf": (104, 106, 114),  # 砖块主体（前沿）
    "bl": (122, 124, 132),  # 砖块上沿受光
    "tl": (150, 152, 160),  # 顶面主体
    "th": (176, 180, 186),  # 顶面高光边
}

TOP_H = 14       # 顶面高度（含受光边）
PITCH = 16       # 砖距（角色比例尺：赛菲莉娅 4px × 4）
SEAM = 4         # 结构缝
COURSE = 8       # 砖层高


def wall_cell(px, ox, oy, west_open, east_open, south_open):
    """一格墙：上部顶面（明显更亮）+ 下部前沿（砖层）+ 端面处理。"""
    for y in range(CELL):
        for x in range(CELL):
            X, Y = ox + x, oy + y
            if y < TOP_H:
                if y < 2:
                    tone = "th"
                elif y == TOP_H - 1:
                    tone = "n"
                elif x % PITCH < SEAM and x > 0:
                    tone = "db"
                else:
                    tone = "tl"
            else:
                rel = y - (TOP_H + 2)
                course = max(0, rel) // COURSE
                bond = (course % 2) * (PITCH // 2)      # 错缝：隔层错半砖
                if y < TOP_H + 2 or y >= CELL - 2:
                    tone = "n"
                elif (x + bond) % PITCH < SEAM and x > 0:
                    tone = "s"
                elif rel < 0:
                    tone = "n"
                elif rel % COURSE == 0:
                    tone = "bl"
                elif rel % COURSE >= COURSE - 2:
                    tone = "db"
                else:
                    tone = "bf"
            px[X, Y] = WALL[tone] + (255,)
    # 端面：西侧受光（左上光源），东侧压暗
    if west_open:
        for y in range(TOP_H, CELL):
            for x in (0, 1):
                px[ox + x, oy + y] = WALL["bl" if y < CELL - 3 else "n"] + (255,)
    if east_open and not west_open:
        for y in range(TOP_H, CELL):
            px[ox + CELL - 1, oy + y] = WALL["n"] + (255,)


def outline(im):
    """最外一圈不透明像素换成轮廓色（不扩张 Alpha）。"""
    src = im.copy()
    a = src.load()
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            if a[x, y][3] == 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if nx < 0 or ny < 0 or nx >= im.width or ny >= im.height or a[nx, ny][3] == 0:
                    px[x, y] = WALL["k"] + (255,)
                    break
    return im


def build(cells, w_cells, h_cells, shadow=True, floor_name="stone_a_32.png", with_floor=False):
    """cells: 墙格集合（格坐标）；w/h_cells: 画布格数。"""
    im = Image.new("RGBA", (w_cells * CELL, h_cells * CELL), (0, 0, 0, 0))
    px = im.load()
    if with_floor:
        with Image.open(os.path.join(HERE, floor_name)) as fl:
            tile = fl.convert("RGBA")
        for gy in range(h_cells):
            for gx in range(w_cells):
                im.alpha_composite(tile, (gx * CELL, gy * CELL))
    wall_cells = set(cells)
    for (gx, gy) in sorted(wall_cells):
        west_open = (gx - 1, gy) not in wall_cells
        east_open = (gx + 1, gy) not in wall_cells
        south_open = (gx, gy + 1) not in wall_cells
        wall_cell(px, gx * CELL, gy * CELL, west_open, east_open, south_open)
    if shadow:
        # 墙脚投影：落在南侧空格上，2px 暗带 + 1px 更深
        for (gx, gy) in wall_cells:
            if (gx, gy + 1) in wall_cells or gy + 1 >= h_cells:
                continue
            for x in range(gx * CELL + 2, gx * CELL + CELL - 2):
                for y in range((gy + 1) * CELL, (gy + 1) * CELL + 3):
                    if y < im.height:
                        r, g, b, a = px[x, y]
                        if a == 0:
                            continue
                        k = 0.55 if y < (gy + 1) * CELL + 2 else 0.75
                        px[x, y] = (int(r * k), int(g * k), int(b * k), a)
    return outline(im)


def main():
    out = {}
    # 直墙：2 格宽 × 1 格深
    straight = build({(0, 0), (1, 0)}, 2, 1, shadow=False)
    straight.save(os.path.join(HERE, "wall_straight_2x1_64.png"))
    out["wall_straight_2x1_64.png"] = {"role": "modular_structure_32", "cells": [2, 1]}

    # 墙角（L）：北墙两格 + 东墙一格，内角在左下
    corner = build({(0, 0), (1, 0), (1, 1)}, 2, 2)
    corner.save(os.path.join(HERE, "wall_corner_l_2x2_64.png"))
    out["wall_corner_l_2x2_64.png"] = {"role": "modular_structure_32", "cells": [2, 2]}

    # 落地接触：墙角压在同色石砖地面上
    corner_ground = build({(0, 0), (1, 0), (1, 1)}, 2, 2, with_floor=True)
    corner_ground.save(os.path.join(HERE, "wall_corner_l_2x2_64_on_floor.png"))

    # 图鉴
    scale, gap = 6, 16
    items = [straight, corner, corner_ground]
    w = sum(i.width for i in items) * scale + gap * (len(items) - 1)
    h = max(i.height for i in items) * scale
    sheet = Image.new("RGB", (w, h), (20, 20, 24))
    x = 0
    for i in items:
        sheet.paste(i.resize((i.width * scale, i.height * scale), Image.NEAREST), (x, 0))
        x += i.width * scale + gap
    sheet.save(os.path.join(HERE, "contact_wall_6x.png"))

    for name in out:
        with Image.open(os.path.join(HERE, name)) as im:
            cols = {c[:3] for c in im.getdata() if c[3] > 0}
            out[name]["colors"] = len(cols)
            print("%-30s cells=%s colors=%d" % (name, out[name]["cells"], len(cols)))
    with open(os.path.join(HERE, "wall_report_v1.json"), "w", encoding="utf-8") as fh:
        json.dump(out, fh, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
