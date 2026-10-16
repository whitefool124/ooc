from pathlib import Path
import json,shutil,hashlib
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
def bind(p):return {'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
def save(p,v):p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
entries=[]
for row in json.loads((OUT/'generation_sources.json').read_text(encoding='utf-8')):
 id=row['id'];folder=OUT/id;folder.mkdir(exist_ok=True)
 raw=folder/'paired_raw.png';shutil.copy2(row['source'],raw)
 if row.get('rejected'):shutil.copy2(row['rejected'],folder/'rejected_anchor_r1.png')
 ref=ROOT/f'UnityProject/Assets/Game/Resources/Art/AcademyCoverCandidates20260924/{id}.png';reference=Image.open(ref);assert reference.size==(32,32)
 image=Image.open(raw).convert('RGBA');assert image.width==image.height and image.width%2==0;half=image.width//2
 if row.get('normal_override'):shutil.copy2(row['normal_override'],folder/'normal_raw_r3.png')
 entry={'id':id,'status':'QA_PENDING','source_albedo':bind(ref),'source_bbox':reference.getchannel('A').getbbox(),'source_raw':bind(raw),'maps':[],'normal_convention':'OpenGL +Y up, no green flip','world_pixel_pitch':1/32,'material_regions':'Nonmetal wood/paper/fabric or pale stone; neutral black metallic/glass region input','human_registration_review':'PENDING'}
 for kind,rect in [('normal',(0,0,half,half)),('roughness',(half,0,image.width,half))]:
  panel=folder/(kind+'_panel.png');image.crop(rect).save(panel)
  if kind=='normal' and row.get('normal_override'):panel=folder/'normal_raw_r3.png'
  src=np.asarray(Image.open(panel).convert('RGBA'));assert src.shape[0]==src.shape[1] and np.all(src[:,:,3]==255);pitch=src.shape[0]
  out=np.zeros((32,32,4),np.uint8);variances=[]
  for y in range(32):
   for x in range(32):
    values=src[int((y+.25)*pitch/32):int((y+.75)*pitch/32),int((x+.25)*pitch/32):int((x+.75)*pitch/32),:3].reshape(-1,3)
    median=np.median(values.astype(float),axis=0);out[y,x,:3]=values[np.argmin(np.sum((values.astype(float)-median)**2,axis=1))];out[y,x,3]=255;variances.append(float(values.std(axis=0).max()))
  if kind=='normal':assert out[:,:,2].min()>190,'Invalid shallow normal component'
  else:assert np.max(out[:,:,:3].max(axis=2)-out[:,:,:3].min(axis=2))<=5 and out[:,:,0].min()>100,'Invalid numeric roughness'
  dest=ROOT/f'UnityProject/Assets/Game/Resources/HD2DMaterialData/Props/{id}/{kind}.png';dest.parent.mkdir(parents=True,exist_ok=True);Image.fromarray(out).save(dest)
  Image.fromarray(out).resize((512,512),Image.Resampling.NEAREST).save(folder/(kind+'_qa16x.png'))
  record={'schema':'occ-registered-prop-material-data-v1','status':'REVIEW_READY','source':bind(panel),'original_raw':bind(raw),'reference':bind(ref),'output':bind(dest),'native_grid':[32,32],'source_macro_pitch':pitch/32,'method':'Known UV canvas central-cell observed RGB medoid; exact quadrant crop, no interpolation/new RGB','source_bbox':entry['source_bbox'],'max_cell_std':max(variances),'human_registration_review':'PENDING','limitation':'Known-field registration is not automatic Sprite1.2 confidence PASS; region alignment and art remain pending'}
  save(folder/(kind+'_registration.json'),record);entry['maps'].append({'kind':kind,'source':bind(panel),'delivery':bind(dest),'registration':bind(folder/(kind+'_registration.json')),'rgb_min':out[:,:,:3].min(axis=(0,1)).tolist(),'rgb_max':out[:,:,:3].max(axis=(0,1)).tolist()})
 save(folder/'material_provenance.json',entry);entries.append(entry);print(id,[(m['kind'],m['rgb_min'],m['rgb_max']) for m in entry['maps']])
save(OUT/'material_inventory.json',{'schema':'occ-actual-cover-material-inventory-v1','status':'QA_PENDING','assets':entries})
