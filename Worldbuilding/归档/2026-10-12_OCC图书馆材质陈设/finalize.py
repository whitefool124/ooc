import hashlib,json,subprocess,sys
from pathlib import Path

folder=Path(__file__).resolve().parent;root=folder.parents[2]
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
bind=lambda p:{'path':p.relative_to(root).as_posix(),'sha256':sha(p)}
write=lambda p,d:p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
rounds=[json.loads((folder/f'decode_r{i}.json').read_text(encoding='utf-8')) for i in range(1,4)]
assert all(r['status']=='REJECT_UNSTABLE_SUBJECT_GRID' and r['subject_lattice_confidence']<1.2 for r in rounds)
check=json.loads((folder/'normal_variant_check.json').read_text(encoding='utf-8'))
assert check['isWarmedUp'] and not check['messages'] and not check['play'] and not check['dirty']
source=folder/'bookshelf_raw_r3.png'
manifest={'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':'occ.library.bookshelf.rejected.r3','role':'hd2d_environment_module','status':'QA_PENDING',
 'provenance':{'source_channel':'codex_builtin_imagegen','source_descriptor':'Third independent library shelving raw; rejected subject lattice','source_path':bind(source)['path'],'source_sha256':sha(source)},
 'delivery':{'output_path':None,'native_ppu':32,'logical_cells':None,'palette_max':16},
 'application':{'rendering_mode':'battlefield_hd2d_environment','camera_projection':'perspective','runtime_draw_rect':'Not placed: source rejected'},
 'evidence':{},'human_review':{'overall':'PENDING'},
 'blockers':['Three independent raws failed1.2 subject-grid threshold; no deliverable or Unity import','No current application or human approval; normal/surface maps not provided'],
 'generation_rounds':[{'source':bind(folder/f'bookshelf_raw_r{i}.png'),'decode':bind(folder/f'decode_r{i}.json'),'status':r['status'],'confidence':r['subject_lattice_confidence']} for i,r in enumerate(rounds,1)]}
write(folder/'manifest.json',manifest)
brief=json.loads((root/'Tools/OCCArt/hd2d_material_brief_template_v1.json').read_text(encoding='utf-8'))
brief.update(asset_id=manifest['asset_id'],asset_role='hd2d_environment_module',scene_profile='battlefield-v1/library_discipline',reading_goal='Recognize wood shelves and paper book clusters in a low noninteractive backdrop',stage_theme_source='OCC mother document A.2.4; existing library_discipline')
brief['pixel_density'].update(reference_asset_id='academy_block_court_b/native32 battle units',world_pixel_pitch=1/32,world_size_or_uv_mapping='Intended native32 card; no accepted decode or placement')
brief['construction']['type']='pixel card; not built'
brief['material_regions']=[{'id':'walnut','meaning':'wood structure'}, {'id':'paper','meaning':'book blocks'}]
brief['maps']['albedo'].update(source=None,sha256=None,status='RAW_REJECTED')
write(folder/'material_brief.json',brief)

report_path=folder/'quality_candidate.json'
subprocess.run([sys.executable,str(root/'Tools/OCCArt/create_hd2d_quality_report.py'),'--scene','battlefield','--output',str(report_path)],check=True,capture_output=True)
report=json.loads(report_path.read_text(encoding='utf-8'))
changed=['UnityProject/Assets/Game/Resources/Shaders/Hd2DBakedArchitecture.shader','UnityProject/Assets/Game/Runtime/Presentation/Hd2DAcademyEnvironment.cs','UnityProject/Assets/Game/Editor/Hd2DLocalIntegrationTools.cs']
inputs={b['path']:b for b in report['inputs']}
for p in changed:inputs[p]=bind(root/p)
for p in folder.iterdir():
 if p.is_file() and p.name not in {'quality_candidate.json','quality_gate.json','asset_gate.json'}:inputs[bind(p)['path']]=bind(p)
report['changed_files']=changed;report['inputs']=list(inputs.values());report['additional_evidence']=[bind(folder/'normal_variant_check.json')]
report['runtime_observations']={'compile_readiness':check,'new_art':'No new accepted delivery or Unity import','generation':manifest['generation_rounds'],'verification':'one shader variant; no Play Mode, journey or focused windows'}
report['blockers']=['Normal input/tangents implemented but no authored new normal or surface maps','Bookshelf generation rejected after three independent rounds','Current in-game material response/screenshots/motion/human review and performance missing']
write(report_path,report)
for name,args in [('asset_gate',['validate_occ_art_asset.py',str(folder/'manifest.json')]),('quality_gate',['validate_hd2d_quality.py',str(report_path),'--scope','battlefield'])]:
 r=subprocess.run([sys.executable,'-X','utf8',str(root/'Tools/OCCArt'/args[0]),*args[1:]],capture_output=True,text=True,encoding='utf-8');(folder/(name+'.json')).write_text(r.stdout,encoding='utf-8');print(name,r.returncode)
