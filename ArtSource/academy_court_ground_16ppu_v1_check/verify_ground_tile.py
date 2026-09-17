# -*- coding: utf-8 -*-
"""按合同逐条验收一块 16×16 地面瓦片。"""
import json
import os
import sys
from collections import Counter

from PIL import Image

CONTRACT = r"E:\数据库\OCC_Codex\Tools\OCCArt\occ_art_contract_v1.json"


def lum(c):
    return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]


def main(path):
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    px = im.load()
    print(f"file: {os.path.basename(path)}  size={w}x{h}")
    print(f"[规格] 交付尺寸 16x16: {'PASS' if (w, h) == (16, 16) else 'FAIL ' + str((w, h))}")

    alphas = {px[x, y][3] for y in range(h) for x in range(w)}
    hard = alphas <= {0, 255}
    print(f"[规格] 硬 Alpha（仅 0/255）: {'PASS' if hard else 'FAIL ' + str(sorted(alphas))}")

    opaque = [(x, y, px[x, y]) for y in range(h) for x in range(w) if px[x, y][3] > 0]
    colors = Counter(c[:3] for _x, _y, c in opaque)
    total = len(opaque)
    print(f"[规格] 不透明像素 {total}/{w*h}；调色板 {len(colors)} 色（上限 6）: "
          f"{'PASS' if len(colors) <= 6 else 'FAIL'}")
    for col, n in colors.most_common(6):
        print("        #%02X%02X%02X  %5.1f%%" % (col[0], col[1], col[2], 100.0 * n / total))
    dom = colors.most_common(1)[0]
    dom_pct = 100.0 * dom[1] / total
    verdict = "PASS" if dom_pct >= 45 else "FAIL"
    print("[规格] 主色占比 >=45%: " + verdict + "  ({:.1f}%  #{:02X}{:02X}{:02X})".format(
        dom_pct, dom[0][0], dom[0][1], dom[0][2]))

    isolated = 0
    for x, y, c in opaque:
        same = 0
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3] > 0 and px[nx, ny][:3] == c[:3]:
                same += 1
        if same == 0:
            isolated += 1
    print(f"[规格] 孤立单像素（四邻无同色）: {isolated} 个 "
          f"{'PASS' if isolated == 0 else 'FAIL' if isolated > 2 else 'WARN'}")

    # 边缘一圈是否被烘焙成暗框
    ring = [px[x, y][:3] for x in range(w) for y in (0, h - 1)] + [px[x, y][:3] for y in range(h) for x in (0, w - 1)]
    inner = [c[:3] for _x, _y, c in opaque if 0 < _x < w - 1 and 0 < _y < h - 1]
    ring_l = sum(lum(c) for c in ring) / len(ring)
    inner_l = sum(lum(c) for c in inner) / len(inner)
    delta = ring_l - inner_l
    print(f"[规格] 烘焙每格暗框: 外圈亮度 {ring_l:.1f} vs 内部 {inner_l:.1f}（差 {delta:+.1f}）"
          f" {'PASS（未压暗外圈）' if delta > -12 else 'FAIL（外圈明显压暗 = 烘焙边框）'}")

    # 固定左上体积明暗：整体左半/上半是否比右半/下半亮
    left = sum(lum(px[x, y][:3]) for y in range(h) for x in range(w // 2)) / (h * (w // 2))
    right = sum(lum(px[x, y][:3]) for y in range(h) for x in range(w // 2, w)) / (h * (w // 2))
    top = sum(lum(px[x, y][:3]) for y in range(h // 2) for x in range(w)) / ((h // 2) * w)
    bottom = sum(lum(px[x, y][:3]) for y in range(h // 2, h) for x in range(w)) / ((h // 2) * w)
    print(f"[规格] 固定左上体积明暗: 左-右 {left - right:+.1f} / 上-下 {top - bottom:+.1f} "
          f"{'PASS' if (left - right) >= -1 and (top - bottom) >= -1 else 'CHECK（出现右下更亮）'}")

    # 砖缝：逐行/逐列平均亮度，找明显偏暗的缝行
    rows = [sum(lum(px[x, y][:3]) for x in range(w)) / w for y in range(h)]
    cols = [sum(lum(px[x, y][:3]) for y in range(h)) / h for x in range(w)]
    rmean = sum(rows) / h
    seam_rows = [i for i, v in enumerate(rows) if v < rmean - 4]
    seam_cols = [i for i, v in enumerate(cols) if v < rmean - 4]
    print(f"[规格] 砖缝行 {seam_rows}；列 {seam_cols}")
    if len(seam_rows) >= 2:
        pitch = [seam_rows[i + 1] - seam_rows[i] for i in range(len(seam_rows) - 1)]
        print(f"[规格] 行缝间距 {pitch}（合同：砖距 4px / 缝 1px 级别）")
    # 唯一的行/列形态数 → 重复感
    uniq_rows = len({tuple(px[x, y][:3] for x in range(w)) for y in range(h)})
    uniq_cols = len({tuple(px[x, y][:3] for y in range(h)) for x in range(w)})
    print(f"[规格] 不同行形态 {uniq_rows}/{h}，不同列形态 {uniq_cols}/{w}"
          f" → {'节奏强（铺开易机械）' if min(uniq_rows, uniq_cols) <= h // 2 else '节奏较松散'}")


if __name__ == "__main__":
    main(sys.argv[1])
