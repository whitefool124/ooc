#!/usr/bin/env python3
from __future__ import annotations
import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];CAT=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A23/item_footprints_4_catalog.json";OUT=ROOT/"UnityProject/Artifacts/ItemFootprints4/contacts"
def f(n):return ImageFont.truetype("C:/Windows/Fonts/msyh.ttc",n)
def make(w,h):
 a=json.loads(CAT.read_text(encoding="utf-8"))["assets"];im=Image.new("RGB",(w,h),(31,29,26));d=ImageDraw.Draw(im);d.text((40,20),"OCC M-A23 通用物品背包占格 · 4 件",fill=(235,220,190),font=f(30 if w>1000 else 16));s=2 if w>1000 else 1;cw=(w-80-30*3)/4;top=100 if s==2 else 55
 for i,v in enumerate(a):
  x=int(40+i*(cw+30));d.rectangle((x,top,int(x+cw),h-35),fill=(220,209,183),outline=(79,71,59),width=3);p=Image.open(ROOT/v["staging_path"]).convert("RGBA");p=p.resize((p.width*s,p.height*s),Image.Resampling.NEAREST);im.paste(p,(x+int((cw-p.width)/2),top+int((h-top-75-p.height)/2)),p);d.text((x+10,h-68),f"{v['name']}  {v['logical_cells'][0]}×{v['logical_cells'][1]}",fill=(42,37,31),font=f(18 if s==2 else 9))
 OUT.mkdir(parents=True,exist_ok=True);p=OUT/f"normalized_contact_{w}x{h}.png";im.save(p);return str(p)
def main():print(json.dumps({"contacts":[make(1920,1080),make(960,540)]},ensure_ascii=False))
if __name__=="__main__":main()
