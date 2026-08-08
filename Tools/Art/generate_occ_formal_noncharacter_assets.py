#!/usr/bin/env python3
"""Generate OCC M-A3 formal non-character pixel assets.

The generator is intentionally limited to environment tiles, UI symbols, item
silhouettes and other non-character assets. It never authors units or character
animation. Every output is deterministic, uses ART-BASE-01 colors only, and is
machine-audited before it can be copied into Unity Resources.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


P = {
    "clear": (0, 0, 0, 0),
    "n0": (8, 10, 12, 255), "n1": (16, 19, 23, 255),
    "n2": (23, 26, 29, 255), "n3": (33, 36, 43, 255),
    "n4": (48, 52, 58, 255), "n5": (64, 69, 74, 255),
    "n6": (86, 92, 96, 255), "n7": (112, 118, 122, 255),
    "n8": (144, 148, 151, 255), "n9": (181, 183, 180, 255),
    "n10": (215, 216, 210, 255), "n11": (240, 240, 232, 255),
    "c0": (7, 52, 62, 255), "c1": (8, 99, 119, 255),
    "c2": (3, 143, 169, 255), "c3": (45, 221, 254, 255),
    "r0": (53, 22, 21, 255), "r1": (94, 36, 33, 255),
    "r2": (144, 49, 43, 255), "r3": (216, 91, 73, 255),
    "y0": (75, 56, 22, 255), "y1": (128, 96, 30, 255),
    "y2": (201, 164, 86, 255), "y3": (243, 183, 34, 255),
    "g0": (28, 44, 37, 255), "g1": (49, 81, 63, 255),
    "g2": (88, 124, 98, 255), "g3": (142, 176, 139, 255),
    "p0": (43, 23, 40, 255), "p1": (79, 39, 74, 255),
    "p2": (122, 59, 105, 255), "p3": (179, 93, 146, 255),
}

SIZE = (32, 32)
NEAREST = Image.Resampling.NEAREST


def canvas(opaque=False):
    return Image.new("RGBA", SIZE, P["n2"] if opaque else P["clear"])


def draw_frame(d, outer="n7", inner="n3"):
    d.rectangle((3, 3, 28, 28), outline=P[outer])
    d.rectangle((5, 5, 26, 26), outline=P[inner])
    for x, y in ((3, 3), (28, 3), (3, 28), (28, 28)):
        d.point((x, y), fill=P["n10"])


def bolt(d, points, color="c3", width=2):
    d.line(points, fill=P["n0"], width=width + 2)
    d.line(points, fill=P[color], width=width)


def flame(d, cx=16, cy=17, scale=1, accent="r3"):
    pts = [(cx, cy-10*scale), (cx+5*scale, cy-3*scale), (cx+4*scale, cy+7*scale),
           (cx, cy+10*scale), (cx-6*scale, cy+6*scale), (cx-5*scale, cy-2*scale)]
    d.polygon(pts, fill=P["r1"], outline=P["n0"])
    d.polygon([(cx, cy-5*scale), (cx+2*scale, cy+1*scale), (cx, cy+6*scale),
               (cx-3*scale, cy+2*scale)], fill=P[accent])
    d.point((cx, cy+2*scale), fill=P["y3"])


def shield(d, color="c2", broken=False):
    pts = [(16, 5), (25, 8), (24, 18), (16, 27), (8, 18), (7, 8)]
    d.polygon(pts, fill=P["n2"], outline=P["n0"])
    d.line(pts + [pts[0]], fill=P[color], width=2)
    if broken:
        d.line((17, 6, 13, 14, 18, 17, 14, 26), fill=P["r3"], width=2)
    else:
        d.line((12, 16, 15, 19, 21, 12), fill=P["n10"], width=2)


def cross(d, color="g3"):
    d.rectangle((13, 6, 18, 25), fill=P["n0"])
    d.rectangle((6, 13, 25, 18), fill=P["n0"])
    d.rectangle((14, 7, 17, 24), fill=P[color])
    d.rectangle((7, 14, 24, 17), fill=P[color])


def eye(d, color="y3", slashed=False):
    d.polygon([(5, 16), (10, 10), (16, 7), (22, 10), (27, 16), (22, 22), (16, 25), (10, 22)], outline=P[color])
    d.ellipse((12, 12, 20, 20), fill=P["n0"], outline=P[color])
    d.rectangle((15, 15, 17, 17), fill=P["n11"])
    if slashed:
        d.line((7, 25, 25, 7), fill=P["r3"], width=3)


def droplet(d, color="c3", cx=16, cy=16):
    d.polygon([(cx, cy-10), (cx+7, cy+3), (cx+5, cy+8), (cx, cy+11), (cx-5, cy+8), (cx-7, cy+3)], fill=P[color], outline=P["n0"])


def snowflake(d, color="n11"):
    for line in [((16, 5), (16, 27)), ((6, 10), (26, 22)), ((6, 22), (26, 10))]:
        d.line(line, fill=P["n0"], width=3)
        d.line(line, fill=P[color], width=1)
    for x, y in ((16,5),(16,27),(6,10),(26,22),(6,22),(26,10)):
        d.rectangle((x-1,y-1,x+1,y+1), fill=P["c3"])


def make_floor(kind):
    im = canvas(True); d = ImageDraw.Draw(im)
    d.rectangle((0, 0, 31, 31), fill=P["n3"])
    d.line((0, 0, 31, 0), fill=P["n5"]); d.line((0, 31, 31, 31), fill=P["n1"])
    if kind == "plain":
        for x, y in ((5,7),(23,5),(10,25),(27,19)): d.rectangle((x,y,x+1,y+1), fill=P["n4"])
    elif kind == "industrial":
        d.rectangle((3, 3, 28, 28), outline=P["n5"])
        d.line((4, 16, 27, 16), fill=P["n1"])
        for x, y in ((5,5),(25,5),(5,25),(25,25)): d.rectangle((x,y,x+1,y+1), fill=P["n7"])
    elif kind == "warning":
        for x in range(-16, 48, 10): d.polygon([(x,31),(x+5,31),(x+21,0),(x+16,0)], fill=P["y1"])
        d.rectangle((1,1,30,30), outline=P["y2"])
    else:
        d.rectangle((3,3,28,28), fill=P["r0"], outline=P["r2"])
        for i in range(5, 28, 6): d.line((i,5,27,i+10), fill=P["r1"])
        d.polygon([(16,7),(25,24),(7,24)], outline=P["y3"])
        d.rectangle((15,12,17,19), fill=P["y3"]); d.rectangle((15,22,17,24), fill=P["y3"])
    return im


def make_rail(kind):
    im = make_floor("plain"); d = ImageDraw.Draw(im)
    cx = cy = 16
    arms = {"horizontal":("w","e"), "vertical":("n","s"), "end_n":("n",), "end_e":("e",),
            "end_s":("s",), "end_w":("w",), "corner_ne":("n","e"), "corner_se":("s","e"),
            "corner_sw":("s","w"), "corner_nw":("n","w"), "cross":("n","e","s","w")}[kind]
    for arm in arms:
        box = {"n":(12,0,19,16), "s":(12,16,19,31), "w":(0,12,16,19), "e":(16,12,31,19)}[arm]
        d.rectangle(box, fill=P["n1"])
        if arm in "ns":
            d.line((13,box[1],13,box[3]), fill=P["n7"]); d.line((18,box[1],18,box[3]), fill=P["n7"])
        else:
            d.line((box[0],13,box[2],13), fill=P["n7"]); d.line((box[0],18,box[2],18), fill=P["n7"])
    d.rectangle((12,12,19,19), fill=P["n1"], outline=P["n7"])
    d.rectangle((15,15,16,16), fill=P["c2"])
    return im


def make_object(kind, state):
    im = canvas(); d = ImageDraw.Draw(im)
    if kind in ("light_cover", "heavy_cover"):
        y0 = 13 if kind == "light_cover" else 7
        d.polygon([(4,y0+3),(8,y0),(27,y0),(29,y0+3),(27,27),(5,27)], fill=P["n3"], outline=P["n0"])
        d.rectangle((7,y0+3,26,24), fill=P["n5"], outline=P["n7"])
        if kind == "heavy_cover": d.rectangle((10,9,23,15), fill=P["n2"], outline=P["n8"])
        d.line((8,22,26,22), fill=P["y1"], width=2)
        if state == "damaged":
            d.line((18,y0,14,17,20,21,17,27), fill=P["r3"], width=2)
            d.rectangle((23,y0+1,28,y0+7), fill=P["clear"])
        elif state == "rubble":
            im = canvas(); d = ImageDraw.Draw(im)
            d.polygon([(4,25),(8,19),(14,23),(18,16),(23,22),(28,20),(29,27),(5,28)], fill=P["n4"], outline=P["n0"])
            d.line((7,25,25,23), fill=P["r2"])
    elif kind == "relay":
        d.rectangle((10,7,22,27), fill=P["n3"], outline=P["n0"])
        d.rectangle((12,9,20,23), fill=P["n5"], outline=P["n8"])
        d.ellipse((13,10,19,16), fill=P["c1"], outline=P["c3"])
        d.line((16,16,16,23), fill=P["c2"], width=2)
        d.rectangle((7,25,25,28), fill=P["n4"], outline=P["n0"])
        if state == "damaged":
            d.line((12,8,18,14,14,20,20,25), fill=P["r3"], width=2)
            d.point((23,10), fill=P["y3"]); d.point((25,8), fill=P["y2"])
        elif state == "rubble":
            im = canvas(); d = ImageDraw.Draw(im)
            d.rectangle((7,25,25,28), fill=P["n4"], outline=P["n0"])
            d.polygon([(9,24),(13,14),(17,22),(22,17),(25,25)], fill=P["n3"], outline=P["n0"])
            d.rectangle((15,19,17,22), fill=P["c1"])
    else:
        d.rectangle((5,12,27,26), fill=P["y0"], outline=P["n0"])
        d.rectangle((7,14,25,24), fill=P["n4"], outline=P["y2"])
        d.rectangle((13,18,19,22), fill=P["y3"], outline=P["n0"])
        if state == "open":
            d.polygon([(5,12),(9,5),(25,5),(27,12)], fill=P["n4"], outline=P["y2"])
            d.rectangle((9,13,23,17), fill=P["n0"])
        elif state == "empty":
            d.polygon([(5,12),(9,7),(25,7),(27,12)], fill=P["n3"], outline=P["n7"])
            d.rectangle((8,14,24,23), fill=P["n1"], outline=P["n5"])
    return im


def make_environment(name):
    im = canvas(); d = ImageDraw.Draw(im)
    d.rectangle((1,1,30,30), outline=P["n5"])
    if name == "burning_ground":
        d.line((4,25,28,25), fill=P["r1"], width=2); flame(d,10,18); flame(d,22,20)
    elif name == "water":
        for y, off in ((10,0),(16,4),(22,0)): d.line((4+off,y,12+off,y-2,20+off,y,28,y-2), fill=P["c2"], width=2)
    elif name == "ice": snowflake(d)
    elif name == "smoke":
        for box in ((5,14,15,24),(11,7,23,21),(18,13,28,25)): d.ellipse(box, fill=P["n5"], outline=P["n8"])
    elif name == "bright_zone":
        for a,b in [((16,3),(16,8)),((16,24),(16,29)),((3,16),(8,16)),((24,16),(29,16)),((7,7),(10,10)),((22,22),(25,25)),((7,25),(10,22)),((22,10),(25,7))]: d.line((a,b),fill=P["y3"],width=2)
        d.ellipse((10,10,22,22), fill=P["n11"], outline=P["y2"])
    elif name == "dark_zone":
        d.ellipse((7,5,25,27), fill=P["p0"], outline=P["p2"]); d.ellipse((13,8,25,22), fill=P["clear"]); d.rectangle((7,24,10,27), fill=P["p3"])
    elif name == "conductive_path": bolt(d, [(3,20),(11,12),(15,18),(23,8),(29,12)], "c3", 2)
    else:
        d.rectangle((6,12,26,25), fill=P["n4"], outline=P["n0"]); d.rectangle((9,9,23,22), fill=P["n6"], outline=P["y2"]); d.line((10,18,22,18), fill=P["n2"], width=2)
    return im


def make_overlay(name):
    im = canvas(); d = ImageDraw.Draw(im)
    colors = {"spawn":"c3","exit":"g3","objective":"y3","high_risk":"r3","selected":"n11","move_range":"c2","attack_range":"r2","unreachable":"n7","line_of_sight":"y2"}
    c = colors[name]
    if name in ("move_range","attack_range"):
        d.rectangle((2,2,29,29), outline=P[c], width=2)
        for x,y in ((4,4),(27,4),(4,27),(27,27)): d.rectangle((x-1,y-1,x+1,y+1), fill=P[c])
    elif name == "line_of_sight": bolt(d, [(2,24),(10,18),(18,14),(30,7)], c, 1)
    elif name == "unreachable":
        d.rectangle((3,3,28,28), outline=P[c]); d.line((7,7,25,25),fill=P[c],width=3); d.line((25,7,7,25),fill=P[c],width=3)
    elif name == "objective":
        d.ellipse((6,6,26,26), outline=P[c], width=2); d.ellipse((11,11,21,21), outline=P[c], width=2); d.rectangle((15,15,17,17),fill=P[c])
    elif name == "high_risk":
        d.polygon([(16,3),(29,27),(3,27)], outline=P[c]); d.rectangle((15,10,17,19),fill=P[c]); d.rectangle((15,23,17,25),fill=P[c])
    else:
        d.rectangle((3,3,11,5),fill=P[c]); d.rectangle((3,3,5,11),fill=P[c]); d.rectangle((20,3,28,5),fill=P[c]); d.rectangle((26,3,28,11),fill=P[c]); d.rectangle((3,26,11,28),fill=P[c]); d.rectangle((3,20,5,28),fill=P[c]); d.rectangle((20,26,28,28),fill=P[c]); d.rectangle((26,20,28,28),fill=P[c])
        if name == "spawn": d.polygon([(16,7),(22,18),(18,18),(18,25),(14,25),(14,18),(10,18)],fill=P[c])
        elif name == "exit": d.polygon([(16,25),(10,14),(14,14),(14,7),(18,7),(18,14),(22,14)],fill=P[c])
    return im


def make_semantic(group, name):
    im = canvas(); d = ImageDraw.Draw(im); draw_frame(d)
    if group == "status":
        if name == "burning": flame(d)
        elif name == "slow": d.ellipse((7,7,25,25),outline=P["c2"],width=2); d.line((16,16,16,9),fill=P["n10"],width=2); d.line((16,16,11,20),fill=P["n10"],width=2)
        elif name == "bound": d.ellipse((6,10,17,21),outline=P["p3"],width=3); d.ellipse((15,10,26,21),outline=P["p3"],width=3)
        elif name == "armor_break": shield(d,"r3",True)
        elif name == "dazzled": eye(d,"y3",True)
        else: eye(d,"c3",False)
    elif group == "environment":
        return make_environment(name)
    elif group == "resource":
        if name == "health": cross(d)
        elif name == "shield": shield(d,"g3")
        elif name == "mana": droplet(d,"c3")
        elif name == "action_point": d.polygon([(17,5),(9,17),(15,17),(13,27),(24,13),(18,13)],fill=P["y3"],outline=P["n0"])
        elif name == "parts":
            d.ellipse((7,7,25,25),fill=P["n6"],outline=P["n0"]); d.ellipse((12,12,20,20),fill=P["n1"]); [d.rectangle(b,fill=P["n9"]) for b in ((14,4,18,9),(14,23,18,28),(4,14,9,18),(23,14,28,18))]
        else:
            d.hex = None; d.polygon([(16,5),(25,10),(25,22),(16,27),(7,22),(7,10)],fill=P["c0"],outline=P["c3"]); d.line((11,16,21,16),fill=P["n11"],width=2)
    elif group == "element":
        if name == "fire": flame(d)
        elif name == "water": droplet(d)
        elif name == "wind":
            d.arc((4,7,25,19),180,350,fill=P["c3"],width=2); d.arc((8,13,29,27),10,180,fill=P["n10"],width=2)
        elif name == "earth": d.polygon([(5,24),(10,12),(16,17),(21,7),(28,24)],fill=P["y1"],outline=P["n0"])
        elif name == "lightning": bolt(d,[(18,4),(9,17),(15,17),(12,28),(24,13),(18,13)],"y3",2)
        elif name == "ice": snowflake(d)
        elif name == "light": make = None; d.ellipse((10,10,22,22),fill=P["n11"],outline=P["y3"]); [d.line((16,4,16,8),fill=P["y3"]),d.line((16,24,16,28),fill=P["y3"]),d.line((4,16,8,16),fill=P["y3"]),d.line((24,16,28,16),fill=P["y3"])]
        else: d.ellipse((7,5,25,27),fill=P["p1"],outline=P["p3"]); d.ellipse((13,7,25,22),fill=P["n1"])
    else:
        if name == "move": d.polygon([(16,5),(26,15),(21,15),(21,26),(11,26),(11,15),(6,15)],fill=P["c3"],outline=P["n0"])
        elif name == "attack": d.polygon([(7,21),(20,8),(24,8),(24,12),(11,25)],fill=P["r3"],outline=P["n0"]); d.line((8,20,13,25),fill=P["n10"],width=2)
        elif name == "cast": bolt(d,[(7,23),(13,12),(18,17),(25,7)],"p3",2)
        elif name == "defend": shield(d,"g3")
        else: d.rectangle((7,8,25,24),fill=P["n4"],outline=P["y3"]); d.rectangle((13,4,19,10),fill=P["n6"],outline=P["n0"]); d.line((11,16,21,16),fill=P["r3"],width=2)
    return im


def make_node(name):
    im = canvas(); d = ImageDraw.Draw(im); d.hex = None
    d.polygon([(16,2),(27,8),(27,23),(16,30),(5,23),(5,8)],fill=P["n2"],outline=P["n8"])
    if name == "start": d.polygon([(12,9),(23,16),(12,23)],fill=P["c3"])
    elif name == "combat": make_semantic("intent","attack").crop((6,6,27,27)).resize((20,20),NEAREST); d.line((9,23,23,9),fill=P["r3"],width=3); d.line((9,19,13,23),fill=P["n10"],width=2)
    elif name == "elite": d.polygon([(16,6),(19,12),(26,12),(21,17),(23,25),(16,20),(9,25),(11,17),(6,12),(13,12)],fill=P["r3"],outline=P["n0"])
    elif name == "event": d.arc((7,5,25,23),200,520,fill=P["p3"],width=3); d.rectangle((14,22,18,26),fill=P["p3"])
    elif name == "workshop": d.line((8,24,23,9),fill=P["y2"],width=4); d.ellipse((18,5,27,14),outline=P["n10"],width=2)
    elif name == "shop": d.rectangle((8,12,24,25),fill=P["n4"],outline=P["y2"]); d.polygon([(6,12),(9,6),(23,6),(26,12)],fill=P["y1"],outline=P["n0"])
    elif name == "rest": d.arc((7,8,25,25),20,170,fill=P["g3"],width=3); d.line((8,24,24,24),fill=P["g2"],width=2)
    elif name == "treasure": d.rectangle((7,13,25,24),fill=P["y0"],outline=P["y3"]); d.arc((9,6,23,18),180,360,fill=P["y2"],width=3)
    else: d.ellipse((7,7,25,25),outline=P["r3"],width=3); d.polygon([(16,8),(23,21),(9,21)],fill=P["r1"],outline=P["y3"])
    return im


def make_feedback(name):
    aliases = {"burning":("status","burning"),"bound":("status","bound"),"slow":("status","slow"),"armor_break":("status","armor_break"),"healing":("resource","health"),"shield_restore":("resource","shield"),"mana_restore":("resource","mana"),"movement":("intent","move")}
    if name in aliases: return make_semantic(*aliases[name])
    im=canvas(); d=ImageDraw.Draw(im); draw_frame(d)
    if name == "damage": d.polygon([(16,5),(20,12),(27,13),(22,18),(24,26),(16,22),(8,26),(10,18),(5,13),(12,12)],fill=P["r3"],outline=P["n0"])
    elif name == "shield_absorb": shield(d,"c3")
    elif name == "status_cleared": cross(d,"n11"); d.arc((5,5,27,27),30,300,fill=P["g3"],width=2)
    elif name == "object_damaged": d.rectangle((7,8,25,25),fill=P["n5"],outline=P["n0"]); d.line((17,8,13,15,19,19,15,25),fill=P["r3"],width=2)
    elif name == "object_destroyed":
        for pts in [((7,9),(14,14),(9,23)),((18,6),(25,12),(20,17)),((15,18),(22,25),(11,26))]: d.polygon(pts,fill=P["n6"],outline=P["r3"])
    elif name == "unit_defeated": d.line((8,8,24,24),fill=P["r3"],width=4); d.line((24,8,8,24),fill=P["r3"],width=4); d.ellipse((11,11,21,21),outline=P["n10"])
    return im


def make_item(name):
    im=canvas(); d=ImageDraw.Draw(im); draw_frame(d)
    if "rifle" in name: d.line((6,23,24,8),fill=P["n0"],width=5); d.line((6,23,24,8),fill=P["n9"],width=2); d.rectangle((20,6,27,9),fill=P["y2"]); d.line((12,17,17,23),fill=P["r2"],width=3)
    elif "hammer" in name: d.line((11,25,20,10),fill=P["y1"],width=4); d.rectangle((11,6,25,13),fill=P["n7"],outline=P["n0"]); d.rectangle((8,8,13,16),fill=P["n5"],outline=P["n0"])
    elif "wand" in name: bolt(d,[(8,25),(14,18),(19,12),(24,6)],"c3" if "aether" in name else "p3" if "arcane" in name else "y3",2); d.ellipse((20,3,27,10),fill=P["c1"],outline=P["n11"])
    elif name == "shield": shield(d,"g3")
    elif name == "medkit": d.rectangle((6,9,26,25),fill=P["n4"],outline=P["g3"]); d.rectangle((11,6,21,11),outline=P["n9"]); cross(d,"g3")
    elif name == "shield_cell": d.rectangle((10,5,22,27),fill=P["c0"],outline=P["n10"]); d.rectangle((13,8,19,24),fill=P["c2"]); d.rectangle((14,10,18,15),fill=P["c3"])
    elif name == "fire_bolt_reward": flame(d)
    else: snowflake(d)
    return im


def make_skill(name, index=0, fire=False):
    im=canvas(); d=ImageDraw.Draw(im); draw_frame(d, "r2" if fire else "n7", "r0" if fire else "n3")
    if fire:
        family=(index-1)//10; variant=(index-1)%10
        if family==0:  # precise damage / ignition: ten distinct delivery silhouettes
            motifs=("bolt","lance","orb","brand","needle","fork","fan","drop","crosshair","burst")
            motif=motifs[variant]
            if motif=="bolt": bolt(d,[(6,24),(13,17),(19,13),(27,6)],"r3",2)
            elif motif=="lance": d.line((6,25,25,6),fill=P["n0"],width=5); d.line((6,25,25,6),fill=P["y3"],width=2); d.polygon([(25,6),(21,12),(27,11)],fill=P["r3"])
            elif motif=="orb": d.ellipse((7,7,25,25),fill=P["r1"],outline=P["y3"]); d.ellipse((12,12,20,20),fill=P["r3"])
            elif motif=="brand": d.polygon([(16,5),(25,10),(23,23),(16,27),(8,23),(6,10)],outline=P["r3"],width=2); flame(d,16,17)
            elif motif=="needle": d.polygon([(5,26),(11,16),(24,5),(18,20)],fill=P["r3"],outline=P["n0"]); d.line((9,23,24,5),fill=P["y3"],width=1)
            elif motif=="fork": bolt(d,[(16,27),(16,15),(8,7)],"r3",2); bolt(d,[(16,15),(25,6)],"y3",1)
            elif motif=="fan": [d.line((7,25,26,y),fill=P[c],width=2) for y,c in ((7,"r3"),(13,"y3"),(19,"r2"))]
            elif motif=="drop": droplet(d,"r3"); d.polygon([(12,18),(16,7),(20,18),(16,25)],fill=P["y3"])
            elif motif=="crosshair": d.ellipse((7,7,25,25),outline=P["r3"],width=2); d.line((16,4,16,28),fill=P["y3"]); d.line((4,16,28,16),fill=P["y3"]); flame(d,16,17)
            else: d.polygon([(16,4),(19,12),(27,9),(22,16),(28,22),(19,20),(16,28),(13,20),(4,23),(10,16),(5,9),(13,12)],fill=P["r3"],outline=P["y3"])
        elif family==1:  # fire-field / positional pressure
            if variant==0: [flame(d,x,20 if x!=16 else 16) for x in (9,16,23)]
            elif variant==1: [flame(d,x,y) for x,y in ((9,10),(22,14),(14,23))]
            elif variant==2: d.rectangle((6,6,25,25),outline=P["r3"],width=2); flame(d,16,17)
            elif variant==3: d.ellipse((5,5,27,27),outline=P["y3"],width=2); [flame(d,x,y) for x,y in ((16,9),(9,21),(23,21))]
            elif variant==4: d.line((5,24,27,8),fill=P["r3"],width=4); [flame(d,x,18) for x in (9,16,23)]
            elif variant==5: d.polygon([(5,8),(27,8),(16,27)],outline=P["r3"],width=2); flame(d,16,16)
            elif variant==6: [d.rectangle((x,y,x+5,y+5),fill=P["r2"],outline=P["y3"]) for x,y in ((6,6),(20,6),(13,20))]
            elif variant==7: d.arc((4,4,28,28),0,300,fill=P["r3"],width=4); flame(d,16,17)
            elif variant==8: [d.line((x,5,x,27),fill=P["r3"],width=3) for x in (9,16,23)]
            else: d.polygon([(4,16),(10,10),(16,16),(22,10),(28,16),(22,22),(16,16),(10,22)],outline=P["y3"],width=2); flame(d,16,17)
        elif family==2:  # detonation / harvest
            shapes=("x","ring","star","diamond","split","implosion","chain","cone","core","wave")
            motif=shapes[variant]
            if motif=="x": d.line((6,25,25,6),fill=P["y3"],width=3); d.line((6,6,25,25),fill=P["r3"],width=3)
            elif motif=="ring": d.ellipse((5,5,27,27),outline=P["r3"],width=3); d.ellipse((11,11,21,21),outline=P["y3"],width=2)
            elif motif=="star": d.polygon([(16,4),(19,12),(28,9),(22,16),(28,23),(19,20),(16,28),(13,20),(4,23),(10,16),(4,9),(13,12)],fill=P["r3"],outline=P["y3"])
            elif motif=="diamond": d.polygon([(16,4),(28,16),(16,28),(4,16)],outline=P["r3"],width=3); d.rectangle((13,13,19,19),fill=P["y3"])
            elif motif=="split": [d.polygon(p,fill=P["r3"],outline=P["y3"]) for p in ([(5,6),(15,13),(8,18)],[(27,6),(17,13),(24,18)],[(16,28),(12,17),(20,17)])]
            elif motif=="implosion": [d.line(line,fill=P["r3"],width=3) for line in ((4,4,13,13),(28,4,19,13),(4,28,13,19),(28,28,19,19))]; d.rectangle((14,14,18,18),fill=P["y3"])
            elif motif=="chain": [d.ellipse(b,outline=P[c],width=2) for b,c in (((4,8,14,18),"r3"),((11,11,21,21),"y3"),((18,14,28,24),"r3"))]
            elif motif=="cone": d.polygon([(16,26),(6,7),(26,7)],fill=P["r1"],outline=P["r3"]); d.polygon([(16,22),(12,10),(20,10)],fill=P["y3"])
            elif motif=="core": d.rectangle((5,5,27,27),outline=P["r2"],width=2); d.ellipse((10,10,22,22),fill=P["r3"],outline=P["y3"])
            else: [d.arc((4+i*3,4+i*3,28-i*3,28-i*3),180,360,fill=P[c],width=2) for i,c in ((0,"r2"),(1,"r3"),(2,"y3"))]
        elif family==3:  # armor melting / breaching
            if variant in (0,1,2): shield(d,"r3",True); d.line((5+variant*3,27,27,5+variant*2),fill=P["y3"],width=2)
            elif variant==3: d.rectangle((7,7,25,25),fill=P["n5"],outline=P["n0"]); [d.line(line,fill=P["r3"],width=2) for line in ((8,24,24,8),(10,7,22,25))]
            elif variant==4: d.line((8,25,19,9),fill=P["y1"],width=4); d.rectangle((12,6,27,13),fill=P["r3"],outline=P["n0"])
            elif variant==5: droplet(d,"r3"); d.rectangle((7,24,25,27),fill=P["n6"])
            elif variant==6: d.polygon([(6,8),(26,8),(22,26),(10,26)],outline=P["r3"],width=3); bolt(d,[(8,24),(16,16),(24,8)],"y3",1)
            elif variant==7: [d.rectangle((x,8,x+4,24),fill=P["n6"],outline=P["r3"]) for x in (6,14,22)]
            elif variant==8: d.arc((4,4,28,28),220,500,fill=P["r3"],width=4); d.polygon([(20,4),(28,7),(23,13)],fill=P["y3"])
            else: d.polygon([(5,25),(10,9),(16,15),(22,6),(27,25)],fill=P["r1"],outline=P["y3"])
        else:  # thermal tactics / mobility / resource control
            if variant==0: d.ellipse((6,6,26,26),outline=P["y2"],width=2); flame(d,16,17)
            elif variant==1: d.polygon([(6,20),(18,7),(18,13),(27,13),(15,26),(15,20)],fill=P["r3"],outline=P["y3"])
            elif variant==2: cross(d,"r3"); flame(d,16,17)
            elif variant==3: shield(d,"r3"); d.ellipse((12,12,20,20),fill=P["y3"])
            elif variant==4: d.arc((5,5,27,27),-90,210,fill=P["r3"],width=3); d.polygon([(6,8),(13,7),(9,14)],fill=P["y3"])
            elif variant==5: d.line((5,24,25,8),fill=P["r3"],width=3); d.polygon([(22,5),(28,7),(24,13)],fill=P["y3"]); flame(d,9,20)
            elif variant==6: d.rectangle((7,6,25,26),outline=P["y2"],width=2); d.rectangle((11,18,21,23),fill=P["r3"]); flame(d,16,13)
            elif variant==7: [d.ellipse(b,outline=P[c],width=2) for b,c in (((5,5,17,17),"r3"),((15,15,27,27),"y3"))]; bolt(d,[(9,23),(16,16),(23,9)],"r3",1)
            elif variant==8: d.polygon([(16,5),(26,10),(24,24),(16,28),(8,24),(6,10)],outline=P["y2"],width=2); d.line((8,23,24,9),fill=P["r3"],width=3)
            else: [flame(d,x,y) for x,y in ((8,21),(16,12),(24,21))]; d.arc((5,5,27,27),180,360,fill=P["y3"],width=2)
        return im
    low=name.lower()
    if any(k in low for k in ("fire","ember","searing","cinder","thermal")): flame(d)
    elif any(k in low for k in ("frost","cryo")): snowflake(d)
    elif any(k in low for k in ("shield","barrier","bastion")): shield(d,"g3")
    elif any(k in low for k in ("repair","regenerative","rescue")): cross(d,"g3")
    elif any(k in low for k in ("bind","tether","anchor")): d.ellipse((6,10,17,21),outline=P["p3"],width=3); d.ellipse((15,10,26,21),outline=P["p3"],width=3)
    elif any(k in low for k in ("breach","solvent","demolition")): shield(d,"r3",True)
    elif "phase" in low: d.polygon([(8,8),(19,8),(14,14),(24,14),(13,25),(16,17),(7,17)],fill=P["c3"],outline=P["n0"])
    else: bolt(d,[(7,24),(13,16),(18,19),(25,7)],"c3",2)
    return im


def make_ui(name):
    im=canvas(); d=ImageDraw.Draw(im)
    color={"panel":"n7","slot":"n6","slot_selected":"c3","slot_disabled":"n4","slot_cooldown":"y3","slot_empty":"r3"}[name]
    d.rectangle((1,1,30,30),fill=P["n1"],outline=P[color])
    d.rectangle((4,4,27,27),outline=P["n3"])
    for x,y in ((2,2),(29,2),(2,29),(29,29)): d.rectangle((x-1,y-1,x+1,y+1),fill=P[color])
    if name=="slot_disabled": d.line((6,25,25,6),fill=P["n6"],width=2)
    elif name=="slot_cooldown": d.arc((7,7,25,25),-90,200,fill=P["y3"],width=3)
    elif name=="slot_empty": d.rectangle((10,14,22,18),fill=P["r3"])
    return im


def audit(path):
    im=Image.open(path).convert("RGBA")
    colors={p[:3] for p in im.getdata() if p[3]}
    alphas={p[3] for p in im.getdata()}
    failures=[]
    if im.size != SIZE: failures.append("size")
    if not alphas.issubset({0,255}): failures.append("alpha")
    if len(colors)>16: failures.append(f"palette:{len(colors)}")
    return {"path":str(path),"sha256":hashlib.sha256(path.read_bytes()).hexdigest(),"size":list(im.size),"visible_colors":len(colors),"alpha_values":sorted(alphas),"result":"PASS" if not failures else "FAIL","failures":failures}


def contact_sheet(paths, out, columns=8):
    paths=list(paths); scale=4; label_h=10; cell_w=32*scale; cell_h=32*scale+label_h
    rows=max(1,(len(paths)+columns-1)//columns)
    sheet=Image.new("RGBA",(columns*cell_w,rows*cell_h),P["n0"]); d=ImageDraw.Draw(sheet)
    for i,path in enumerate(paths):
        x=(i%columns)*cell_w; y=(i//columns)*cell_h
        bg=Image.new("RGBA",SIZE,P["n3"]); bg.alpha_composite(Image.open(path).convert("RGBA"))
        sheet.alpha_composite(bg.resize((cell_w,32*scale),NEAREST),(x,y))
        d.text((x+2,y+32*scale),path.stem[:19],fill=P["n10"])
    out.parent.mkdir(parents=True,exist_ok=True); sheet.save(out)


def generate(unity_root: Path, qa_root: Path):
    outputs=[]
    def save(rel, image):
        path=unity_root/rel; path.parent.mkdir(parents=True,exist_ok=True); image.save(path); outputs.append(path)

    for k in ("plain","industrial","warning","hazard"): save(f"FormalRelayV01/floor_{k}.png",make_floor(k))
    for k in ("horizontal","vertical","end_n","end_e","end_s","end_w","corner_ne","corner_se","corner_sw","corner_nw","cross"): save(f"FormalRelayV01/rail_{k}.png",make_rail(k))
    for kind in ("light_cover","heavy_cover","relay"):
        for state in ("intact","damaged","rubble"): save(f"FormalRelayV01/{kind}_{state}.png",make_object(kind,state))
    for state in ("closed","open","empty"): save(f"FormalRelayV01/loot_crate_{state}.png",make_object("loot",state))
    environments=("burning_ground","water","ice","smoke","bright_zone","dark_zone","conductive_path","obstacle_cover")
    for name in environments: save(f"FormalEnvironment32/{name}.png",make_environment(name))
    for name in ("spawn","exit","objective","high_risk","selected","move_range","attack_range","unreachable","line_of_sight"): save(f"FormalTacticalOverlays32/{name}.png",make_overlay(name))
    statuses=("burning","slow","bound","armor_break","dazzled","revealed")
    for name in statuses: save(f"FormalStatusIcons32/{name}.png",make_semantic("status",name))
    resources=("health","shield","mana","action_point","parts","operational_aether")
    for name in resources: save(f"FormalResourceIcons32/{name}.png",make_semantic("resource",name))
    for name in environments: save(f"FormalEnvironmentIcons32/{name}.png",make_semantic("environment",name))
    for name in ("fire","water","wind","earth","lightning","ice","light","dark"): save(f"FormalElementIcons32/{name}.png",make_semantic("element",name))
    for name in ("move","attack","cast","defend","interact_destroy"): save(f"FormalIntentIcons32/{name}.png",make_semantic("intent",name))
    nodes=("start","combat","elite","event","workshop","shop","rest","treasure","finale")
    for name in nodes: save(f"FormalNodeIcons32/types/{name}.png",make_node(name))
    feedback=("damage","shield_absorb","armor_break","burning","bound","slow","healing","shield_restore","mana_restore","status_cleared","movement","object_damaged","object_destroyed","unit_defeated")
    for name in feedback: save(f"FormalFeedbackIcons32/{name}.png",make_feedback(name))
    items=("rifle","hammer","wand","shield","medkit","shield_cell","war_hammer","aether_wand","arcane_wand","fire_bolt_reward","frost_bind_reward")
    for name in items: save(f"FormalItemIcons32/{name}.png",make_item(name))
    runtime=("fire_bolt","frost_bind","ember_lance","breach_shot","hammer_pulse","searing_mark","rail_burst","cinder_sweep","tether_arc","damping_field","armor_solvent","cryo_pulse","anchor_seal","arc_bolt","mana_siphon","shield_converter","aether_surge","prism_arc","phase_step","overload_needle","field_repair","barrier_charge","thermal_purge","regenerative_seal","rescue_beam","bastion_pulse","demolition_charge")
    for name in runtime: save(f"FormalSkillIcons32/Runtime/{name}.png",make_skill(name))
    for index in range(1,51): save(f"FormalSkillIcons32/Fire/f-p{index:02}.png",make_skill(f"f-p{index:02}",index,True))
    for name in ("panel","slot","slot_selected","slot_disabled","slot_cooldown","slot_empty"): save(f"FormalUI32/{name}.png",make_ui(name))

    reports=[audit(p) for p in outputs]
    qa_root.mkdir(parents=True,exist_ok=True)
    (qa_root/"occ_noncharacter_asset_qa_v01.json").write_text(json.dumps({"schema":"occ.formal.noncharacter.qa.v0.1","count":len(reports),"passed":sum(r["result"]=="PASS" for r in reports),"failed":sum(r["result"]!="PASS" for r in reports),"assets":reports},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    groups={}
    for p in outputs: groups.setdefault(p.parent.name,[]).append(p)
    for group,paths in groups.items(): contact_sheet(paths,qa_root/f"contact_{group}.png")
    contact_sheet(outputs,qa_root/"contact_all_noncharacter.png",10)
    return reports


def main():
    parser=argparse.ArgumentParser(); parser.add_argument("--unity-root",type=Path,required=True); parser.add_argument("--qa-root",type=Path,required=True); args=parser.parse_args()
    reports=generate(args.unity_root,args.qa_root); failed=[r for r in reports if r["result"]!="PASS"]
    print(json.dumps({"generated":len(reports),"passed":len(reports)-len(failed),"failed":len(failed)},ensure_ascii=False))
    raise SystemExit(2 if failed else 0)


if __name__=="__main__": main()
