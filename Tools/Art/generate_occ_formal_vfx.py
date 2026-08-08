#!/usr/bin/env python3
"""Generate the 24 base OCC VFX and five fire-composition templates.

Outputs independent 32x32 frames as runtime assets. Strips and GIFs are review
derivatives only, never the source for slicing.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


P={
"z":(0,0,0,0),"n0":(8,10,12,255),"n3":(33,36,43,255),"n6":(86,92,96,255),"n9":(181,183,180,255),"n11":(240,240,232,255),
"c1":(8,99,119,255),"c2":(3,143,169,255),"c3":(45,221,254,255),"r1":(94,36,33,255),"r2":(144,49,43,255),"r3":(216,91,73,255),
"y1":(128,96,30,255),"y2":(201,164,86,255),"y3":(243,183,34,255),"g1":(49,81,63,255),"g2":(88,124,98,255),"g3":(142,176,139,255),
"p1":(79,39,74,255),"p2":(122,59,105,255),"p3":(179,93,146,255)}
N=Image.Resampling.NEAREST

BASE=("selection","lock","path","landing","shoot","melee","hit","heavy_hit","shield_hit","shield_absorb","shield_break","shield_restore","health_repair","mana_restore","cleanse","burning","slow","bound","armor_break","dazzled","revealed","object_damage","object_break","debris")
FIRE=("fire_projectile","fire_spray","fire_cross_blast","fire_burning_ground","fire_detonate")


def img(): return Image.new("RGBA",(32,32),P["z"])
def ring(d,box,c,w=1): d.ellipse(box,outline=P[c],width=w)
def ray(d,a,b,c,w=1): d.line((a,b),fill=P["n0"],width=w+2); d.line((a,b),fill=P[c],width=w)
def flame(d,x,y,s=1):
    d.polygon([(x,y-7*s),(x+4*s,y-2*s),(x+3*s,y+6*s),(x,y+8*s),(x-4*s,y+5*s),(x-3*s,y-2*s)],fill=P["r2"],outline=P["n0"])
    d.polygon([(x,y-3*s),(x+2*s,y+2*s),(x,y+5*s),(x-2*s,y+2*s)],fill=P["y3"])
def spark(d,x,y,c="y3",r=4):
    d.line((x-r,y,x+r,y),fill=P[c]); d.line((x,y-r,x,y+r),fill=P[c]); d.line((x-r+1,y-r+1,x+r-1,y+r-1),fill=P[c]); d.line((x+r-1,y-r+1,x-r+1,y+r-1),fill=P[c])
def shield(d,box,c="c3",broken=False):
    x0,y0,x1,y1=box; cx=(x0+x1)//2
    pts=[(cx,y0),(x1,y0+3),(x1-2,y1-5),(cx,y1),(x0+2,y1-5),(x0,y0+3)]
    d.line(pts+[pts[0]],fill=P[c],width=2)
    if broken: d.line((cx+1,y0,cx-2,(y0+y1)//2,cx+2,y1),fill=P["r3"],width=2)


def frame(effect,t,count=6):
    im=img(); d=ImageDraw.Draw(im); q=t/(count-1)
    if effect=="selection":
        inset=2+(t%3); d.rectangle((inset,inset,31-inset,31-inset),outline=P["c3"],width=1+(t%2))
        for x,y in ((inset,inset),(31-inset,inset),(inset,31-inset),(31-inset,31-inset)): d.rectangle((x-1,y-1,x+1,y+1),fill=P["n11"])
    elif effect=="lock":
        r=13-t; ring(d,(16-r,16-r,16+r,16+r),"r3",2); d.line((16,2+t,16,8+t),fill=P["y3"]); d.line((2+t,16,8+t,16),fill=P["y3"])
    elif effect=="path":
        pts=[(2,26),(8,22),(13,17),(19,14),(25,8),(30,5)]; upto=max(2,min(len(pts),t+2)); d.line(pts[:upto],fill=P["n0"],width=3); d.line(pts[:upto],fill=P["c3"],width=1); d.rectangle((pts[upto-1][0]-1,pts[upto-1][1]-1,pts[upto-1][0]+1,pts[upto-1][1]+1),fill=P["n11"])
    elif effect=="landing":
        r=max(2,13-t*2); ring(d,(16-r,16-r,16+r,16+r),"y3",2); d.ellipse((13,13,19,19),fill=P["y2"])
    elif effect=="shoot":
        x=3+t*5; ray(d,(2,23),(x,8),"y3",1); d.rectangle((x-1,7,x+1,9),fill=P["n11"])
    elif effect=="melee":
        d.arc((3,3,29,29),210-t*18,250+t*22,fill=P["n11"],width=2); d.line((7+t,25,22+t//2,7),fill=P["r3"],width=2)
    elif effect in ("hit","heavy_hit"):
        r=3+t*(3 if effect=="heavy_hit" else 2); spark(d,16,16,"r3",min(13,r));
        if effect=="heavy_hit": ring(d,(16-r,16-r,16+r,16+r),"y3",2)
    elif effect in ("shield_hit","shield_absorb","shield_break","shield_restore"):
        shield(d,(6,4,26,28),"c3" if effect!="shield_restore" else "g3",effect=="shield_break")
        if effect=="shield_hit": spark(d,7+t*3,18-t,"y3",3+t)
        elif effect=="shield_absorb": ring(d,(9-t,7-t,23+t,25+t),"c2",1)
        elif effect=="shield_break":
            for x,y in ((6-t,7+t),(25+t,8+t),(8-t,25+t),(24+t,24+t)): d.rectangle((x,y,x+2,y+2),fill=P["c3"])
        else: d.arc((3,3,29,29),30+t*25,180+t*25,fill=P["g3"],width=2)
    elif effect in ("health_repair","mana_restore","cleanse"):
        c="g3" if effect!="mana_restore" else "c3"; y=27-t*4
        if effect=="health_repair": d.rectangle((13,y-7,18,y+7),fill=P[c]); d.rectangle((9,y-3,22,y+3),fill=P[c])
        elif effect=="mana_restore": d.polygon([(16,y-9),(22,y+2),(20,y+7),(16,y+10),(12,y+7),(10,y+2)],fill=P[c],outline=P["n0"])
        else: ring(d,(5+t,5+t,27-t,27-t),"n11",2); spark(d,16,16,"g3",3+t)
    elif effect=="burning": flame(d,16,20-t//2,1); [d.point((7+i*4,25-(i+t)%8),fill=P["r3"]) for i in range(5)]
    elif effect=="slow":
        ring(d,(7,7,25,25),"c2",2); d.line((16,16,16,9),fill=P["n11"],width=2); d.line((16,16,10+t,20),fill=P["n11"],width=2)
    elif effect=="bound":
        d.ellipse((4+t,9,16+t,22),outline=P["p3"],width=3); d.ellipse((16-t,9,28-t,22),outline=P["p3"],width=3)
    elif effect=="armor_break": shield(d,(6,4,26,28),"r3",True); spark(d,16,16,"y3",3+t)
    elif effect=="dazzled":
        d.polygon([(4,16),(10,9),(16,7),(22,9),(28,16),(22,23),(16,25),(10,23)],outline=P["y3"],width=2); spark(d,16,16,"n11",2+t)
    elif effect=="revealed":
        d.polygon([(4,16),(10,9),(16,7),(22,9),(28,16),(22,23),(16,25),(10,23)],outline=P["c3"],width=2); ring(d,(11-t//2,11-t//2,21+t//2,21+t//2),"n11",1)
    elif effect in ("object_damage","object_break","debris"):
        if effect!="debris": d.rectangle((6,7,26,26),fill=P["n6"],outline=P["n0"]); d.line((17,7,13,14,19,18,15,26),fill=P["r3"],width=2)
        amount=3+t if effect!="object_damage" else t
        for i in range(amount):
            x=5+(i*7+t*3)%23; y=25-((i*4+t*3)%18); d.polygon([(x,y),(x+2,y-3),(x+4,y+1)],fill=P["n9" if i%2 else "r3"])
    elif effect=="fire_projectile":
        x=4+t*5; flame(d,x,20-t*2); d.line((2,24,x-3,21-t*2),fill=P["r3"],width=2)
    elif effect=="fire_spray":
        for i in range(t+1): flame(d,7+i*4,23-i*2); d.line((3,26,28,8),fill=P["r1"],width=2)
    elif effect=="fire_cross_blast":
        r=2+t*3; d.line((16-r,16,16+r,16),fill=P["r3"],width=3); d.line((16,16-r,16,16+r),fill=P["y3"],width=3); spark(d,16,16,"n11",min(12,r))
    elif effect=="fire_burning_ground":
        d.line((3,27,29,27),fill=P["r1"],width=2); [flame(d,x,23-((t+i)%3),1) for i,x in enumerate((8,16,24))]
    elif effect=="fire_detonate":
        r=2+t*3; ring(d,(16-r,16-r,16+r,16+r),"r3",2); spark(d,16,16,"y3",min(12,r));
        if t>2: [flame(d,x,y) for x,y in ((8,10),(24,10),(10,24),(23,23))]
    return im


def audit(path):
    im=Image.open(path).convert("RGBA"); colors={p[:3] for p in im.getdata() if p[3]}; alphas={p[3] for p in im.getdata()}; fail=[]
    if im.size!=(32,32): fail.append("size")
    if not alphas.issubset({0,255}): fail.append("alpha")
    if len(colors)>16: fail.append("palette")
    return {"path":str(path),"sha256":hashlib.sha256(path.read_bytes()).hexdigest(),"colors":len(colors),"result":"PASS" if not fail else "FAIL","failures":fail}


def generate(unity_root,qa_root,count=6):
    records=[]; qa_root.mkdir(parents=True,exist_ok=True)
    for effect in BASE+FIRE:
        frames=[]; folder=unity_root/effect; folder.mkdir(parents=True,exist_ok=True)
        for t in range(count):
            im=frame(effect,t,count); path=folder/f"frame_{t:02}.png"; im.save(path); frames.append(path); records.append(audit(path))
        strip=Image.new("RGBA",(32*count,32),P["z"])
        for i,path in enumerate(frames): strip.alpha_composite(Image.open(path).convert("RGBA"),(i*32,0))
        strip.save(qa_root/f"{effect}_strip.png")
        preview=[Image.open(p).convert("RGBA").resize((128,128),N) for p in frames]
        preview[0].save(qa_root/f"{effect}.gif",save_all=True,append_images=preview[1:],duration=90 if effect in FIRE else 110,loop=0,disposal=2)
    report={"schema":"occ.formal.vfx.qa.v0.1","effects":len(BASE)+len(FIRE),"frames":len(records),"passed":sum(r["result"]=="PASS" for r in records),"failed":sum(r["result"]!="PASS" for r in records),"event_frames":{"shoot":3,"melee":3,"hit":2,"heavy_hit":3,"shield_break":3,"object_break":3,"fire_projectile":4,"fire_cross_blast":3,"fire_detonate":3},"assets":records}
    (qa_root/"occ_formal_vfx_qa_v01.json").write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    strips=[Image.open(qa_root/f"{e}_strip.png").convert("RGBA").resize((32*count*2,64),N) for e in BASE+FIRE]
    sheet=Image.new("RGBA",(32*count*2,64*len(strips)),P["n0"])
    for i,s in enumerate(strips): sheet.alpha_composite(s,(0,i*64)); ImageDraw.Draw(sheet).text((2,i*64+48), (BASE+FIRE)[i],fill=P["n11"])
    sheet.save(qa_root/"contact_all_vfx.png")
    return report


def main():
    p=argparse.ArgumentParser(); p.add_argument("--unity-root",type=Path,required=True); p.add_argument("--qa-root",type=Path,required=True); a=p.parse_args()
    r=generate(a.unity_root,a.qa_root); print(json.dumps({k:r[k] for k in ("effects","frames","passed","failed")},ensure_ascii=False)); raise SystemExit(2 if r["failed"] else 0)

if __name__=="__main__": main()
