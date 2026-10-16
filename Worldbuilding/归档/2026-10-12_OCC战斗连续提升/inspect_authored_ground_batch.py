from pathlib import Path
import json,hashlib
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
records=json.loads((OUT/'ground_authored_batch_r1/material_batch_result.json').read_text(encoding='utf-8'))
canvas=Image.new('RGB',(960,1600),(48,48,48));draw=ImageDraw.Draw(canvas);results=[]
for i,item in enumerate(records):
 ident=item['asset_id'];row=i//2;col=i%2;folder=OUT/'ground_authored_batch_r1/fields'/ident
 paths=[]
 for j,kind in enumerate(['normal','surface']):
  path=folder/(kind+'.png');repeat=OUT/'ground_authored_batch_repeat/fields'/ident/(kind+'.png')
  assert path.read_bytes()==repeat.read_bytes(),'Nondeterministic bake '+ident
  im=Image.open(path);assert im.size==(32,32)
  canvas.paste(im.resize((144,144),Image.Resampling.NEAREST),(col*480+160*(j+1),row*160))
  paths.append(dict(kind=kind,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
 source=ROOT/item['normal']['albedo_reference']['path']
 assert hashlib.sha256(source.read_bytes()).hexdigest()==item['normal']['albedo_reference']['sha256']
 im=Image.open(source).convert('RGBA').resize((144,144),Image.Resampling.NEAREST)
 canvas.paste(im,(col*480,row*160),im)
 draw.text((col*480,row*160+145),ident.replace('academy_floor_',''),fill='white')
 results.append(dict(id=ident,bake_repeat_identical=True,source_art_unchanged=True,fields=paths,human_alignment='PENDING'))
canvas.save(OUT/'authored_ground_batch_source_fields.png')
(OUT/'authored_ground_batch_integrity.json').write_text(json.dumps(dict(schema='occ-ground-batch-integrity-v1',status='QA_PENDING',assets=results),indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(assets=len(records),numeric_fields=40,repeat_identical=True,albedos_unchanged=True)))
