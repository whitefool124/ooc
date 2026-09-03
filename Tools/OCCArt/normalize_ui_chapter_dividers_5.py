#!/usr/bin/env python3
from __future__ import annotations
import hashlib,json
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]; M30=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A30"; ART=ROOT/"UnityProject/Artifacts/UiChapterDividers5"; STAGE=ROOT/"UnityProject/Assets/Game/Resources/Art/ValidationUIChapterDividers"
STEMS=["teaching_record","workshop_record","infirmary_record","field_survey","sealed_dossier"]
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def norm(src):
 im=Image.open(src).convert("RGBA"); a=im.getchannel("A").point(lambda v:255 if v>=32 else 0); b=a.getbbox()
 if not b:raise RuntimeError("no visible content "+str(src))
 im.putalpha(a);im=im.crop(b);s=min(124/im.width,28/im.height);z=(max(1,round(im.width*s)),max(1,round(im.height*s)));im=im.resize(z,Image.Resampling.BOX);ha=im.getchannel("A").point(lambda v:255 if v>=96 else 0);im=im.quantize(colors=12,method=Image.Quantize.FASTOCTREE,dither=Image.Dither.NONE).convert("RGBA");im.putalpha(ha);out=Image.new("RGBA",(128,32),(0,0,0,0));out.alpha_composite(im,((128-z[0])//2,(32-z[1])//2));return out
def check(im,scale=4):
 e=im.resize((im.width*scale,im.height*scale),Image.Resampling.NEAREST);o=Image.new("RGBA",e.size);d=ImageDraw.Draw(o)
 for y in range(0,o.height,8):
  for x in range(0,o.width,8):v=226 if (x//8+y//8)%2==0 else 178;d.rectangle((x,y,x+7,y+7),fill=(v,v,v,255))
 o.alpha_composite(e);return o
def main():
 STAGE.mkdir(parents=True,exist_ok=True);ims={}
 for stem in STEMS:
  f=ART/stem;src=f/"source.png";im=norm(src);ims[stem]=im;out=STAGE/f"{stem}.png";im.save(out);im.save(f/"1x.png");im.resize((256,64),Image.Resampling.NEAREST).save(f/"2x.png");im.resize((512,128),Image.Resampling.NEAREST).save(f/"4x.png");g=im.convert("L");Image.merge("RGBA",(g,g,g,im.getchannel("A"))).save(f/"grayscale.png");check(im).save(f/"checker.png");mp=M30/"manifests"/f"divider_{stem}.occ-art-manifest-v1.json";m=json.loads(mp.read_text(encoding="utf-8-sig"));m["provenance"]["source_sha256"]=digest(src);m["delivery"]["output_sha256"]=digest(out);mp.write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
 c=ART/"contacts";c.mkdir(parents=True,exist_ok=True);sheet=Image.new("RGB",(1040,500),(42,40,35));d=ImageDraw.Draw(sheet)
 for i,(stem,im) in enumerate(ims.items()):x,y=(i%2)*520,(i//2)*165;pv=check(im).convert("RGB");sheet.paste(pv,(x+4,y+30));d.text((x+8,y+8),stem,fill=(242,235,221))
 sheet.save(c/"ui_chapter_dividers_5_review.png");print(json.dumps({"status":"PASS","normalized":5}))
if __name__=="__main__":main()
