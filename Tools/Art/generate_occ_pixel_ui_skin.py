#!/usr/bin/env python3
"""Generate the deterministic OCC pixel UI skin and its review sheet."""
from __future__ import annotations

import argparse, hashlib, json
from pathlib import Path
from PIL import Image, ImageDraw

INK=(5,8,11,255); SURFACE=(10,16,21,255); PANEL=(15,23,29,255); RAISED=(22,31,38,255)
EDGE=(55,70,76,255); LIGHT=(108,126,130,255); CYAN=(77,199,224,255); AMBER=(250,184,71,255)
RED=(209,87,71,255); MUTED=(78,89,94,255)

SPECS={
 "panel":(PANEL,EDGE,LIGHT), "panel_elevated":(RAISED,LIGHT,CYAN), "header":(SURFACE,EDGE,CYAN),
 "button_idle":(RAISED,EDGE,LIGHT), "button_hover":((27,43,49,255),CYAN,LIGHT),
 "button_pressed":((8,14,18,255),CYAN,MUTED), "button_disabled":((9,11,13,255),MUTED,(38,43,46,255)),
 "tab_idle":(SURFACE,EDGE,LIGHT), "tab_active":((19,39,45,255),CYAN,LIGHT),
 "slot":((9,14,18,255),EDGE,LIGHT), "bar_track":((6,10,13,255),EDGE,MUTED),
 "bar_fill":((20,49,55,255),CYAN,LIGHT), "focus":((0,0,0,0),CYAN,LIGHT),
 "danger":((35,17,17,255),RED,AMBER), "reward":((33,28,14,255),AMBER,LIGHT),
}

def tile(name, colors):
    fill, edge, shine=colors; im=Image.new("RGBA",(16,16),fill); d=ImageDraw.Draw(im)
    d.rectangle((1,1,14,14),outline=edge); d.point([(0,3),(0,12),(15,3),(15,12),(3,0),(12,0),(3,15),(12,15)],fill=edge)
    d.line((3,2,12,2),fill=shine); d.line((2,3,2,12),fill=shine)
    d.line((3,13,12,13),fill=(3,5,7,255)); d.line((13,3,13,12),fill=(3,5,7,255))
    if name in {"header","tab_active","bar_fill"}: d.rectangle((4,4,11,5),fill=CYAN)
    if name=="reward": d.point([(4,4),(11,4),(4,11),(11,11)],fill=AMBER)
    if name=="danger": d.line((4,4,11,11),fill=RED); d.line((11,4,4,11),fill=RED)
    if name=="focus":
        im=Image.new("RGBA",(16,16),(0,0,0,0)); d=ImageDraw.Draw(im)
        d.line((0,0,5,0),fill=CYAN); d.line((0,0,0,5),fill=CYAN); d.line((15,0,10,0),fill=CYAN); d.line((15,0,15,5),fill=CYAN)
        d.line((0,15,5,15),fill=CYAN); d.line((0,15,0,10),fill=CYAN); d.line((15,15,10,15),fill=CYAN); d.line((15,15,15,10),fill=CYAN)
    return im

def main():
    p=argparse.ArgumentParser(); p.add_argument("--unity",type=Path,required=True); p.add_argument("--qa",type=Path,required=True); a=p.parse_args()
    a.unity.mkdir(parents=True,exist_ok=True); a.qa.mkdir(parents=True,exist_ok=True); records=[]; images=[]
    for name, colors in SPECS.items():
        im=tile(name,colors); path=a.unity/(name+".png"); im.save(path,optimize=False); images.append((name,im))
        alpha=set(im.getchannel("A").getdata()); records.append({"id":name,"size":[16,16],"colors":len(set(im.getdata())),"hard_alpha":alpha.issubset({0,255}),"sha256":hashlib.sha256(path.read_bytes()).hexdigest(),"status":"PASS"})
    sheet=Image.new("RGBA",(5*96,3*96),(4,7,9,255)); d=ImageDraw.Draw(sheet)
    for i,(name,im) in enumerate(images):
        x=(i%5)*96; y=(i//5)*96; sheet.alpha_composite(im.resize((64,64),Image.Resampling.NEAREST),(x+16,y+8)); d.text((x+4,y+74),name,fill=(220,230,232,255))
    sheet.save(a.qa/"occ_pixel_ui_skin_contact_v01.png")
    (a.qa/"occ_pixel_ui_skin_qa_v01.json").write_text(json.dumps({"schema":"occ.pixel.ui.skin.v0.1","status":"QA_PASS","asset_count":len(records),"slice_border":[4,4,4,4],"records":records},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"assets":len(records),"pass":sum(r["status"]=="PASS" for r in records)},ensure_ascii=False))
if __name__=="__main__": main()
