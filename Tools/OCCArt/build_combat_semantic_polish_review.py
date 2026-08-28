#!/usr/bin/env python3
from __future__ import annotations
import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[2]
CAT=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A25/combat_semantic_polish_10_catalog.json"
OUT=ROOT/"UnityProject/Artifacts/CombatSemanticPolish10/contacts/offline_before_after_1920x1080.png"
def font(size):
 for p in (Path("C:/Windows/Fonts/consola.ttf"),Path("C:/Windows/Fonts/arial.ttf")):
  if p.exists():return ImageFont.truetype(str(p),size)
 return ImageFont.load_default()
def main():
 assets=json.loads(CAT.read_text(encoding="utf-8"))["assets"];canvas=Image.new("RGB",(1920,1080),"#0e1519");d=ImageDraw.Draw(canvas)
 title,sub,label=font(30),font(18),font(13);d.text((52,28),"OCC M-A25 — TARGETED COMBAT SEMANTIC POLISH",fill="#eee6d1",font=title)
 d.text((52,76),"BEFORE (M-A24)",fill="#a9946a",font=sub);d.text((985,76),"AFTER (M-A25 CANDIDATE)",fill="#c9a756",font=sub)
 for side,before in ((0,True),(1,False)):
  xbase=52+side*933
  for i,a in enumerate(assets):
   row,col=divmod(i,5);x=xbase+col*174;y=116+row*250
   d.rounded_rectangle((x,y,x+156,y+210),radius=7,fill="#1c282d",outline="#405158",width=2)
   path=(ROOT/a["source_path"]).parent/"formal_before.png" if before else ROOT/a["staging_path"]
   im=Image.open(path).convert("RGBA");factor=4 if a["delivery_size"][0]==16 else 3;large=im.resize((im.width*factor,im.height*factor),Image.Resampling.NEAREST)
   canvas.paste(large,(x+(156-large.width)//2,y+24),large)
   gray=im.convert("LA").convert("RGBA");gray.putalpha(im.getchannel("A"));gray=gray.resize(large.size,Image.Resampling.NEAREST);canvas.paste(gray,(x+(156-gray.width)//2,y+118),gray)
   box=d.textbbox((0,0),a["stem"],font=label);d.text((x+(156-box[2]+box[0])//2,y+190),a["stem"],fill="#d8d2c4",font=label)
 OUT.parent.mkdir(parents=True,exist_ok=True);canvas.save(OUT);print(OUT)
if __name__=="__main__":main()
