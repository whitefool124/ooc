# -*- coding: utf-8 -*-
"""读取赛菲莉亚的 Unity 资源，学习美术规格（贴图名 / 真实像素尺寸 / 光照贴图）。

只做规格测量与少量样本导出到 ArtSource 学习目录，不复制进 OCC 正式美术。
"""
import json
import os
import re
import sys

import UnityPy

GAME = r"E:\SteamLibrary\steamapps\common\Sephiria\Sephiria_Data"
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "sephiria_extract")
os.makedirs(OUT, exist_ok=True)
KEYS = ("tile", "ground", "wall", "cliff", "floor", "light", "lamp", "torch", "gradient",
        "noise", "shadow", "roof", "carpet", "grass", "dirt", "stone", "wood", "atlas")
FILES = ["resources.assets", "sharedassets0.assets", "sharedassets1.assets", "sharedassets2.assets",
         "globalgamemanagers.assets"]


def main():
    report = {}
    for fn in FILES:
        path = os.path.join(GAME, fn)
        if not os.path.exists(path):
            continue
        env = UnityPy.load(path)
        tex = []
        for obj in env.objects:
            if obj.type.name != "Texture2D":
                continue
            try:
                d = obj.read()
            except Exception:
                continue
            name = d.m_Name
            tex.append({"name": name, "w": d.m_Width, "h": d.m_Height, "fmt": str(d.m_TextureFormat)})
        report[fn] = {"texture_count": len(tex), "textures": tex}
        hit = [t for t in tex if any(k in t["name"].lower() for k in KEYS)]
        print("== %s: %d textures, %d keyword hits" % (fn, len(tex), len(hit)))
        for t in sorted(hit, key=lambda x: -(x["w"] * x["h"]))[:25]:
            print("   %-44s %4dx%-4d %s" % (t["name"][:44], t["w"], t["h"], t["fmt"]))
    with open(os.path.join(OUT, "textures_index.json"), "w", encoding="utf-8") as fh:
        json.dump(report, fh, ensure_ascii=False, indent=1)

    # 尺寸直方图：像素画的真实瓦片尺寸
    hist = {}
    for fn, r in report.items():
        for t in r["textures"]:
            hist[(t["w"], t["h"])] = hist.get((t["w"], t["h"]), 0) + 1
    print("\n== 尺寸分布（前 15）==")
    for (w, h), n in sorted(hist.items(), key=lambda kv: -kv[1])[:15]:
        print("   %4dx%-4d  %d 张" % (w, h, n))


if __name__ == "__main__":
    main()
