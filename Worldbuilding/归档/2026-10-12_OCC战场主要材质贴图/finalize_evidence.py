from pathlib import Path
import hashlib,json
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
def bind(p):return {'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
def save(p,v):p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
inventory=json.loads((OUT/'material_inventory.json').read_text(encoding='utf-8'))
probe=OUT/'probe';batch=json.loads((probe/'batch_state.json').read_text(encoding='utf-8'))
assert batch['status']=='completed' and batch['materialCount']==16 and not batch['play'] and not batch['dirty']
metrics=[]
for folder in sorted(p for p in probe.iterdir() if p.is_dir()):
 result=json.loads((folder/'result.json').read_text(encoding='utf-8'))
 assert result['activeScenePreserved'] and not result['dirty'] and not result['play'] and not result['messages']
 for m in result['maps']:
  assert (m['width'],m['height'])==(32,32) and m['filter']=='Point' and not m['sRGBTexture'] and not m['mipmapEnabled'] and m['compression']=='Uncompressed'
 pairs=[('baseline_540','full_540')]
 if (folder/'baseline_1080.png').exists():pairs += [('baseline_1080','normal_only_1080'),('baseline_1080','roughness_only_1080'),('roughness_only_1080','full_1080'),('full_1080','rotated_light_1080')]
 for a,b in pairs:
  aa=np.asarray(Image.open(folder/(a+'.png')).convert('RGB')).astype(np.int16);bb=np.asarray(Image.open(folder/(b+'.png')).convert('RGB')).astype(np.int16)
  assert aa.shape==bb.shape
  if a.endswith('540'):assert aa.shape==(540,960,3)
  else:assert aa.shape==(1080,1920,3)
  delta=np.abs(aa-bb);changed=int(np.any(delta>0,axis=2).sum());assert changed>0,(folder,a,b)
  metrics.append({'material':folder.name,'a':a,'b':b,'changed_pixels':changed,'max_delta':int(delta.max()),'mean_delta':float(delta.mean())})
save(OUT/'render_metrics.json',{'scope':'Material probes only; no scene quality, motion, gameplay or performance assertion','comparisons':metrics})
# Original captures at native resolution, tiled for QA comparison.
sheet=Image.new('RGB',(1920,2160))
for i,family in enumerate(['court','road','ruin','earth']):
 folder=probe/f'academy_block_{family}_b_dry'
 sheet.paste(Image.open(folder/'baseline_540.png'),(0,i*540));sheet.paste(Image.open(folder/'full_540.png'),(960,i*540))
sheet.save(OUT/'four_materials_before_after.png')
for asset in inventory['assets']:
 for m in asset['maps']:
  delivery=ROOT/m['delivery']['path'];meta=delivery.with_suffix('.png.meta')
  guid=next(line.split(':',1)[1].strip() for line in meta.read_text().splitlines() if line.startswith('guid:'))
  manifest={'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':f"academy_block_{asset['id']}_{m['kind']}",'role':'hd2d_material_data_32','status':'QA_PENDING','provenance':{'source_channel':'codex_builtin_imagegen','source_path':asset['raw_pair']['path'],'source_sha256':asset['raw_pair']['sha256'],'component_source':m['source'],'registration':m['registration']},'delivery':{'output_path':m['delivery']['path'],'output_sha256':m['delivery']['sha256'],'native_ppu':32,'dimensions':[32,32]},'unity_import':{'guid':guid,'filter_mode':'Point','colour_space':'linear','mipmaps':False,'compression':'Uncompressed','wrap_mode':'Repeat'},'application':{'albedo':asset['albedo'],'shared_uv':True,'world_pixel_pitch':1/32,'record':bind(probe/f"academy_block_{asset['id']}_dry/result.json")},'human_review':{'overall':'PENDING'},'admission_blockers':['Numeric material data role not registered in Sprite admission contract; cannot promote to FORMAL','Full-scene density inventory and genuine human registration/material review pending']}
  if m['kind']=='roughness' and asset['id'].endswith('_b') and asset['id']!='court_b':manifest['provenance']['source_path']=m['source']['path'];manifest['provenance']['source_sha256']=m['source']['sha256']
  save(OUT/asset['id']/f"{m['kind']}_manifest.json",manifest)
report=json.loads((OUT/'quality_candidate.json').read_text(encoding='utf-8'))
code=[ROOT/p for p in ['UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldMaterialMaps.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DAcademyEnvironment.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldSample.cs','UnityProject/Assets/Game/Editor/Hd2DBattlefieldMaterialProbe.cs','UnityProject/Assets/Game/Resources/Shaders/Hd2DBakedArchitecture.shader','UnityProject/Assets/Game/Resources/Shaders/Hd2DWetCourtyard.shader','Tools/OCCArt/decode_registered_material_map.py']]
code += [p.with_suffix(p.suffix+'.meta') for p in list(code) if p.with_suffix(p.suffix+'.meta').exists()]
data=list((ROOT/'UnityProject/Assets/Game/Resources/HD2DMaterialData').rglob('*'));data=[p for p in data if p.is_file()]
report['changed_files']=sorted(set(bind(p)['path'] for p in code+data))
inputs=[bind(p) for p in code+data]+[asset['albedo'] for asset in inventory['assets']]
inputs += [bind(p) for p in OUT.rglob('*') if p.is_file() and 'before' not in p.parts and p.suffix in ['.png','.json'] and p.name!='quality_candidate.json']
report['inputs']=list({v['path']:v for v in report['inputs']+inputs}.values())
report['local_material_probe']={'batch':bind(probe/'batch_state.json'),'metrics':bind(OUT/'render_metrics.json'),'dry_material_count':16,'additional_wet_variant_count':1,'normal_entry':False,'window_focus':False,'play_mode':False}
report['runtime'].update(isolation_pass=True,record=bind(probe/'batch_state.json'))
report['blockers']=['Only material-local offscreen captures verified; complete game scene, motion/zoom, performance and genuine human aesthetic approval pending','Numeric material-data role remains QA_PENDING outside formal Sprite admission']
save(OUT/'quality_candidate.json',report)
print(json.dumps({'materials':16,'wet_extra':1,'comparisons':len(metrics),'all_nonzero':True,'status':'QA_PENDING'}))
