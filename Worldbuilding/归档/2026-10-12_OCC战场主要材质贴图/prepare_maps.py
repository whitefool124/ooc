from pathlib import Path
import json,shutil,subprocess,sys,hashlib
from PIL import Image
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
REF=ROOT/'UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32'
def bind(p):return {'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
def save(p,v):p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
records=[]
for row in json.loads((OUT/'generation_sources.json').read_text(encoding='utf-8')):
 id=row['id'];folder=OUT/id;folder.mkdir(exist_ok=True)
 raw=folder/'paired_raw_r1.png';shutil.copy2(row['pair'],raw)
 im=Image.open(raw);assert im.width==im.height and im.width%2==0
 half=im.width//2
 im.crop((0,0,half,half)).save(folder/'normal_panel.png')
 im.crop((half,0,im.width,half)).save(folder/'roughness_panel_r1.png')
 if row.get('rough2'):
  shutil.copy2(row['rough2'],folder/'roughness_raw_r2.png')
  rough=folder/'roughness_raw_r2.png'
 else:rough=folder/'roughness_panel_r1.png'
 entry={'id':id,'status':'QA_PENDING','albedo':bind(REF/f'academy_block_{id}.png'),'raw_pair':bind(raw),'panel_crop':'Normal top-left; roughness top-right; exact quadrant crop without resampling. Bottom duplicates unused.','maps':[],'human_registration_review':'PENDING'}
 for kind,source in [('normal',folder/'normal_panel.png'),('roughness',rough)]:
  png=folder/f'{kind}32.png';report=folder/f'{kind}_registration.json'
  result=subprocess.run([sys.executable,str(ROOT/'Tools/OCCArt/decode_registered_material_map.py'),'--source',str(source),'--reference',str(REF/f'academy_block_{id}.png'),'--kind',kind,'--output',str(png),'--report',str(report)],capture_output=True,text=True)
  if result.returncode: print(id,kind,result.stderr);continue
  a=np.asarray(Image.open(png))[:,:,:3]
  if kind=='roughness' and a[:,:,0].min()<140:print('REJECT_LOW_ROUGHNESS',id,int(a[:,:,0].min()));continue
  dest=ROOT/f'UnityProject/Assets/Game/Resources/HD2DMaterialData/Battlefield/academy_block_{id}/{kind}.png'
  dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(png,dest)
  entry['maps'].append({'kind':kind,'source':bind(source),'registration':bind(report),'delivery':bind(dest),'rgb_min':a.min(axis=(0,1)).tolist(),'rgb_max':a.max(axis=(0,1)).tolist()})
 save(folder/'material_provenance.json',entry);records.append(entry)
 print(id,[(m['kind'],m['rgb_min'],m['rgb_max']) for m in entry['maps']])
save(OUT/'material_inventory.json',{'schema':'occ-hd2d-paired-material-inventory-v1','status':'QA_PENDING','world_pixel_pitch':1/32,'source_route':'Independent Codex built-in material fields; no albedo-derived numerical maps','assets':records})
assert all(len(e['maps'])==2 for e in records),'Repair failed candidates before runtime verification'
