"""Native 16x16 fruit studies, drawn after rejecting automatic downsampling."""
from __future__ import annotations

import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).parent; OUT=ROOT/'Items16'/'Fruits_v02'; QA=ROOT/'QA'
OUT.mkdir(parents=True,exist_ok=True); QA.mkdir(exist_ok=True)
C={
'K':'#171419',
'a':'#6d1e28','A':'#b52c2e','r':'#e84a30','R':'#ff8050','g':'#2c682d','G':'#5ca63b',
'o':'#9a4219','O':'#dc691a','p':'#ff9b25','P':'#ffd45a',
'v':'#492159','V':'#753b8f','l':'#b963c0','L':'#e8a0df',
'y':'#8d6515','Y':'#d49b1a','q':'#f6c62c','Q':'#fff078',
'b':'#15315c','B':'#285b9c','u':'#4b88cb','U':'#8dc4f2',
's':'#8d2025','S':'#cc3430','t':'#f45436','T':'#ff8c5a'
}
G={
'apple':[
'................','.......Kg.......','......KGGK......','.....KAAA K......'.replace(' ',''),
'....KArrrAK.....','...KArrRrrK.....','...KArrRrrK.....','...KArrrrrK.....','...KArrrrrK.....','....KArrrK......','.....KAAA K......'.replace(' ',''),'......KKK.......','................','................','................','................'],
'citrus':[
'................','................','......KKKK......','....KKOOOOKK....',
'...KOOpppOOK....','..KOOpPpppOK....','..KOpPPOppOK....','..KOpPpPppOK....','..KOpppPpOK.....','...KOOppOK......','....KOOOOK......','.....KKKK.......','................','................','................','................'],
'plum':[
'................','.......K........','......KvK.......','.....KVVVK......','....KVVllVK.....','...KVVllLVK.....','...KVVllLVK.....','...KVVllVK......','...KVVVVVK......','....KVVVK.......','.....KVVK.......','......KK........','................','................','................','................'],
'pear':[
'................','.......Kg.......','......KGGK......','......KYYK......','.....KYqqYK.....','....KYqQqYK.....','....KYqQqYK.....','...KYqqqqYK.....','...KYqqqqYK.....','....KYqqYK......','.....KYYK.......','......KK........','................','................','................','................'],
'blueberries':[
'................','........Kg......','.......KGGK.....','......KKK.......','....KBBK........','...KBuBK.KBBK...','...KBuBKKBuBK...','...KBBBKKBuuBK..','....KBBK.KBBK...','.....KK...KK....','................','................','................','................','................','................'],
'strawberry':[
'................','......KGGK......','.....KGGGGK.....','....KSSSSSK.....','...KSStttSK.....','...KSStTtSK.....','...KSttTtSK.....','...KSStttSK.....','....KSttSK......','.....KSSK.......','......KK........','................','................','................','................','................']}

def make(rows):
    if len(rows)!=16 or any(len(r)!=16 for r in rows): raise ValueError([(len(r),r) for r in rows])
    im=Image.new('RGBA',(16,16),(0,0,0,0)); px=im.load()
    for y,row in enumerate(rows):
      for x,ch in enumerate(row):
       if ch!='.': px[x,y]=(*bytes.fromhex(C[ch][1:]),255)
    return im
def main():
 items=[]; report=[]
 for name,rows in G.items():
  im=make(rows); path=OUT/f'occ_fruit_{name}_16_v02.png'; im.save(path); items.append((name,im))
  report.append({'file':path.name,'size':[16,16],'opaque_colors':len({p[:3] for p in im.getdata() if p[3]}),'hard_alpha':True})
 scale,cw,h=14,200,320; board=Image.new('RGBA',(cw*len(items),h),'#16161c'); d=ImageDraw.Draw(board); f=ImageFont.load_default()
 for i,(name,im) in enumerate(items):
  x=i*cw; d.text((x+10,10),name,fill='#e9e5dc',font=f); d.text((x+10,28),'native 16x16 / V02',fill='#aaa7b1',font=f)
  ox=x+92
  for yy in range(16):
   for xx in range(16): d.point((ox+xx,57+yy),fill='#393943' if (xx+yy)%2 else '#2b2b34')
  board.alpha_composite(im,(ox,57)); board.alpha_composite(im.resize((224,224),Image.Resampling.NEAREST),(x-12,85))
 board.save(QA/'fruits16_v02_overview.png'); (QA/'fruits16_v02_report.json').write_text(json.dumps(report,indent=2),encoding='utf8')
if __name__=='__main__': main()
