#!/usr/bin/env python3
from __future__ import annotations
import hashlib,json
from pathlib import Path
from PIL import Image,ImageDraw

ROOT=Path(__file__).resolve().parents[2]
CAT=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A25/combat_semantic_polish_10_catalog.json"
MANIFESTS=CAT.parent/"manifests"
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def normalize(path,w,h,palette):
 im=Image.open(path).convert("RGBA");a=im.getchannel("A").point(lambda v:255 if v>=128 else 0);im.putalpha(a);box=a.getbbox()
 if box is None:raise ValueError(f"empty source: {path}")
 crop=im.crop(box);border=1 if w==16 else 2;scale=min((w-border*2)/crop.width,(h-border*2)/crop.height)
 fit=crop.resize((max(1,round(crop.width*scale)),max(1,round(crop.height*scale))),Image.Resampling.NEAREST)
 fa=fit.getchannel("A").point(lambda v:255 if v>=128 else 0);fit=fit.convert("RGB").quantize(colors=palette,method=Image.Quantize.MEDIANCUT).convert("RGBA");fit.putalpha(fa)
 out=Image.new("RGBA",(w,h),(0,0,0,0));out.alpha_composite(fit,((w-fit.width)//2,(h-fit.height)//2));return out
def checker(w,h):
 im=Image.new("RGBA",(w,h));d=ImageDraw.Draw(im);cell=4 if w<=64 else 8
 for y in range(0,h,cell):
  for x in range(0,w,cell):
   v=76 if((x//cell+y//cell)%2)else 122;d.rectangle((x,y,x+cell-1,y+cell-1),fill=(v,v,v,255))
 return im
def main():
 assets=json.loads(CAT.read_text(encoding="utf-8"))["assets"]
 for asset in assets:
  w,h=asset["delivery_size"];src=ROOT/asset["source_path"];dst=ROOT/asset["staging_path"];dst.parent.mkdir(parents=True,exist_ok=True)
  im=normalize(src,w,h,asset["palette_max"]);im.save(dst);e=src.parent;im.save(e/"1x.png");large=im.resize((w*4,h*4),Image.Resampling.NEAREST);large.save(e/"4x.png")
  gray=im.convert("LA").convert("RGBA");gray.putalpha(im.getchannel("A"));gray.save(e/"grayscale.png");board=checker(w*4,h*4);board.alpha_composite(large);board.save(e/"checker.png")
  mp=MANIFESTS/f"{asset['group']}_{asset['stem']}.occ-art-manifest-v1.json";m=json.loads(mp.read_text(encoding="utf-8"));m["provenance"]["source_sha256"]=sha(src);m["delivery"]["output_sha256"]=sha(dst);mp.write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
 print(json.dumps({"normalized":len(assets)}))
if __name__=="__main__":main()
