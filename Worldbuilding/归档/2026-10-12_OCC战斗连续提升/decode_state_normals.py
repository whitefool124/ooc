from pathlib import Path
import json,hashlib,shutil
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
def bind(p):return dict(path=p.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
refs={r['id']:r for r in json.loads((OUT/'reference_inventory.json').read_text(encoding='utf-8'))}
rows=list({r['id']:r for f in sorted(OUT.glob('state_generation_sources*.json')) for r in json.loads(f.read_text(encoding='utf-8'))}.values());results=[]
for row in rows:
 ident=row['id'];folder=OUT/ident;folder.mkdir(exist_ok=True)
 raw=folder/('normal_raw.png' if row.get('kind')=='normal' else 'paired_raw.png');shutil.copy2(row['source'],raw)
 if ident=='academy_light_planter_damaged' and row.get('kind')!='normal':
  results.append(dict(id=ident,status='REJECTED',reason='Black atlas frame; retry the normal field without border'));continue
 im=Image.open(raw).convert('RGBA')
 if row.get('kind')=='normal':
  w,h=refs[ident]['dimensions'];assert im.width*h==im.height*w,'Wrong registered canvas aspect';panel=im
 else:
  assert im.width==im.height*2,'Wrong atlas aspect';panel=im.crop((0,0,im.height,im.height))
 panel.save(folder/'normal_panel.png')
 src=np.asarray(panel);w,h=refs[ident]['dimensions'];out=np.zeros((h,w,4),np.uint8)
 for y in range(h):
  for x in range(w):
   values=src[int((y+.25)*panel.height/h):int((y+.75)*panel.height/h),int((x+.25)*panel.width/w):int((x+.75)*panel.width/w),:3].reshape(-1,3)
   mid=np.median(values.astype(float),axis=0)
   out[y,x,:3]=values[np.argmin(((values.astype(float)-mid)**2).sum(axis=1))];out[y,x,3]=255
 assert out[:,:,2].min()>=190,'Invalid normal blue channel'
 dest=ROOT/f'UnityProject/Assets/Game/Resources/HD2DMaterialData/Props/{ident}/normal.png';dest.parent.mkdir(parents=True,exist_ok=True)
 Image.fromarray(out).save(dest);Image.fromarray(out).resize((w*16,h*16),Image.Resampling.NEAREST).save(folder/'normal_qa16x.png')
 entry=dict(schema='occ-registered-prop-material-data-v1',id=ident,status='QA_PENDING',source=bind(raw),panel=bind(folder/'normal_panel.png'),source_albedo=refs[ident]['source'],normal=bind(dest),method='Independent built-in model field, exact full canvas or left atlas panel, observed RGB medoid on registered native UV; no albedo-derived height',native_grid=[w,h],world_pixel_pitch=1/32,normal_convention='OpenGL +Y, no green flip',uniform_surface=dict(roughness=row.get('roughness',.88),metallic=0,coat=0,reason='Explicit semantic material role, not grayscale conversion'),human_registration_review='PENDING',limitation='Shared UV candidate; source/region aesthetics and whole-scene density pending')
 (folder/'material_provenance.json').write_text(json.dumps(entry,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');results.append(entry)
(OUT/'state_material_inventory.json').write_text(json.dumps(results,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(decoded=sum(r['status']=='QA_PENDING' for r in results),rejected=sum(r['status']=='REJECTED' for r in results))))
