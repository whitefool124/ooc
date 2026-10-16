from pathlib import Path
import json,hashlib,shutil
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
def bind(p):return dict(path=p.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
def medoid(panel):
 src=np.asarray(panel.convert('RGB'));h,w=src.shape[:2];out=np.zeros((32,32,3),np.uint8)
 for y in range(32):
  for x in range(32):
   values=src[int((y+.25)*h/32):int((y+.75)*h/32),int((x+.25)*w/32):int((x+.75)*w/32)].reshape(-1,3)
   mid=np.median(values.astype(float),axis=0);out[y,x]=values[np.argmin(((values.astype(float)-mid)**2).sum(axis=1))]
 return out
results=[]
for row in json.loads((OUT/'ground_batch_sources_r1.json').read_text(encoding='utf-8')):
 folder=OUT/'ground_batch'/row['id'];folder.mkdir(parents=True,exist_ok=True)
 raw=folder/'atlas_raw.png';shutil.copy2(row['source'],raw)
 im=Image.open(raw).convert('RGB');record=dict(id=row['id'],source=bind(raw),size=list(im.size),normal_status='PENDING',surface_status='REJECTED_SEMANTIC_CHANNELS',role=row['role'])
 if im.width!=2*im.height:
  record['normal_status']='REJECTED_ATLAS_ASPECT_OR_PADDING'
 else:
  normal=medoid(im.crop((0,0,im.height,im.height)))
  surface=medoid(im.crop((im.height,0,im.width,im.height)))
  record['normal_min_rgb']=normal.min(axis=(0,1)).tolist();record['surface_max_rgb']=surface.max(axis=(0,1)).tolist()
  if int(normal[:,:,2].min())<190:record['normal_status']='REJECTED_NORMAL_CHANNELS'
  else:
   record['normal_status']='NUMERIC_REVIEW_PENDING';Image.fromarray(normal).save(folder/'normal_candidate.png')
   Image.fromarray(normal).resize((512,512),Image.Resampling.NEAREST).save(folder/'normal_qa16x.png')
  Image.fromarray(surface).save(folder/'surface_rejected.png')
  # No region map may be admitted from a grey beauty image or contaminated RGB channels.
  record['surface_rejection']='Generated surface panel mixes appearance/grey/paint values with declared numerical channels. Regenerate independent categorical labels, encode only explicit semantic roles.'
 results.append(record)
(OUT/'ground_batch_r1_inspection.json').write_text(json.dumps(results,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(total=len(results),normal_numeric_review=sum(r['normal_status']=='NUMERIC_REVIEW_PENDING' for r in results),normal_retry=[r['id'] for r in results if r['normal_status']!='NUMERIC_REVIEW_PENDING'],surface_retry=20)))
