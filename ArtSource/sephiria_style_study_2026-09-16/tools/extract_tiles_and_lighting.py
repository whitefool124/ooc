# -*- coding: utf-8 -*-
"""导出瓦片/墙/影子/光效样本，并抽取光照脚本的字段名，推断实现细节。"""
import os
import re

import UnityPy

GAME = r"E:\SteamLibrary\steamapps\common\Sephiria\Sephiria_Data"
STUDY = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(STUDY, "sephiria_extract", "tiles")
os.makedirs(OUT, exist_ok=True)

WANT_SPRITES = [
    "CaveWall_0", "CaveWall_4", "CaveWall_7", "CaveFloor_10", "CaveFloor_19", "CaveFloor_32",
    "TownCliffTile_6", "TownCliffTile_15", "JailGround0_10", "JailGround0_1",
    "Cliff_GroundShadow_R_L_C", "Cliff_GroundShadow_Bay", "Qliphoth_Shadow_Tile_0",
    "CostumeTile0_17", "CaveTileset_25",
]


def export_sprites():
    env = UnityPy.load(os.path.join(GAME, "sharedassets0.assets"))
    sprites = {}
    for obj in env.objects:
        if obj.type.name != "Sprite":
            continue
        try:
            d = obj.read()
        except Exception:
            continue
        if d.m_Name in WANT_SPRITES:
            sprites[d.m_Name] = d
    for name, d in sprites.items():
        try:
            img = d.image
            img.save(os.path.join(OUT, "%s_%dx%d.png" % (name, img.width, img.height)))
            print("exported %-28s %dx%d" % (name, img.width, img.height))
        except Exception as exc:  # noqa: BLE001
            print("skip", name, exc)


def lighting_fields():
    p = os.path.join(GAME, "Managed", "Assembly-CSharp.dll")
    b = open(p, "rb").read()
    strings = [s.decode("latin1") for s in re.findall(rb"[\x20-\x7e]{3,}", b)]
    joined = "\n".join(strings)
    for cls in ("WavingLight2D", "DayCycle", "CharacterLight", "CharacterSpriteLight",
                "SetCustomLightAmbient", "LightAnimator", "SirenLight", "LensFlareTrigger"):
        idx = [i for i, s in enumerate(strings) if s.strip() == cls]
        print("\n== %s ==" % cls)
        for i in idx[:1]:
            window = strings[max(0, i - 2):i + 40]
            print("   ", " | ".join(w for w in window if w.strip())[:600])
        if not idx:
            hits = [s for s in strings if cls in s]
            print("   (类名未单独出现) 相关串：", " | ".join(hits[:6])[:400])


if __name__ == "__main__":
    export_sprites()
    lighting_fields()
