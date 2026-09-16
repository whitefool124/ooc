# -*- coding: utf-8 -*-
"""读赛菲莉亚的 Sprite 元数据（PPU / 切图矩形）与关键贴图，用于确定真实美术规格。

PPU（pixels per unit）直接决定"一个像素代表多长世界"，是比例尺问题的权威答案。
"""
import json
import os

import UnityPy

GAME = r"E:\SteamLibrary\steamapps\common\Sephiria\Sephiria_Data"
STUDY = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(STUDY, "sephiria_extract")
os.makedirs(OUT, exist_ok=True)
FILES = ["sharedassets0.assets", "resources.assets", "sharedassets2.assets"]


def main():
    rows = []
    for fn in FILES:
        path = os.path.join(GAME, fn)
        env = UnityPy.load(path)
        for obj in env.objects:
            if obj.type.name != "Sprite":
                continue
            try:
                d = obj.read()
            except Exception:
                continue
            r = d.m_Rect
            rows.append({
                "file": fn,
                "name": d.m_Name,
                "w": round(r.width, 1),
                "h": round(r.height, 1),
                "ppu": d.m_PixelsToUnits,
                "pivot": [round(d.m_Pivot.x, 3), round(d.m_Pivot.y, 3)],
            })
    with open(os.path.join(OUT, "sprites_index.json"), "w", encoding="utf-8") as fh:
        json.dump(rows, fh, ensure_ascii=False, indent=1)

    ppu_hist = {}
    for r in rows:
        ppu_hist[r["ppu"]] = ppu_hist.get(r["ppu"], 0) + 1
    print("sprite 总数:", len(rows))
    print("PPU 分布:", dict(sorted(ppu_hist.items(), key=lambda kv: -kv[1])))

    # 瓦片类：按尺寸找 16/24/32 的方块
    tiles = [r for r in rows if abs(r["w"] - r["h"]) < 0.6 and r["w"] in (8, 12, 16, 24, 32, 48, 64)]
    print("\n方形 sprite（可能是瓦片）前 20：")
    for r in sorted(tiles, key=lambda x: (x["w"], x["name"]))[:20]:
        print("   %-40s %3.0fx%-3.0f ppu=%s" % (r["name"][:40], r["w"], r["h"], r["ppu"]))

    # 关键贴图导出
    keep = ["Tilemaps", "PlayerHouseInterior_Ground", "PlayerHouseInterior_Wall", "Lib_NextStage_Wall",
            "ComboLight10", "PotLight_R00", "sactx-0-2048x2048-Uncompressed-Tilemaps"]
    for fn in FILES:
        env = UnityPy.load(os.path.join(GAME, fn))
        for obj in env.objects:
            if obj.type.name != "Texture2D":
                continue
            try:
                d = obj.read()
            except Exception:
                continue
            if not any(k.lower() in d.m_Name.lower() for k in keep):
                continue
            safe = "".join(c if c.isalnum() or c in "-_." else "_" for c in d.m_Name)
            p = os.path.join(OUT, "%s_%dx%d.png" % (safe[:48], d.m_Width, d.m_Height))
            if os.path.exists(p):
                continue
            try:
                d.image.save(p)
                print("exported", os.path.basename(p))
            except Exception as exc:  # noqa: BLE001
                print("skip", d.m_Name, exc)


if __name__ == "__main__":
    main()
