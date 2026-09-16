# -*- coding: utf-8 -*-
"""把赛菲莉娅截图还原到其原生像素网格，提取材质配方。

游戏以固定步长整数放大（检测为 6 屏幕像素 = 1 原生像素）。这里先找相位，
再按相位取样，得到真正的原生像素图与色表。仅用于观察学习。
"""
import json
import os
import sys
from collections import Counter

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
STUDY = os.path.dirname(HERE)
REFS = os.path.join(STUDY, "refs")
OUT = os.path.join(STUDY, "refs")
PITCH = 6


def best_phase(im, pitch=PITCH):
    """找宏像素栅格相位：块内方差最小的偏移。"""
    g = im.convert("L")
    w, h = g.size
    px = g.load()
    best, best_score = (0, 0), None
    for oy in range(pitch):
        for ox in range(pitch):
            s = 0
            n = 0
            for by in range(oy, h - pitch, pitch * 3):
                for bx in range(ox, w - pitch, pitch * 3):
                    vals = [px[bx + i, by + j] for i in range(pitch) for j in range(pitch)]
                    m = sum(vals) / len(vals)
                    s += sum((v - m) ** 2 for v in vals)
                    n += len(vals)
            if best_score is None or s / n < best_score:
                best_score, best = s / n, (ox, oy)
    return best, round(best_score or 0, 2)


def downsample(im, phase):
    ox, oy = phase
    w = (im.width - ox) // PITCH
    h = (im.height - oy) // PITCH
    out = Image.new("RGB", (w, h))
    px_in = im.convert("RGB").load()
    px_out = out.load()
    c = PITCH // 2
    for y in range(h):
        for x in range(w):
            px_out[x, y] = px_in[ox + x * PITCH + c, oy + y * PITCH + c]
    return out


def main():
    names = sorted(f for f in os.listdir(REFS) if f.endswith(".png") and not f.startswith("native"))
    report = {}
    for name in names:
        label = name[:-4]
        with Image.open(os.path.join(REFS, name)) as im:
            im = im.convert("RGB")
            phase, score = best_phase(im)
            nat = downsample(im, phase)
        nat.save(os.path.join(OUT, "native_%s.png" % label))
        nat.resize((nat.width * 4, nat.height * 4), Image.NEAREST).save(
            os.path.join(OUT, "native4x_%s.png" % label))
        cnt = Counter(nat.getdata())
        report[label] = {
            "phase": list(phase),
            "phase_var": score,
            "native_size": list(nat.size),
            "native_colors": len(cnt),
            "top_colors": [{"hex": "#%02X%02X%02X" % c, "n": v} for c, v in cnt.most_common(16)],
        }
        print("%-22s phase=%s var=%-6s native=%sx%s colors=%d" %
              (label, phase, score, nat.width, nat.height, len(cnt)))
    with open(os.path.join(STUDY, "analysis", "native_grid.json"), "w", encoding="utf-8") as fh:
        json.dump(report, fh, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
