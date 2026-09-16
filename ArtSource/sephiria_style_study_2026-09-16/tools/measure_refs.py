# -*- coding: utf-8 -*-
"""赛菲莉娅截图量化分析：像素步长、调色板、明度与饱和度分层。

只读用户截图目录，输出派生测量数据与局部 1:1 裁切，供人工比对。
不写入 UnityProject，不作为美术资产，不构成原作素材库。
"""
import json
import os
import sys
from collections import Counter

from PIL import Image

REFS = os.path.join(os.environ["USERPROFILE"], "Videos", "NVIDIA", "Sephiria")
HERE = os.path.dirname(os.path.abspath(__file__))
STUDY = os.path.dirname(HERE)


def grid_pitch(img, thresh=0.25):
    """宏像素步长：像素画放大后，差分能量只出现在宏像素边界上，
    这些边界位置构成间距为步长的栅格。取相邻边界间距的众数。
    """
    g = img.convert("L")
    w, h = g.size
    px = g.load()
    out = {}
    for axis in ("x", "y"):
        if axis == "x":
            score = [sum(abs(px[x + 1, y] - px[x, y]) for y in range(0, h, 2)) for x in range(w - 1)]
        else:
            score = [sum(abs(px[x, y + 1] - px[x, y]) for x in range(0, w, 2)) for y in range(h - 1)]
        mx = max(score) or 1
        edges = [i for i, v in enumerate(score) if v > mx * thresh]
        diffs = [b - a for a, b in zip(edges, edges[1:]) if 2 <= b - a <= 12]
        mode = Counter(diffs).most_common(1)
        out[axis] = mode[0][0] if mode else None
        out[axis + "_top"] = Counter(diffs).most_common(4)
    return out


def top_colors(img, n=24, box=None):
    im = img.convert("RGB")
    if box:
        im = im.crop(box)
    cnt = Counter(im.getdata())
    total = sum(cnt.values())
    return [{"rgb": c, "hex": "#%02X%02X%02X" % c, "share": round(v / total, 4)}
            for c, v in cnt.most_common(n)]


def region_stats(img, box):
    im = img.convert("RGB").crop(box)
    px = list(im.getdata())
    n = len(px)
    lum = [0.2126 * r + 0.7152 * g + 0.0722 * b for r, g, b in px]
    sat = []
    for r, g, b in px:
        mx, mn = max(r, g, b), min(r, g, b)
        sat.append(0 if mx == 0 else (mx - mn) / mx)
    lum.sort()
    return {
        "mean_lum": round(sum(lum) / n, 1),
        "p05": round(lum[int(n * 0.05)], 1),
        "p50": round(lum[int(n * 0.50)], 1),
        "p95": round(lum[int(n * 0.95)], 1),
        "mean_sat": round(sum(sat) / n, 3),
        "unique_colors": len(set(px)),
    }


def main():
    names = sorted(os.listdir(REFS))
    rows = []
    for name in names:
        if not name.lower().endswith((".png", ".jpg")):
            continue
        path = os.path.join(REFS, name)
        try:
            with Image.open(path) as im:
                im = im.convert("RGB")
                info = {"file": name, "size": list(im.size)}
                info.update({"pitch": grid_pitch(im)})
                info["stats"] = region_stats(im, (0, 0, im.width, im.height))
                rows.append(info)
        except Exception as exc:  # noqa: BLE001
            rows.append({"file": name, "error": str(exc)})
        print(".", end="", flush=True)
    print()
    with open(os.path.join(STUDY, "analysis", "refs_scan.json"), "w", encoding="utf-8") as fh:
        json.dump(rows, fh, ensure_ascii=False, indent=1)
    print("scanned %d files" % len(rows))
    for r in rows[-12:]:
        p = r.get("pitch", {})
        st = r.get("stats", {})
        print("%-52s pitch x=%s y=%s  lum=%s sat=%s" %
              (r["file"][:52], p.get("x"), p.get("y"), st.get("mean_lum"), st.get("mean_sat")))


if __name__ == "__main__":
    main()
