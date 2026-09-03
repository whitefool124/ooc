#!/usr/bin/env python3
"""Build M-A22 normalized contact sheets."""
from __future__ import annotations
import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
CATALOG=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A22/artifact_footprints_20_catalog.json"
OUT=ROOT/"UnityProject/Artifacts/ArtifactFootprints20/contacts"
def font(n):
 p=Path("C:/Windows/Fonts/msyh.ttc"); return ImageFont.truetype(str(p),n) if p.exists() else ImageFont.load_default()
def build(w,h):
 a=json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]; im=Image.new("RGB",(w,h),(31,29,26)); d=ImageDraw.Draw(im); mx,top,g=w*.025,h*.09,w*.006; cw=(w-2*mx-4*g)/5; ch=(h-top-h*.03-3*g)/4; s=2 if w>=1600 else 1
 d.text((mx,18),"OCC M-A22 法宝背包占格 · 20 件",fill=(233,220,190),font=font(28 if s==2 else 15))
 for i,v in enumerate(a):
  c,r=i%5,i//5; x=int(mx+c*(cw+g)); y=int(top+r*(ch+g)); d.rectangle((x,y,int(x+cw),int(y+ch)),fill=(220,209,183),outline=(79,71,59),width=3)
  icon=Image.open(ROOT/v["staging_path"]).convert("RGBA"); sc=icon.resize((icon.width*s,icon.height*s),Image.Resampling.NEAREST); maxw,maxh=int(cw-16),int(ch-38)
  if sc.width>maxw or sc.height>maxh:
   q=min(maxw/sc.width,maxh/sc.height); sc=icon.resize((max(1,round(icon.width*q)),max(1,round(icon.height*q))),Image.Resampling.NEAREST)
  im.paste(sc,(x+int((cw-sc.width)/2),y+6+int((ch-40-sc.height)/2)),sc); cells=v["logical_cells"]; d.text((x+8,int(y+ch-28)),f"{v['stem']}  {cells[0]}×{cells[1]}",fill=(38,34,29),font=font(13 if s==2 else 7))
 OUT.mkdir(parents=True,exist_ok=True); p=OUT/f"normalized_contact_{w}x{h}.png"; im.save(p); return str(p)
def main(): print(json.dumps({"contacts":[build(1920,1080),build(960,540)]},ensure_ascii=False))
if __name__=="__main__": main()
