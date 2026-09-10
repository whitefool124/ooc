"""Assemble unaltered Unity capture crops and verify changes stay in the board viewport."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',15)
sheet=Image.new('RGB',(1000,920),(22,29,32)); draw=ImageDraw.Draw(sheet)
draw.text((24,16),'战棋遮挡可读性 · 生产界面前后对照',font=font,fill='#e0ded0')
draw.text((24,48),'左：修改前    右：只补被遮挡的边缘；血条和护盾条增加所属单位点选入口',font=small,fill='#c2c9c5')
for col,kind in enumerate(('before','after')):
    x=24+col*490
    im=Image.open(HERE/f'{kind}_overview_960.png').convert('RGB')
    sheet.paste(im.crop((164,86,556,380)),(x,112))
    draw.text((x,84),'960×540 全场 · 原生截图裁切',font=small,fill='#c2c9c5')
    im=Image.open(HERE/f'{kind}_zoom_1920.png').convert('RGB')
    sheet.paste(im.crop((554,232,1014,596)),(x,466))
    draw.text((x,438),'1920×1080 放大档 · 原生截图裁切',font=small,fill='#c2c9c5')
draw.text((24,850),'友方冷青、敌方封存红；细轮廓不拦截点击，保留角色原图尺寸和足底深度。',font=small,fill='#c2c9c5')
draw.text((24,880),'7组布局 / 42个资源条入口已检查；动态实机与人工审美仍待继续验收。',font=small,fill='#c2c9c5')
sheet.save(HERE/'review-board.png')
diffs=[]
for width in (960,1920):
    for mode in ('overview','zoom'):
        before=np.array(Image.open(HERE/f'before_{mode}_{width}.png').convert('RGB'))
        after=np.array(Image.open(HERE/f'after_{mode}_{width}.png').convert('RGB'))
        changed=np.any(before!=after,axis=2)
        ys,xs=np.where(changed)
        assert len(xs)>0
        # Reference battlefield region excludes header, HUD and bottom command bar.
        scale=width/1920
        outside=changed.copy()
        outside[int(80*scale):int(848*scale),int(16*scale):int(1440*scale)]=False
        assert not outside.any(), 'Unexpected change outside tactical viewport'
        diffs.append({'width':width,'mode':mode,'changed_pixels':int(changed.sum()),
                      'bbox':[int(xs.min()),int(ys.min()),int(xs.max())+1,int(ys.max())+1],
                      'outside_viewport_changed_pixels':int(outside.sum())})
old=json.loads((ROOT/'Worldbuilding/归档/2026-09-05_寻迹兽轮廓返修/final-state.json').read_text(encoding='utf-8'))
assert all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==h for p,h in old['formal_hashes'].items())
(HERE/'image-diff-and-assets.json').write_text(json.dumps({'differences':diffs,'formal_png_and_meta_hashes_unchanged':True},indent=2)+'\n',encoding='utf-8')
print(json.dumps(diffs))
