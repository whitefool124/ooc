from pathlib import Path
import json
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
refs=[v for v in json.loads((OUT/'reference_inventory.json').read_text(encoding='utf-8')) if 'ground' in v['group']]
canvas=Image.new('RGB',(1280,1600),(48,48,48));draw=ImageDraw.Draw(canvas)
for i,v in enumerate(refs):
 x=i%4*320;y=i//4*320
 im=Image.open(ROOT/v['reference']).convert('RGBA').resize((288,288),Image.Resampling.NEAREST)
 canvas.paste(im,(x+16,y),im)
 draw.text((x+4,y+292),v['id'].replace('academy_floor_',''),fill='white')
canvas.save(OUT/'ground_batch_reference.png')
