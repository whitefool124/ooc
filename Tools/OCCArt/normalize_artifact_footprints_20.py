#!/usr/bin/env python3
"""Normalize M-A22 independent sources into exact footprint canvases and QA evidence."""
from __future__ import annotations
import hashlib, json
from pathlib import Path
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[2]
CATALOG=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A22/artifact_footprints_20_catalog.json"
MANIFESTS=CATALOG.parent/"manifests"
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def normalize(path,w,h):
    im=Image.open(path).convert("RGBA"); a=im.getchannel("A").point(lambda v:255 if v>=128 else 0); im.putalpha(a); box=a.getbbox()
    if box is None: raise ValueError(f"empty source {path}")
    crop=im.crop(box); scale=min((w-4)/crop.width,(h-4)/crop.height); size=(max(1,round(crop.width*scale)),max(1,round(crop.height*scale)))
    fit=crop.resize(size,Image.Resampling.NEAREST); fa=fit.getchannel("A").point(lambda v:255 if v>=128 else 0)
    rgb=fit.convert("RGB").quantize(colors=12,method=Image.Quantize.MEDIANCUT).convert("RGB"); fit=rgb.convert("RGBA"); fit.putalpha(fa)
    out=Image.new("RGBA",(w,h),(0,0,0,0)); out.alpha_composite(fit,((w-fit.width)//2,(h-fit.height)//2)); return out
def checker(w,h):
    im=Image.new("RGBA",(w,h)); d=ImageDraw.Draw(im)
    for y in range(0,h,8):
      for x in range(0,w,8):
        v=76 if ((x//8)+(y//8))%2 else 122; d.rectangle((x,y,x+7,y+7),fill=(v,v,v,255))
    return im
def main():
    assets=json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    for v in assets:
      w,h=v["delivery_size"]; src=ROOT/v["source_path"]; dst=ROOT/v["staging_path"]; dst.parent.mkdir(parents=True,exist_ok=True)
      im=normalize(src,w,h); dst.parent.mkdir(parents=True,exist_ok=True); im.save(dst); ev=ROOT/f"UnityProject/Artifacts/ArtifactFootprints20/{v['stem']}"
      im.save(ev/"1x.png"); im.resize((w*4,h*4),Image.Resampling.NEAREST).save(ev/"4x.png"); gray=im.convert("LA").convert("RGBA"); gray.putalpha(im.getchannel("A")); gray.save(ev/"grayscale.png")
      board=checker(w*4,h*4); board.alpha_composite(im.resize(board.size,Image.Resampling.NEAREST)); board.save(ev/"checker.png")
      mp=MANIFESTS/f"{v['stem']}.occ-art-manifest-v1.json"; m=json.loads(mp.read_text(encoding="utf-8")); m["provenance"]["source_sha256"]=digest(src); m["delivery"]["output_sha256"]=digest(dst); mp.write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"normalized":len(assets)}))
if __name__=="__main__": main()
