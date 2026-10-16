"""Registered numeric fields: observed colours only, never albedo-derived data."""
from pathlib import Path
import hashlib,json,shutil
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
GEN=Path('C:/Users/FNHF/.codex/generated_images/01a12f14-7a73-7871-b3db-2c160edcd4d0')
ROWS=[
 ('academy_light_wood_bench_intact','exec-d291c8f8-e57c-4d63-978e-ef2f13c3e200.png','exec-2bd8518c-1a88-4275-bfb2-fdae62852c8f.png','satin timber seat, rough timber legs'),
 ('academy_light_training_pad_intact','exec-39584b67-75c7-439b-adbd-02bbfec63aa0.png','exec-400b26f4-95c3-4a02-afd0-613913114229.png','matte cloth pad, timber support'),
 ('academy_light_planter_cover_intact','exec-0e4e74c9-7377-4d7a-a31f-2785a7273983.png','exec-45a814e5-852e-4d96-b535-15b0e4766152.png','rough foliage and soil, timber rim, clay panels')]
def bind(p):return dict(path=p.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
def save(p,v):p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
entries=[]
for ident,norm,rough,semantics in ROWS:
 folder=OUT/ident;folder.mkdir(exist_ok=True)
 ref=ROOT/f'UnityProject/Assets/Game/Resources/Art/AcademyCoverCandidates20260924/{ident}.png'
 reference=Image.open(ref);w,h=reference.size
 entry=dict(id=ident,status='QA_PENDING',source_albedo=bind(ref),source_bbox=reference.getchannel('A').getbbox(),material_regions=semantics,world_pixel_pitch=1/32,normal_convention='OpenGL +Y, no green flip',human_registration_review='PENDING',maps=[])
 for kind,filename in [('normal',norm),('roughness',rough)]:
  raw=folder/(kind+'_raw.png');shutil.copy2(GEN/filename,raw)
  im=Image.open(raw).convert('RGBA');src=np.asarray(im)
  assert im.width*h==im.height*w,'Wrong canvas aspect; do not distort'
  assert np.all(src[:,:,3]==255),'Opaque numeric input required'
  out=np.zeros((h,w,4),np.uint8);variance=[]
  for y in range(h):
   for x in range(w):
    values=src[int((y+.25)*im.height/h):int((y+.75)*im.height/h),int((x+.25)*im.width/w):int((x+.75)*im.width/w),:3].reshape(-1,3)
    mid=np.median(values.astype(float),axis=0)
    out[y,x,:3]=values[np.argmin(np.sum((values.astype(float)-mid)**2,axis=1))];out[y,x,3]=255
    variance.append(float(values.std(axis=0).max()))
  if kind=='normal':assert out[:,:,2].min()>=190,'Invalid shallow normal blue channel'
  else:assert np.max(out[:,:,:3].max(axis=2)-out[:,:,:3].min(axis=2))<=5,'Roughness must be grayscale'
  dest=ROOT/f'UnityProject/Assets/Game/Resources/HD2DMaterialData/Props/{ident}/{kind}.png'
  dest.parent.mkdir(parents=True,exist_ok=True);Image.fromarray(out).save(dest)
  Image.fromarray(out).resize((w*16,h*16),Image.Resampling.NEAREST).save(folder/(kind+'_qa16x.png'))
  record=dict(schema='occ-registered-prop-material-data-v1',status='REVIEW_READY',source=bind(raw),reference=bind(ref),output=bind(dest),native_grid=[w,h],source_macro_pitch=im.width/w,method='Known UV central-cell observed RGB medoid, no geometric interpolation, new RGB, albedo inference or recolouring',max_cell_std=max(variance),human_registration_review='PENDING',limitation='Numeric-field registration is not Sprite grid-confidence PASS; exact region boundaries and material review remain pending')
  save(folder/(kind+'_registration.json'),record)
  entry['maps'].append(dict(kind=kind,source=bind(raw),delivery=bind(dest),registration=bind(folder/(kind+'_registration.json')),rgb_min=out[:,:,:3].min(axis=(0,1)).tolist(),rgb_max=out[:,:,:3].max(axis=(0,1)).tolist()))
 save(folder/'material_provenance.json',entry);entries.append(entry)
save(OUT/'material_inventory.json',dict(schema='occ-continuous-battle-material-batch-v1',status='QA_PENDING',assets=entries))
save(OUT/'batch_state.json',dict(status='decoded',completed=[e['id'] for e in entries],next='import and local material response; exact source region alignment review pending'))
print(json.dumps(dict(materials=len(entries),maps=sum(len(e['maps']) for e in entries),status='QA_PENDING')))
