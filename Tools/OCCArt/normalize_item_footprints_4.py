#!/usr/bin/env python3
from __future__ import annotations
import hashlib,json
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2];CAT=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A23/item_footprints_4_catalog.json";M=CAT.parent/"manifests"
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def norm(p,w,hg):
 im=Image.open(p).convert("RGBA");a=im.getchannel("A").point(lambda x:255 if x>=128 else 0);im.putalpha(a);b=a.getbbox();crop=im.crop(b);s=min((w-4)/crop.width,(hg-4)/crop.height);fit=crop.resize((max(1,round(crop.width*s)),max(1,round(crop.height*s))),Image.Resampling.NEAREST);fa=fit.getchannel("A").point(lambda x:255 if x>=128 else 0);rgb=fit.convert("RGB").quantize(colors=12,method=Image.Quantize.MEDIANCUT).convert("RGBA");rgb.putalpha(fa);out=Image.new("RGBA",(w,hg));out.alpha_composite(rgb,((w-rgb.width)//2,(hg-rgb.height)//2));return out
def board(w,hg):
 im=Image.new("RGBA",(w,hg));d=ImageDraw.Draw(im)
 for y in range(0,hg,8):
  for x in range(0,w,8):v=76 if((x//8+y//8)%2)else 122;d.rectangle((x,y,x+7,y+7),fill=(v,v,v,255))
 return im
def main():
 a=json.loads(CAT.read_text(encoding="utf-8"))["assets"]
 for v in a:
  w,hg=v["delivery_size"];src=ROOT/v["source_path"];dst=ROOT/v["staging_path"];dst.parent.mkdir(parents=True,exist_ok=True);im=norm(src,w,hg);im.save(dst);e=ROOT/f"UnityProject/Artifacts/ItemFootprints4/{v['stem']}";im.save(e/"1x.png");im.resize((w*4,hg*4),Image.Resampling.NEAREST).save(e/"4x.png");g=im.convert("LA").convert("RGBA");g.putalpha(im.getchannel("A"));g.save(e/"grayscale.png");b=board(w*4,hg*4);b.alpha_composite(im.resize(b.size,Image.Resampling.NEAREST));b.save(e/"checker.png");mp=M/f"{v['stem']}.occ-art-manifest-v1.json";m=json.loads(mp.read_text(encoding="utf-8"));m["provenance"]["source_sha256"]=h(src);m["delivery"]["output_sha256"]=h(dst);mp.write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
 print(json.dumps({"normalized":len(a)}))
if __name__=="__main__":main()
