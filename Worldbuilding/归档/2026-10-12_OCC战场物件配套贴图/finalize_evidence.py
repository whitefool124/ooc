from pathlib import Path
import hashlib,json
from PIL import Image
import numpy as np
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
def bind(p):return {'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
def save(p,v):p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
inventory=json.loads((OUT/'material_inventory.json').read_text(encoding='utf-8'));probe=OUT/'probe'
batch=json.loads((probe/'batch_state.json').read_text(encoding='utf-8'));assert batch['status']=='completed' and batch['materialCount']==7 and not batch['play'] and not batch['dirty']
metrics=[]
for folder in sorted(p for p in probe.iterdir() if p.is_dir()):
 result=json.loads((folder/'result.json').read_text(encoding='utf-8'));assert result['activeScenePreserved'] and not result['play'] and not result['dirty'] and not result['messages']
 for m in result['maps']:assert (m['width'],m['height'])==(32,32) and m['filter']=='Point' and not m['sRGBTexture'] and not m['mipmapEnabled'] and m['compression']=='Uncompressed'
 pairs=[('baseline_540','full_540')]
 if (folder/'baseline_1080.png').exists():pairs += [('baseline_1080','normal_only_1080'),('baseline_1080','surface_only_1080'),('surface_only_1080','full_1080'),('full_1080','rotated_light_1080')]
 for a,b in pairs:
  aa=np.asarray(Image.open(folder/(a+'.png')).convert('RGB')).astype(np.int16);bb=np.asarray(Image.open(folder/(b+'.png')).convert('RGB')).astype(np.int16);assert aa.shape==bb.shape
  assert aa.shape==((540,960,3) if a.endswith('540') else (1080,1920,3))
  delta=np.abs(aa-bb);changed=int(np.any(delta>0,axis=2).sum());assert changed>0,(folder,a,b)
  background=aa[0,0];outside=np.all(aa==background,axis=2);spill=int(np.any(delta[outside]>0,axis=1).sum())
  # RGB equality with background is not an alpha/depth coverage test: dark material can share its RGB.
  # Retain collisions as a diagnostic; do not claim silhouette PASS from colour segmentation.
  metrics.append({'asset':folder.name,'a':a,'b':b,'changed_pixels':changed,'max_delta':int(delta.max()),'mean_delta':float(delta.mean()),'background_changed_pixels':spill})
save(OUT/'render_metrics.json',{'scope':'Local sprite material responses and background footprint only; not scene quality/human approval','comparisons':metrics})
sheet=Image.new('RGB',(1920,1620))
for i,id in enumerate(['academy_heavy_archive_stack_intact','academy_loot_chest_closed','academy_decoy_lantern_active']):
 folder=probe/(id+'_dry');sheet.paste(Image.open(folder/'baseline_540.png'),(0,i*540));sheet.paste(Image.open(folder/'full_540.png'),(960,i*540))
sheet.save(OUT/'props_before_after.png')
for asset in inventory['assets']:
 for m in asset['maps']:
  dest=ROOT/m['delivery']['path'];meta=dest.with_suffix('.png.meta');guid=next(line.split(':',1)[1].strip() for line in meta.read_text().splitlines() if line.startswith('guid:'))
  manifest={'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':asset['id']+'_'+m['kind'],'role':'hd2d_material_data_32','status':'QA_PENDING','provenance':{'source_channel':'codex_builtin_imagegen','source_path':m['source']['path'],'source_sha256':m['source']['sha256'],'independent_raw':asset['roughness_raw'] if m['kind']=='roughness' else asset['raw'],'registration':m['registration']},'delivery':{'output_path':m['delivery']['path'],'output_sha256':m['delivery']['sha256'],'native_ppu':32,'dimensions':[32,32]},'unity_import':{'guid':guid,'filter_mode':'Point','colour_space':'linear','mipmaps':False,'compression':'Uncompressed','wrap_mode':'Clamp'},'application':{'albedo':asset['albedo'],'shared_uv':True,'world_pixel_pitch':1/32,'record':bind(probe/(asset['id']+'_dry')/'result.json')},'human_review':{'overall':'PENDING'},'admission_blockers':['Numeric material-data role not registered in Sprite formal admission; no FORMAL promotion','Source registration, complete scene density and genuine human material review pending']}
  if asset['id']=='academy_loot_chest_closed' and m['kind']=='normal':manifest['provenance']['independent_raw']=m['source']
  save(OUT/asset['id']/(m['kind']+'_manifest.json'),manifest)
report=json.loads((OUT/'quality_candidate.json').read_text(encoding='utf-8'))
code=[ROOT/p for p in ['UnityProject/Assets/Game/Resources/Shaders/Hd2DGroundedSprite.shader','UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldPropMaps.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldSample.cs','UnityProject/Assets/Game/Editor/Hd2DBattlefieldPropProbe.cs']]
code += [p.with_suffix(p.suffix+'.meta') for p in list(code) if p.with_suffix(p.suffix+'.meta').exists()]
data=[p for p in (ROOT/'UnityProject/Assets/Game/Resources/HD2DMaterialData/Props').rglob('*') if p.is_file()]
report['changed_files']=sorted(set(bind(p)['path'] for p in code+data))
inputs=[bind(p) for p in code+data]+[a['albedo'] for a in inventory['assets']]+[bind(p) for p in OUT.rglob('*') if p.is_file() and 'before' not in p.parts and 'failed_backface_r1' not in p.parts and p.suffix in ['.json','.png'] and p.name!='quality_candidate.json']
report['inputs']=list({v['path']:v for v in report['inputs']+inputs}.values());report['runtime'].update(isolation_pass=True,record=bind(probe/'batch_state.json'))
report['local_prop_probe']={'batch':bind(probe/'batch_state.json'),'metrics':bind(OUT/'render_metrics.json'),'material_count':7,'normal_entry':False,'window_focus':False,'play_mode':False}
report['blockers']=['Only local sprite material probes verified; source-region alignment/human review, full scene density, game motion/flow and performance pending','Glass is local mirror coat, not transmissive/refractive glass; unregistered numeric maps remain QA_PENDING']
save(OUT/'quality_candidate.json',report)
print(json.dumps({'materials':7,'maps':sum(len(a['maps']) for a in inventory['assets']),'comparisons':len(metrics),'status':'QA_PENDING'}))
