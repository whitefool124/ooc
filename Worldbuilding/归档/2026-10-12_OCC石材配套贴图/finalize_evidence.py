import hashlib,json
from pathlib import Path
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
def rel(p):return p.relative_to(ROOT).as_posix()
def binding(p):return {'path':rel(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
def save(p,obj):p.write_text(json.dumps(obj,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
probe=OUT/'probe_v4'
arrays={p.stem:np.asarray(Image.open(p).convert('RGB')).astype(np.int16) for p in probe.glob('*.png')}
pairs=[('baseline_1080','normal_only_1080'),('baseline_1080','roughness_only_1080'),('baseline_1080','full_1080'),('roughness_only_1080','full_1080'),('full_1080','rotated_light_1080')]
metrics=[]
for a,b in pairs:
    delta=np.abs(arrays[a]-arrays[b]); changed=int(np.any(delta>0,axis=2).sum())
    assert changed>0,(a,b)
    metrics.append({'a':a,'b':b,'changed_pixels':changed,'mean_abs_channel_delta':float(delta.mean()),'max_channel_delta':int(delta.max())})
assert arrays['full_1080'].shape==(1080,1920,3)
assert arrays['full_540'].shape==(540,960,3)
save(OUT/'probe_metrics.json',{'scope':'Local offscreen material response only; no gameplay, performance or human approval','comparisons':metrics})
# Exact native captures side by side: no image modification or synthesized rendering.
comparison=Image.new('RGB',(3840,1080))
comparison.paste(Image.open(probe/'baseline_1080.png'),(0,0));comparison.paste(Image.open(probe/'full_1080.png'),(1920,0))
comparison.save(OUT/'before_after_1080.png')
base=ROOT/'UnityProject/Assets/Game/Resources'
albedo=base/'Art/FormalAcademyIndependentFloors32/academy_block_court_b.png'
brief=json.loads((ROOT/'Tools/OCCArt/hd2d_material_brief_template_v1.json').read_text(encoding='utf-8-sig'))
brief.update(asset_id='courtyard_stone_paired_data_32',asset_role='opaque_numeric_material_data_candidate',scene_profile='battlefield',reading_goal='Shallow chipped stone relief and spatially varying dry roughness; existing albedo unchanged')
brief['pixel_density'].update(reference_asset_id='academy_block_court_b',authored_pixel_pitch=1,world_pixel_pitch=1/32,world_size_or_uv_mapping='Shared albedo UV; 32 texels per metre. Probe uses one 1m tile.')
brief['maps']['albedo'].update(source=rel(albedo),sha256=binding(albedo)['sha256'])
for kind in ('normal','roughness'):
    raw=OUT/f'stone_{kind}_raw_r1.png';delivery=base/f'HD2DMaterialData/CourtyardStone/{kind}.png'
    data={**binding(delivery),'source':rel(delivery),'colour_space':'linear','status':'QA_PENDING','raw':binding(raw),'registration':binding(OUT/f'{kind}_registered_decode.json')}
    if kind=='normal':data.update(convention='Source green image-down; Unity NormalMap importer flipGreenChannel=true',strength=.35)
    else:data.update(channels='R roughness; shader smoothness=1-R before wetness',range_r=[133/255,219/255])
    brief['maps'][kind]=data
    manifest={'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':f'courtyard_stone_{kind}_32','role':'hd2d_material_data_32','status':'QA_PENDING','provenance':{'source_channel':'codex_builtin_imagegen','source_descriptor':'Independent paired numeric material field; not inferred from albedo brightness','source_path':rel(raw),'source_sha256':binding(raw)['sha256']},'delivery':{'output_path':rel(delivery),'output_sha256':binding(delivery)['sha256'],'native_ppu':32,'dimensions':[32,32]},'application':{'albedo':binding(albedo),'shared_uv':True,'record':binding(probe/'result.json')},'human_review':{'overall':'PENDING'},'admission_blockers':['Numeric material-data role is not registered in the sprite admission contract; this provenance manifest does not authorize FORMAL admission','Full scene pixel density, normal-entry game view and genuine human review pending']}
    save(OUT/f'{kind}_manifest.json',manifest)
brief['registration'].update(shared_subject_canvas_anchor_uv='REGISTERED_CANDIDATE',map_pixel_pitch_review='MANUAL_REVIEW_PENDING')
brief['application_review'].update(neutral_light='LOCAL_CAPTURE',default_light='LOCAL_CAPTURE',rotated_light_material_response='LOCAL_NONZERO_DELTA',normal_and_surface_maps_off='LOCAL_CAPTURE',reading_at_1080_and_540='LOCAL_CAPTURE')
brief['admission']='QA_PENDING numeric-data candidate. Generic sprite normal decode confidence 1.1561 failed; known-canvas numeric registration is separately recorded, not a generic decoder PASS or threshold relaxation.'
save(OUT/'material_brief.json',brief)
report=json.loads((OUT/'quality_candidate.json').read_text(encoding='utf-8-sig'))
changed=[ROOT/p for p in ['Tools/OCCArt/decode_registered_material_map.py','UnityProject/Assets/Game/Runtime/Presentation/Hd2DCourtyardStoneMaps.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DAcademyEnvironment.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldSample.cs','UnityProject/Assets/Game/Editor/Hd2DStoneMaterialProbe.cs','UnityProject/Assets/Game/Resources/Shaders/Hd2DBakedArchitecture.shader','UnityProject/Assets/Game/Resources/Shaders/Hd2DWetCourtyard.shader','Tools/OCCArt/native32_generation_profiles.json','Tools/OCCArt/occ_art_contract_v1.json','Tools/OCCArt/occ_art_production_brief_template_v1.json','AGENTS.md','Worldbuilding/策划案/OCC_项目总策划案_v1.0.md','Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv']]
changed+=list((base/'HD2DMaterialData').rglob('*.png'))+list((base/'HD2DMaterialData').rglob('*.meta'))
changed+=[p.with_suffix(p.suffix+'.meta') for p in list(changed) if p.with_suffix(p.suffix+'.meta').is_file()]
report['changed_files']=sorted(set(rel(p) for p in changed))
inputs=[binding(p) for p in changed]+[binding(albedo)]
inputs+=[binding(p) for p in OUT.glob('*.json') if p.name!='quality_candidate.json']+[binding(p) for p in OUT.glob('stone_*raw*.png')]+[binding(p) for p in probe.iterdir() if p.is_file()]+[binding(OUT/'before_after_1080.png')]
report['inputs']=list({v['path']:v for v in report['inputs']+inputs}.values())
report['runtime'].update(isolation_pass=True,record=binding(probe/'result.json'))
report['local_material_probe']={'result':binding(probe/'result.json'),'metrics':binding(OUT/'probe_metrics.json'),'normal_strength':.35,'normal_green_flip':True,'normal_entry':False,'window_focus':False,'play_mode':False}
report['blockers']=['Only single-material offscreen render verified; normal-entry scene, motion/zoom, complete density inventory, performance and genuine human approval remain pending','Opaque numeric material-data role has not passed formal asset admission']
save(OUT/'quality_candidate.json',report)
print(json.dumps(metrics,ensure_ascii=False))
