# -*- coding: utf-8 -*-
"""实机像素回归：判断截图是否为整数倍放大、倍率是多少、栅格是否对齐。

判据：
- 正确倍率 p 下，战场上几乎所有 p×p 区块内部颜色完全一致（块内方差 = 0）。
- 错误的 p 会让区块跨越真实像素边界，出现大量不一致区块。
- 同时检查栅格偏移：真实像素边界应落在图像坐标 0, p, 2p...（画布从原点绘制）。
"""
import sys

from PIL import Image

REGIONS = {
    "战场左上（地面+掩体）": (20, 90, 700, 400),
    "战场左下（地面）": (20, 600, 700, 810),
    "战场中部": (400, 200, 1000, 600),
    "右侧 HUD": (1480, 90, 1900, 820),
}


def uniform_fraction(img, box, pitch, offset=(0, 0)):
    x0, y0, x1, y1 = box
    px = img.load()
    total = same = 0
    ox, oy = offset
    y = y0 + ((oy - y0) % pitch)
    while y + pitch <= y1:
        x = x0 + ((ox - x0) % pitch)
        while x + pitch <= x1:
            first = px[x, y]
            ok = True
            for dy in range(pitch):
                for dx in range(pitch):
                    if px[x + dx, y + dy] != first:
                        ok = False
                        break
                if not ok:
                    break
            total += 1
            if ok:
                same += 1
            x += pitch
        y += pitch
    return (same / total) if total else 0.0, total


def main():
    path = sys.argv[1]
    img = Image.open(path).convert("RGB")
    print("image:", img.size)

    print("\n== 各候选倍率下的区块一致率（越接近 1.0 越是真实倍率） ==")
    for name, box in REGIONS.items():
        row = []
        for p in (2, 3, 4, 5, 6, 8):
            f, n = uniform_fraction(img, box, p)
            row.append("%dx:%5.1f%%" % (p, f * 100))
        print("  %-22s %s" % (name, "  ".join(row)))

    print("\n== 倍率 6 的栅格偏移扫描（战场中部） ==")
    best = None
    for oy in range(6):
        for ox in range(6):
            f, _ = uniform_fraction(img, REGIONS["战场中部"], 6, (ox, oy))
            if best is None or f > best[0]:
                best = (f, ox, oy)
    print("  最佳偏移 = (%d, %d)，一致率 %.1f%%" % (best[1], best[2], best[0] * 100))
    if best[1] == 0 and best[2] == 0:
        print("  → 栅格与图像原点对齐（画布从 (0,0) 整数倍绘制）")
    else:
        print("  → 栅格相对图像原点偏移了 (%d,%d) 像素，说明存在非整数位移或画布原点未对齐" % (best[1], best[2]))


if __name__ == "__main__":
    main()
