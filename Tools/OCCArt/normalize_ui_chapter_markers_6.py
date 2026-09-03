#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2];M=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A31";A=ROOT/"UnityProject/Artifacts/UiChapterMarkers6";S=ROOT/"UnityProject/Assets/Game/Resources/Art/ValidationUIChapterMarkers";STEMS=["teaching_chalk_clip","workshop_caliper_clip","infirmary_bandage_clip","field_leaf_clip","sealed_red_clip","reward_brass_tag"]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def norm(p):
 im=Image.open(p).convert("RGBA");a=im.getchannel("A").point(lambda v:255 if v>=32 else 0);b=a.getbbox()
 if not b:raise RuntimeError("empty "+str(p))
 im.putalpha(a);im=im.crop(b);q=min(28/im.width,28/im.height);z=(max(1,round(im.width*q)),max(1,round(im.height*q)));im=im.resize(z,Image.Resampling.BOX);ha=im.getchannel("A").point(lambda v:255 if v>=96 else 0);im=im.quantize(colors=10,method=Image.Quantize.FASTOCTREE,dither=Image.Dither.NONE).convert("RGBA");im.putalpha(ha);o=Image.new("RGBA",(32,32),(0,0,0,0));o.alpha_composite(im,((32-z[0])//2,(32-z[1])//2));return o
def checker(im):
 e=im.resize((128,128),Image.Resampling.NEAREST);o=Image.new("RGBA",e.size);d=ImageDraw.Draw(o)
 for y in range(0,128,8):
  for x in range(0,128,8):v=226 if (x//8+y//8)%2==0 else 178;d.rectangle((x,y,x+7,y+7),fill=(v,v,v,255))
 o.alpha_composite(e);return o
def main():
 S.mkdir(parents=True,exist_ok=True);ims={}
 for stem in STEMS:
  f=A/stem;im=norm(f/"source.png");ims[stem]=im;out=S/f"{stem}.png";im.save(out);im.save(f/"1x.png");im.resize((64,64),Image.Resampling.NEAREST).save(f/"2x.png");im.resize((128,128),Image.Resampling.NEAREST).save(f/"4x.png");g=im.convert("L");Image.merge("RGBA",(g,g,g,im.getchannel("A"))).save(f/"grayscale.png");checker(im).save(f/"checker.png");mp=M/"manifests"/f"marker_{stem}.occ-art-manifest-v1.json";m=json.loads(mp.read_text(encoding="utf-8-sig"));m["provenance"]["source_sha256"]=sha(f/"source.png");m["delivery"]["output_sha256"]=sha(out);mp.write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
 c=A/"contacts";c.mkdir(parents=True,exist_ok=True);sh=Image.new("RGB",(768,420),(42,40,35));d=ImageDraw.Draw(sh)
 for i,(stem,im) in enumerate(ims.items()):x,y=(i%3)*256,(i//3)*210;sh.paste(checker(im).convert("RGB"),(x+64,y+38));d.text((x+8,y+8),stem,fill=(242,235,221))
 sh.save(c/"ui_chapter_markers_6_review.png");print(json.dumps({"status":"PASS","normalized":6}))
if __name__=="__main__":main()
