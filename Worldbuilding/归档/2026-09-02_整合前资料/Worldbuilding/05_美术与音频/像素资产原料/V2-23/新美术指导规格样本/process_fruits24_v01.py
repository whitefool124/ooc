"""Create an exact 24x24 fruit resolution comparison from the approved direction."""
from __future__ import annotations
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

SOURCE=Path(r"C:\Users\FNHF\.codex\generated_images\019ff714-5e0a-7ed0-8cc2-b42cf7aa433c\exec-5a68f502-d2ae-4e48-a53f-6e5fc3452ed3.png")
ROOT=Path(__file__).parent; OUT=ROOT/'Items24'/'Fruits_v01'; QA=ROOT/'QA'
OUT.mkdir(parents=True,exist_ok=True); QA.mkdir(exist_ok=True)
REGIONS={'apple':(45,265,365,805),'citrus':(390,305,700,800),'plum':(705,255,1035,805),'pear':(1035,185,1370,805),'blueberries':(1370,250,1690,800),'strawberry':(1710,245,2010,800)}

def remove_white(im):
 im=im.convert('RGBA'); px=im.load()
 for y in range(im.height):
  for x in range(im.width):
   r,g,b,a=px[x,y]
   if min(r,g,b)>242 and max(r,g,b)-min(r,g,b)<12: px[x,y]=(0,0,0,0)
 return im
def convert(region,src):
 raw=remove_white(src.crop(region)); raw=raw.crop(raw.getbbox()); raw.thumbnail((22,22),Image.Resampling.BOX)
 rgb=raw.convert('RGB').quantize(colors=10,method=Image.Quantize.MEDIANCUT).convert('RGBA'); out=Image.new('RGBA',raw.size,(0,0,0,0)); out.paste(rgb,mask=raw.getchannel('A'))
 cell=Image.new('RGBA',(24,24),(0,0,0,0)); cell.alpha_composite(out,((24-out.width)//2,(24-out.height)//2)); return cell
def main():
 src=Image.open(SOURCE); items=[]; report=[]
 for name,region in REGIONS.items():
  im=convert(region,src); path=OUT/f'occ_fruit_{name}_24_v01.png'; im.save(path); items.append((name,im)); report.append({'file':path.name,'size':[24,24],'opaque_colors':len({p[:3] for p in im.getdata() if p[3]}),'hard_alpha':True})
 scale,cw,h=12,210,350; board=Image.new('RGBA',(cw*len(items),h),'#16161c'); d=ImageDraw.Draw(board); f=ImageFont.load_default()
 for i,(name,im) in enumerate(items):
  x=i*cw; d.text((x+10,10),name,fill='#e9e5dc',font=f); d.text((x+10,28),'native 24x24 / V01',fill='#aaa7b1',font=f); ox=x+92
  for yy in range(24):
   for xx in range(24): d.point((ox+xx,58+yy),fill='#393943' if (xx+yy)%2 else '#2b2b34')
  board.alpha_composite(im,(ox,58)); board.alpha_composite(im.resize((24*scale,24*scale),Image.Resampling.NEAREST),(x-39,95))
 board.save(QA/'fruits24_v01_overview.png'); (QA/'fruits24_v01_report.json').write_text(json.dumps(report,indent=2),encoding='utf8')
if __name__=='__main__': main()
