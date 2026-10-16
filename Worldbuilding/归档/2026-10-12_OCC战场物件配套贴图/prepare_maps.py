from pathlib import Path
import hashlib,json,shutil
from PIL import Image
import numpy as np
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
def bind(p):return {'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
def save(p,v):p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
entries=[]
for row in json.loads((OUT/'generation_sources.json').read_text(encoding='utf-8')):
 id=row['id'];folder=OUT/id;folder.mkdir(exist_ok=True)
 raw=folder/'paired_raw_r1.png';shutil.copy2(row['pair'],raw);im=Image.open(raw).convert('RGBA');assert im.width==im.height and im.width%2==0
 half=im.width//2
 im.crop((0,0,half,half)).save(folder/'normal_panel.png');im.crop((0,half,half,im.height)).save(folder/'regions_panel.png');im.crop((half,0,im.width,half)).save(folder/'roughness_panel_rejected.png')
 normal=folder/'normal_panel.png'
 if row.get('normal'):normal=folder/'normal_raw_r2.png';shutil.copy2(row['normal'],normal)
 rough=folder/'roughness_raw_r2.png';shutil.copy2(row['rough'],rough)
 albedo=ROOT/f'UnityProject/Assets/Game/Resources/Art/FormalAcademyCombat32/{id}.png';assert Image.open(albedo).size==(32,32)
 entry={'id':id,'status':'QA_PENDING','albedo':bind(albedo),'raw':bind(raw),'roughness_raw':bind(rough),'world_pixel_pitch':1/32,'normal_convention':'OpenGL +Y up; no importer green flip','maps':[],'human_review':'PENDING'}
 for kind,source in [('normal',normal),('roughness',rough),('regions',folder/'regions_panel.png')]:
  if kind=='regions' and 'archive_stack' in id:continue
  src=np.asarray(Image.open(source).convert('RGBA'));h,w=src.shape[:2];assert np.all(src[:,:,3]==255)
  result=np.zeros((32,32,4),dtype=np.uint8);variance=[]
  for y in range(32):
   for x in range(32):
    pixels=src[int((y+.25)*h/32):int((y+.75)*h/32),int((x+.25)*w/32):int((x+.75)*w/32),:3].reshape(-1,3)
    median=np.median(pixels.astype(float),axis=0);result[y,x,:3]=pixels[np.argmin(np.sum((pixels.astype(float)-median)**2,axis=1))];result[y,x,3]=255;variance.append(float(pixels.std(axis=0).max()))
  if kind=='roughness':assert np.max(result[:,:,:3].max(axis=2)-result[:,:,:3].min(axis=2))<=5,'Non-gray roughness'
  if kind=='regions':assert result[:,:,2].max()<=12,'Unexpected mask blue data'
  dest=ROOT/f'UnityProject/Assets/Game/Resources/HD2DMaterialData/Props/{id}/{kind}.png';dest.parent.mkdir(parents=True,exist_ok=True);Image.fromarray(result).save(dest)
  Image.fromarray(result).resize((512,512),Image.Resampling.NEAREST).save(folder/(kind+'_qa16x.png'))
  registration={'schema':'occ-registered-prop-material-data-v1','status':'REVIEW_READY','source':bind(source),'reference':bind(albedo),'output':bind(dest),'native_grid':[32,32],'source_macro_pitch_xy':[w/32,h/32],'method':'Observed central-cell medoid on known original UV canvas; opaque numeric background retained','geometric_resize_or_interpolation':False,'new_rgb_colours':False,'max_cell_std':max(variance),'human_registration_review':'PENDING'}
  save(folder/(kind+'_registration.json'),registration)
  entry['maps'].append({'kind':kind,'source':bind(source),'delivery':bind(dest),'registration':bind(folder/(kind+'_registration.json')),'rgb_min':result[:,:,:3].min(axis=(0,1)).tolist(),'rgb_max':result[:,:,:3].max(axis=(0,1)).tolist()})
 entry['material_policy']='Archive wood and paper are fully nonmetal with zero glass; neutral built-in black region texture. Chest hardware is region-masked. Lantern iron/glass use independently authored red/green regions; B unused.'
 save(folder/'material_provenance.json',entry);entries.append(entry)
 print(id,[(m['kind'],m['rgb_min'],m['rgb_max']) for m in entry['maps']])
save(OUT/'material_inventory.json',{'schema':'occ-prop-material-inventory-v1','status':'QA_PENDING','assets':entries})
