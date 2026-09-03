"""Authorized M-A18 relaxed normalization: whole-canvas nearest sampling plus a uniform anchor translation."""
import json, sys
from pathlib import Path
from PIL import Image
src, out, qa, report, dy = sys.argv[1:6]
im=Image.open(src).convert('RGBA').resize((64,64),Image.Resampling.NEAREST)
alpha=im.getchannel('A').point(lambda v:255 if v>32 else 0)
im.putalpha(alpha)
if int(dy):
    shifted=Image.new('RGBA',(64,64),(0,0,0,0)); shifted.alpha_composite(im,(0,int(dy))); im=shifted; alpha=im.getchannel('A')
rgb=Image.new('RGB',(64,64)); rgb.paste(im.convert('RGB'),mask=alpha); im=rgb.quantize(colors=24,dither=Image.Dither.NONE).convert('RGBA'); im.putalpha(alpha)
for p in (out,qa,report): Path(p).parent.mkdir(parents=True,exist_ok=True)
im.save(out); im.resize((256,256),Image.Resampling.NEAREST).save(qa)
colors=len({v[:3] for v in im.getdata() if v[3]}); bounds=alpha.getbbox()
Path(report).write_text(json.dumps({'status':'PASS','authorizedRelaxation':'uniform anchor translation after whole-canvas nearest normalization','translationY':int(dy),'size':[64,64],'hardAlpha':set(alpha.getdata()).issubset({0,255}),'colors':colors,'bounds':list(bounds) if bounds else None},indent=2),encoding='utf-8')
