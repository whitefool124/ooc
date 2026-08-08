#!/usr/bin/env python3
"""Generate the complete deterministic OCC strong-pixel UI v02 slice set and QA evidence."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw

INK=(5,8,11,255); SURFACE=(10,16,21,255); PANEL=(15,23,29,255); RAISED=(22,31,38,255)
EDGE=(55,70,76,255); LIGHT=(108,126,130,255); CYAN=(77,199,224,255); AMBER=(250,184,71,255)
SAFE=(82,184,154,255); RED=(209,87,71,255); PURPLE=(154,101,196,255); MUTED=(78,89,94,255)

SPECS={
 "panel":(PANEL,EDGE,LIGHT,"panel"), "panel_elevated":(RAISED,LIGHT,CYAN,"panel"), "header":(SURFACE,EDGE,CYAN,"header"),
 "button_idle":(RAISED,EDGE,LIGHT,"button"), "button_hover":((27,43,49,255),CYAN,LIGHT,"button"),
 "button_pressed":((8,14,18,255),CYAN,MUTED,"pressed"), "button_disabled":((9,11,13,255),MUTED,(38,43,46,255),"disabled"),
 "tab_idle":(SURFACE,EDGE,LIGHT,"tab"), "tab_active":((19,39,45,255),CYAN,LIGHT,"tab_active"),
 "slot":((9,14,18,255),EDGE,LIGHT,"slot"), "bar_track":((6,10,13,255),EDGE,MUTED,"track"),
 "bar_fill":((20,49,55,255),CYAN,LIGHT,"bar"), "focus":((0,0,0,0),CYAN,LIGHT,"focus"),
 "danger":((35,17,17,255),RED,AMBER,"danger"), "reward":((33,28,14,255),AMBER,LIGHT,"reward"),
 "panel_console":((7,12,16,255),CYAN,EDGE,"console"), "panel_module":(PANEL,EDGE,CYAN,"module"),
 "panel_target":((19,17,15,255),AMBER,RED,"target"), "panel_log":((8,13,17,255),EDGE,MUTED,"log"),
 "group_weapon":((14,24,28,255),CYAN,LIGHT,"weapon"), "group_spell":((24,17,30,255),PURPLE,LIGHT,"spell"),
 "group_interaction":((27,24,14,255),AMBER,LIGHT,"interaction"), "group_item":((13,25,20,255),SAFE,LIGHT,"item"),
 "button_end_turn":((20,34,36,255),CYAN,AMBER,"end"),
 "bar_segment_health":((13,38,28,255),SAFE,LIGHT,"segment"), "bar_segment_shield":((14,34,38,255),CYAN,LIGHT,"segment"),
 "bar_segment_mana":((29,20,39,255),PURPLE,LIGHT,"segment"), "badge_cost":((37,29,12,255),AMBER,LIGHT,"badge"),
 "slot_locked":((13,13,15,255),MUTED,RED,"locked"), "timeline_node":((10,19,23,255),CYAN,LIGHT,"timeline")
}

def stepped_frame(draw, fill, edge, shine):
    draw.rectangle((2,0,13,15), fill=fill)
    draw.rectangle((0,2,15,13), fill=fill)
    draw.line((2,0,13,0), fill=edge); draw.line((2,15,13,15), fill=(3,5,7,255))
    draw.line((0,2,0,13), fill=edge); draw.line((15,2,15,13), fill=(3,5,7,255))
    draw.point([(1,1),(14,1),(1,14),(14,14)], fill=edge)
    draw.line((3,2,11,2), fill=shine); draw.line((2,3,2,11), fill=shine)
    draw.line((4,13,12,13), fill=(3,5,7,255)); draw.line((13,4,13,12), fill=(3,5,7,255))

def tile(name, spec):
    fill, edge, shine, motif=spec
    image=Image.new("RGBA",(16,16),(0,0,0,0)); draw=ImageDraw.Draw(image); stepped_frame(draw,fill,edge,shine)
    if motif in {"header","tab_active","module","console"}: draw.rectangle((4,4,11,5),fill=edge)
    if motif in {"button","pressed"}: draw.rectangle((5,6,10,9),fill=(max(0,fill[0]-3),max(0,fill[1]-3),max(0,fill[2]-3),255))
    if motif=="pressed": draw.line((4,12,11,12),fill=edge)
    if motif=="disabled": draw.line((4,4,11,11),fill=MUTED)
    if motif=="slot": draw.rectangle((5,5,10,10),outline=EDGE)
    if motif in {"bar","segment"}: draw.rectangle((4,5,11,10),fill=edge); draw.line((5,5,10,5),fill=shine)
    if motif=="track": draw.rectangle((4,6,11,9),fill=INK)
    if motif=="reward": draw.point([(4,4),(11,4),(4,11),(11,11)],fill=AMBER)
    if motif in {"danger","target"}: draw.line((4,4,11,11),fill=RED); draw.line((11,4,4,11),fill=RED)
    if motif=="console": draw.rectangle((4,7,11,11),outline=EDGE); draw.point((5,8),fill=CYAN)
    if motif=="log":
        for y in (5,8,11): draw.line((4,y,11,y),fill=MUTED)
    if motif in {"weapon","spell","interaction","item"}: draw.rectangle((4,4,6,11),fill=edge); draw.rectangle((9,4,11,11),fill=edge)
    if motif=="end": draw.rectangle((4,4,11,11),outline=AMBER); draw.rectangle((7,3,8,8),fill=CYAN)
    if motif=="badge": draw.rectangle((4,4,11,11),fill=AMBER); draw.rectangle((6,6,9,9),fill=INK)
    if motif=="locked": draw.rectangle((5,7,10,11),outline=RED); draw.line((6,7,6,5),fill=MUTED); draw.line((9,7,9,5),fill=MUTED)
    if motif=="timeline": draw.line((3,8,12,8),fill=EDGE); draw.rectangle((6,5,9,10),fill=CYAN)
    if motif=="focus":
        image=Image.new("RGBA",(16,16),(0,0,0,0)); draw=ImageDraw.Draw(image)
        draw.line((0,0,6,0),fill=CYAN); draw.line((0,0,0,6),fill=CYAN); draw.line((15,0,9,0),fill=CYAN); draw.line((15,0,15,6),fill=CYAN)
        draw.line((0,15,6,15),fill=CYAN); draw.line((0,15,0,9),fill=CYAN); draw.line((15,15,9,15),fill=CYAN); draw.line((15,15,15,9),fill=CYAN)
        draw.point([(2,2),(13,2),(2,13),(13,13)],fill=LIGHT)
    return image

def main():
    parser=argparse.ArgumentParser(); parser.add_argument("--unity",type=Path,required=True); parser.add_argument("--qa",type=Path,required=True); args=parser.parse_args()
    args.unity.mkdir(parents=True,exist_ok=True); args.qa.mkdir(parents=True,exist_ok=True)
    records=[]; images=[]
    for name,spec in SPECS.items():
        image=tile(name,spec); path=args.unity/(name+".png"); image.save(path,optimize=False); images.append((name,image))
        pixels=set(image.getdata()); alpha={pixel[3] for pixel in pixels}; status="PASS" if image.size==(16,16) and alpha.issubset({0,255}) and len(pixels)<=12 else "FAIL"
        records.append({"id":name,"size":[16,16],"colors":len(pixels),"hard_alpha":alpha.issubset({0,255}),"sha256":hashlib.sha256(path.read_bytes()).hexdigest(),"status":status})
    sheet=Image.new("RGBA",(6*112,5*104),(4,7,9,255)); draw=ImageDraw.Draw(sheet)
    for index,(name,image) in enumerate(images):
        x=(index%6)*112; y=(index//6)*104; sheet.alpha_composite(image.resize((80,80),Image.Resampling.NEAREST),(x+16,y+4)); draw.text((x+4,y+86),name,fill=(220,230,232,255))
    sheet.save(args.qa/"occ_pixel_ui_v02_contact.png")
    report={"schema":"occ.pixel.ui.assets.v0.2","status":"QA_PASS" if all(r["status"]=="PASS" for r in records) else "QA_FAIL","asset_count":len(records),"slice_border":[4,4,4,4],"logical_pixel_scale":4,"records":records}
    (args.qa/"occ_pixel_ui_v02_qa.json").write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"assets":len(records),"pass":sum(r["status"]=="PASS" for r in records),"status":report["status"]},ensure_ascii=False))

if __name__=="__main__": main()
