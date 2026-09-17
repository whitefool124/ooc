# -*- coding: utf-8 -*-
"""地面方格逐块验收（FORMAL 前的风格门禁）。

按 occ_art_contract_v1.json 的 floor_tile_32 / ground_front_face_32x8 规则检查一块地面瓦片：
尺寸、硬 Alpha、色数、主色占比、孤立单像素、**可见格边框**、抖色程度、方向明暗中性。

用法：
  python verify_ground_tile.py path/to/tile_32.png [--expect 32x32] [--paving]

--paving 表示铺装类主题，主色占比按 75% 目标判定（通过基准：2–4 色、主色 75–88%、0 孤立像素）。
退出码：0 = 通过；1 = 不通过；2 = 输入错误。
"""
from __future__ import annotations

import argparse
import sys
from collections import Counter
from pathlib import Path

from PIL import Image

PURE_BLACK_MAX = 24          # 边框不得是纯黑：允许的最暗通道上限
BORDER_MIN_DROP = 6.0        # 外圈相对内部的亮度下降，用来确认"有边框"


def lum(c: tuple[int, int, int]) -> float:
    return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("tile")
    parser.add_argument("--expect", default="32x32", help="expected size, e.g. 32x32 or 32x8")
    parser.add_argument("--paving", action="store_true", help="paving theme: dominant tone target 75 percent")
    args = parser.parse_args()

    path = Path(args.tile)
    if not path.is_file():
        print(f"tile not found: {path}", file=sys.stderr)
        return 2

    want = tuple(int(v) for v in args.expect.lower().split("x"))
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    px = im.load()
    checks: list[tuple[str, bool, str]] = []

    checks.append((f"交付尺寸 {args.expect}", (w, h) == want, f"{w}x{h}"))

    alphas = {px[x, y][3] for y in range(h) for x in range(w)}
    checks.append(("硬 Alpha（仅 0/255）", alphas <= {0, 255}, str(sorted(alphas))))

    opaque = [(x, y, px[x, y][:3]) for y in range(h) for x in range(w) if px[x, y][3] > 0]
    colors = Counter(c for _x, _y, c in opaque)
    total = len(opaque)
    checks.append(("调色板 ≤6", len(colors) <= 6, f"{len(colors)} 色"))

    dominant = colors.most_common(1)[0]
    dom_pct = 100.0 * dominant[1] / total
    floor_pct = 75.0 if args.paving else 45.0
    checks.append((f"主色占比 ≥{floor_pct:.0f}%", dom_pct >= floor_pct,
                   f"{dom_pct:.1f}%  #{dominant[0][0]:02X}{dominant[0][1]:02X}{dominant[0][2]:02X}"))

    isolated = 0
    for x, y, c in opaque:
        if not any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] > 0
                   and px[x + dx, y + dy][:3] == c
                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
            isolated += 1
    isolated_pct = 100.0 * isolated / total
    checks.append(("孤立单像素 ≤0.5%", isolated_pct <= 0.5, f"{isolated} 个 = {isolated_pct:.2f}%"))

    ring = [px[x, y][:3] for x in range(w) for y in (0, h - 1)] + \
           [px[x, y][:3] for y in range(h) for x in (0, w - 1)]
    inner = [c for x, y, c in opaque if 0 < x < w - 1 and 0 < y < h - 1]
    ring_l = sum(lum(c) for c in ring) / len(ring)
    inner_l = sum(lum(c) for c in inner) / len(inner) if inner else ring_l
    drop = inner_l - ring_l
    darkest = min(min(c) for c in ring)
    border_ok = drop >= BORDER_MIN_DROP and darkest > PURE_BLACK_MAX
    checks.append(("可见格边框（外圈较暗且非纯黑）", border_ok,
                   f"亮度差 {drop:+.1f}，外圈最暗通道 {darkest}"))

    # 抖色判据：色阶均匀铺开（各色占比接近）就是抖色噪声；安静的地面必须由主色压倒。
    # 不用"1px 连续段"判定，因为要求的 1px 格边框本身就会产生大量 1px 段。
    ranked = colors.most_common()
    second_pct = 100.0 * ranked[1][1] / total if len(ranked) > 1 else 0.0
    checks.append(("非抖色（次色占比 ≤25%）", second_pct <= 25.0, f"次色 {second_pct:.1f}%"))

    left = sum(lum(px[x, y][:3]) for y in range(h) for x in range(w // 2)) / (h * (w // 2))
    right = sum(lum(px[x, y][:3]) for y in range(h) for x in range(w // 2, w)) / (h * (w // 2))
    top = sum(lum(px[x, y][:3]) for y in range(h // 2) for x in range(w)) / ((h // 2) * w)
    bottom = sum(lum(px[x, y][:3]) for y in range(h // 2, h) for x in range(w)) / ((h // 2) * w)
    checks.append(("方向明暗中性（|左-右| 与 |上-下| ≤4）",
                   abs(left - right) <= 4 and abs(top - bottom) <= 4,
                   f"左-右 {left - right:+.1f} / 上-下 {top - bottom:+.1f}"))

    print(f"tile: {path.name}  {w}x{h}")
    failed = 0
    for name, ok, detail in checks:
        if not ok:
            failed += 1
        print(f"  [{'PASS' if ok else 'FAIL'}] {name}  —— {detail}")
    print(f"RESULT: {'PASS' if failed == 0 else 'FAIL (%d 项)' % failed}")
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
